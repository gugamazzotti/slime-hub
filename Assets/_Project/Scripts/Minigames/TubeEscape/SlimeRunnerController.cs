using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Gameplay;
using SlimeHub.Physics;

namespace SlimeHub.Minigames
{
    public readonly struct RunnerBody
    {
        public readonly Vector2 Point;
        public readonly float Radius;
        public readonly int Side;

        public RunnerBody(Vector2 point, float radius, int side)
        {
            Point = point;
            Radius = radius;
            Side = side;
        }
    }

    public sealed class SlimeRunnerController : MonoBehaviour, IDamageable
    {
        public float Mass = 18f;
        public float X;
        public float Y = -2f;
        public float Speed;
        public float HalfWidth = 2.45f;
        public float MinMass = 5f;
        public float SplitFraction = 0.04f;
        public bool Alive = true;
        public bool IsSplit { get; private set; }
        public float PeakMass { get; private set; }
        public int Blobs { get; private set; }

        SlimeActor main;
        SlimeActor left;
        SlimeActor right;
        Transform membrane;
        float leftMass;
        float rightMass;
        float separation;
        float boost;
        float baseSpeed = 7.4f;
        float speedGain = 0.05f;
        float boostMax = 2.6f;
        float runTime;
        float fragility = 1f;
        float aimX;
        float slide;
        float tearUntil;
        float splitUntil;
        const float MeshRadius = 0.5f;

        public float AimX => aimX;

        public void Configure(float startMass, float halfWidth, float minMass, float splitFraction, float baseSpeed, float speedGain, float boostMax, SlimeActor main, SlimeActor left, SlimeActor right, Transform membrane)
        {
            Mass = startMass;
            PeakMass = startMass;
            HalfWidth = halfWidth;
            MinMass = minMass;
            SplitFraction = splitFraction;
            this.baseSpeed = baseSpeed;
            this.speedGain = speedGain;
            this.boostMax = boostMax;
            this.main = main;
            this.left = left;
            this.right = right;
            this.membrane = membrane;
            left.gameObject.SetActive(false);
            right.gameObject.SetActive(false);
            membrane.gameObject.SetActive(false);
            PlaceMain(1f);
        }

        public float RadiusOf(float mass)
        {
            float t = Mathf.InverseLerp(4f, 72f, mass);
            return Mathf.Lerp(0.28f, 0.9f, t);
        }

        public void Tick(float dt, float steerWorld, bool wantSplit, float pinch01)
        {
            if (!Alive)
                return;

            runTime += dt;
            boost = Mathf.MoveTowards(boost, 0f, dt * 1.7f);
            Speed = baseSpeed + runTime * speedGain + boost;
            Y += Speed * dt;
            aimX += steerWorld;

            if (!IsSplit && wantSplit && Mass > MinMass * 2.1f)
                BeginSplit();

            float limit = IsSplit
                ? HalfWidth - Mathf.Max(RadiusOf(leftMass), RadiusOf(rightMass))
                : HalfWidth - RadiusOf(Mass);
            aimX = Mathf.Clamp(aimX, -limit, limit);
            FlowCenter(dt, limit);

            if (IsSplit)
            {
                float target = wantSplit ? Mathf.Lerp(0.45f, HalfWidth * 0.78f, Mathf.Clamp01(pinch01)) : 0.08f;
                separation = Mathf.MoveTowards(separation, target, dt * 7.5f);
                if (!wantSplit && separation <= 0.12f)
                    Merge();
                else
                    PlaceHalves(dt);
            }
            else
            {
                PlaceMain(dt);
            }

            if (Mass > PeakMass)
                PeakMass = Mass;
            if (Mass < MinMass)
                Alive = false;
        }

        public int CollectBodies(RunnerBody[] buffer)
        {
            if (!IsSplit)
            {
                buffer[0] = new RunnerBody(new Vector2(X, Y), RadiusOf(Mass), 0);
                return 1;
            }

            int count = 0;
            if (leftMass > 0.2f)
                buffer[count++] = new RunnerBody(new Vector2(X - separation, Y), RadiusOf(leftMass), 1);
            if (rightMass > 0.2f)
                buffer[count++] = new RunnerBody(new Vector2(X + separation, Y), RadiusOf(rightMass), 2);
            return count;
        }

        public void Tear(Vector3 mouth, float amount)
        {
            if (!Alive)
                return;
            int side = ClosestSide(mouth);
            SlimeActor struck = side == 1 ? left : side == 2 ? right : main;
            Vector3 from = struck != null && struck.gameObject.activeSelf
                ? struck.transform.position
                : new Vector3(X, Y, 0f);
            Damage(amount, mouth, (from - mouth).sqrMagnitude > 0.0001f ? (from - mouth).normalized : Vector3.down);
            if (struck == null || !struck.gameObject.activeSelf || struck.Deformer == null)
                return;
            Vector3 reach = mouth - struck.transform.position;
            reach.z = 0f;
            struck.Deformer.SnapFlow(reach * 0.8f);
            struck.Deformer.Nudge(reach.normalized * 7f);
            tearUntil = Time.time + 0.5f;
        }

        void FlowCenter(float dt, float limit)
        {
            float visc = main != null ? main.Look.Viscosity : 0.35f;
            float response = Mathf.Lerp(15f, 4.4f, visc);
            float maxSpeed = Mathf.Lerp(8.8f, 3.1f, visc);
            float desired = Mathf.Clamp((aimX - X) * response, -maxSpeed, maxSpeed);
            float blend = 1f - Mathf.Exp(-Mathf.Lerp(6.2f, 2.1f, visc) * dt);
            slide = Mathf.Lerp(slide, desired, blend);
            X = Mathf.Clamp(X + slide * dt, -limit, limit);
            if (Mathf.Abs(X) >= limit - 0.02f && Mathf.Abs(aimX - X) < 0.05f)
                slide = 0f;
        }

        public void Damage(float amount, Vector3 point, Vector3 normal)
        {
            if (!Alive)
                return;
            fragility = Mathf.Min(2.1f, fragility + 0.1f);
            float loss = amount * fragility;
            int side = ClosestSide(point);
            if (IsSplit)
            {
                if (side != 2)
                    leftMass = Mathf.Max(0f, leftMass - loss);
                if (side != 1)
                    rightMass = Mathf.Max(0f, rightMass - (side == 0 ? loss * 0.5f : loss));
                if (leftMass < 1.2f) leftMass = 0f;
                if (rightMass < 1.2f) rightMass = 0f;
                Mass = leftMass + rightMass;
                if (leftMass <= 0f || rightMass <= 0f)
                    Collapse();
            }
            else
            {
                Mass = Mathf.Max(0f, Mass - loss);
            }

            boost = boostMax;
            SlimeActor struck = side == 1 ? left : side == 2 ? right : main;
            if (struck != null && struck.gameObject.activeSelf)
            {
                struck.Deformer.Impact(point, normal, 2.4f);
                struck.AddImpactFlatten(normal, 0.28f);
            }
            if (Mass < MinMass)
                Alive = false;
        }

        public void Collect(float amount, Vector3 point)
        {
            Blobs++;
            if (IsSplit)
            {
                if (ClosestSide(point) == 2)
                    rightMass += amount;
                else
                    leftMass += amount;
                Mass = leftMass + rightMass;
            }
            else
            {
                Mass += amount;
            }
            if (Mass > PeakMass)
                PeakMass = Mass;
            main.Deformer.Pulse(3.2f);
            if (IsSplit)
            {
                left.Deformer.Pulse(2.4f);
                right.Deformer.Pulse(2.4f);
            }
        }

        public void PlaySqueeze(float amount)
        {
            float push = Mathf.Lerp(0.2f, 1.05f, Mathf.Clamp01(amount));
            PushUp(main, push);
            if (!IsSplit)
                return;
            PushUp(left, push);
            PushUp(right, push);
        }

        void PushUp(SlimeActor actor, float push)
        {
            if (actor == null || !actor.gameObject.activeSelf || actor.Deformer == null)
                return;
            actor.Deformer.SetFlow(new Vector3(0f, push, 0f));
            actor.Deformer.Nudge(new Vector3(slide * 0.3f, Speed + push * 5f, 0f));
        }

        void BeginSplit()
        {
            Mass *= 1f - SplitFraction;
            leftMass = Mass * 0.5f;
            rightMass = Mass * 0.5f;
            separation = 0.35f;
            IsSplit = true;
            main.Deformer.SnapFlow(Vector3.zero);
            main.gameObject.SetActive(false);
            left.gameObject.SetActive(true);
            right.gameObject.SetActive(true);
            membrane.gameObject.SetActive(true);
            left.Deformer.SnapFlow(Vector3.left * 0.7f);
            right.Deformer.SnapFlow(Vector3.right * 0.7f);
            splitUntil = Time.time + 0.38f;
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(left);
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(right);
            left.Deformer.Pulse(2f);
            right.Deformer.Pulse(2f);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Pop, 1.25f);
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Medium();
        }

        void Merge()
        {
            Mass = leftMass + rightMass;
            IsSplit = false;
            separation = 0f;
            splitUntil = 0f;
            left.gameObject.SetActive(false);
            right.gameObject.SetActive(false);
            membrane.gameObject.SetActive(false);
            main.gameObject.SetActive(true);
            main.Deformer.Pulse(3.4f);
            PlaceMain(1f);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Merge, Mathf.Lerp(1.2f, 0.7f, Mathf.Clamp01(Mass / 50f)));
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Medium();
        }

        void Collapse()
        {
            if (leftMass <= 0f && rightMass <= 0f)
            {
                Mass = 0f;
                Alive = false;
                return;
            }
            Mass = Mathf.Max(leftMass, rightMass);
            Merge();
        }

        int ClosestSide(Vector3 point)
        {
            if (!IsSplit)
                return 0;
            float leftX = X - separation;
            float rightX = X + separation;
            return Mathf.Abs(point.x - leftX) < Mathf.Abs(point.x - rightX) ? 1 : 2;
        }

        void PlaceMain(float dt)
        {
            float scale = RadiusOf(Mass) / MeshRadius;
            float idle = 1f + Mathf.Sin(Time.time * 5.5f) * 0.025f;
            Vector3 target = new Vector3(scale * (1f + Mathf.Abs(slide) * 0.02f), scale * idle, scale);
            main.transform.localScale = Vector3.Lerp(main.transform.localScale, target, 1f - Mathf.Exp(-9f * dt));
            float bob = Mathf.Sin(Time.time * 6.2f) * 0.035f;
            main.transform.SetPositionAndRotation(new Vector3(X, Y + bob, 0f), Lean());
            Flow(main);
        }

        Quaternion Lean()
        {
            float tilt = Mathf.Clamp(-slide * 3.4f, -16f, 16f);
            return Quaternion.Euler(0f, 0f, tilt);
        }

        void Flow(SlimeActor actor)
        {
            if (actor.Deformer == null)
                return;
            actor.Deformer.Nudge(new Vector3(slide, Speed, 0f));
            if (Time.time < tearUntil || Time.time < splitUntil)
                return;
            float visc = actor.Look.Viscosity;
            float lean = Mathf.Lerp(0.07f, 0.032f, visc);
            Vector3 lead = new Vector3(slide * lean * 1.6f, Speed * lean, 0f);
            float span = actor.Deformer.Radius * Mathf.Max(0.25f, actor.transform.lossyScale.x);
            float cap = span * 2.3f;
            if (lead.sqrMagnitude > cap * cap)
                lead = lead.normalized * cap;
            actor.Deformer.SetFlow(lead);
        }

        void PlaceHalves(float dt)
        {
            PlaceHalf(left, X - separation, leftMass, dt);
            PlaceHalf(right, X + separation, rightMass, dt);
            float bridge = Mathf.Max(0.18f, separation * 1.55f);
            float pulse = 0.04f * Mathf.Sin(Time.time * 10f);
            membrane.SetPositionAndRotation(new Vector3(X, Y, 0f), Quaternion.identity);
            membrane.localScale = new Vector3(bridge, 0.2f + pulse, 0.2f + pulse * 0.5f);
        }

        void PlaceHalf(SlimeActor actor, float x, float mass, float dt)
        {
            if (mass <= 0.2f)
            {
                actor.gameObject.SetActive(false);
                return;
            }
            actor.gameObject.SetActive(true);
            float scale = RadiusOf(mass) / MeshRadius;
            actor.transform.localScale = Vector3.Lerp(actor.transform.localScale, Vector3.one * scale, 1f - Mathf.Exp(-9f * dt));
            float bob = Mathf.Sin(Time.time * 7f + x * 3f) * 0.045f;
            actor.transform.SetPositionAndRotation(new Vector3(x, Y + bob, 0f), Lean());
            Flow(actor);
        }
    }
}
