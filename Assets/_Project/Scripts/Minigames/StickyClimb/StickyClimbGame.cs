using System.Collections.Generic;
using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Data;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.Physics;
using SlimeHub.UI;

namespace SlimeHub.Minigames
{
    public sealed class ClimbWall : MonoBehaviour
    {
    }

    public sealed class StickyClimbGame : MonoBehaviour
    {
        GameStateMachine fsm;
        IGameState countdown;
        IGameState playing;
        IGameState paused;
        IGameState gameOver;
        MinigameShell shell;
        MinigameConfigSO config;
        SlimeActor slime;
        Camera cam;
        Vector3 anchor;
        Vector3 sideNormal = Vector3.right;
        Vector3 pull;
        bool stuck = true;
        bool aiming;
        bool flying;
        float stuckTime;
        Transform backdrop;
        float acid = -3.2f;
        Transform acidView;
        float maxHeight;
        float ignoreUntil;
        float overTime;
        bool resolved;
        bool record;
        int score;
        int reward;
        float countT;
        int countStep = -1;
        Vector3 savedGravity;
        readonly List<Transform> goo = new List<Transform>();
        int gooCursor;
        const int AimCount = 12;
        Transform[] aimDots;
        Material aimMaterial;

        void Awake()
        {
            GameServices.Ensure();
            config = MinigameConfigSO.Load();
            savedGravity = UnityEngine.Physics.gravity;
            UnityEngine.Physics.gravity = new Vector3(0f, -config.climbGravity, 0f);

            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.16f);
            cam.transform.rotation = Quaternion.identity;

            backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            var backdropObject = backdrop.gameObject;
            Destroy(backdropObject.GetComponent<Collider>());
            backdropObject.name = "Backdrop";
            backdrop.localScale = new Vector3(10f, 16f, 1f);
            backdropObject.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(
                new Color(0.07f, 0.1f, 0.2f, 1f), true, 0f, 0.22f, new Color(0.35f, 0.75f, 0.95f, 1f));

            Wall(-config.climbHalfWidth - 0.2f, true);
            Wall(config.climbHalfWidth + 0.2f, false);

            acidView = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(acidView.GetComponent<Collider>());
            acidView.name = "Acid";
            acidView.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(new Color(0.72f, 0.95f, 0.18f, 0.55f), false, 0.45f, 0f, new Color(0.95f, 1f, 0.45f, 1f));
            acidView.SetParent(transform, false);
            UpdateAcidVisual();

            slime = SlimeActor.Create(Vector3.zero, 0.42f, AppQuality.LowMemory ? 1 : 2, true, true);
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(slime);
            slime.Body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            slime.Body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            slime.Body.linearDamping = 0.15f;
            var forward = slime.gameObject.AddComponent<ClimbRelay>();
            forward.Game = this;

            for (int i = 0; i < 18; i++)
            {
                var patch = new GameObject("Goo");
                var filter = patch.AddComponent<MeshFilter>();
                filter.sharedMesh = SlimeMeshFactory.SharedBlob(1, 0.2f);
                var renderer = patch.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = SlimeMaterials.CreateUnlit(new Color(slime.Look.Base.r, slime.Look.Base.g, slime.Look.Base.b, 0.45f), false);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                patch.SetActive(false);
                goo.Add(patch.transform);
            }

            anchor = new Vector3(-config.climbHalfWidth + 0.42f, 0.4f, 0f);
            sideNormal = Vector3.right;
            StickAt(anchor, sideNormal, false);

            shell = gameObject.AddComponent<MinigameShell>();
            shell.Build(Loc.Get("game.climb"));
            shell.PausePressed += () => { if (fsm.Current == playing) fsm.Change(paused); };
            shell.ResumePressed += () => fsm.Change(playing);
            shell.Home += () => SceneFlow.Load(SceneNames.Hub);
            shell.Retry += () => SceneFlow.Load(SceneNames.Climb);
            shell.SetHint(Loc.Get("hud.pull"));

            countdown = new LambdaState(TickCountdown);
            playing = new LambdaState(TickPlaying);
            paused = new LambdaState(null, () =>
            {
                Time.timeScale = 0f;
                shell.ShowPause(true);
                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                    audio.SetStretchLoop(false, 1f);
            }, () => { Time.timeScale = 1f; shell.ShowPause(false); });
            gameOver = new LambdaState(TickOver, EnterOver);
            fsm = new GameStateMachine();
            fsm.Change(countdown);
        }

        void Wall(float x, bool left)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = left ? "WallL" : "WallR";
            wall.transform.position = new Vector3(x, 80f, 0f);
            wall.transform.localScale = new Vector3(0.4f, 200f, 1f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(new Color(0.55f, 0.72f, 0.82f, 1f), true, 0.15f, 0f, Color.white);
            wall.AddComponent<ClimbWall>();
        }

        void OnDestroy()
        {
            UnityEngine.Physics.gravity = savedGravity;
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.SetStretchLoop(false, 1f);
        }

        void Update() => fsm.Update();

        void TickCountdown()
        {
            countT += Time.unscaledDeltaTime;
            int step = countT < 0.7f ? 3 : countT < 1.4f ? 2 : countT < 2.1f ? 1 : 0;
            if (step != countStep)
            {
                countStep = step;
                shell.ShowCountdown(step > 0 ? step.ToString() : Loc.Get("shell.go"));
            }
            Follow();
            shell.SetHud("0.0 m", Loc.Get("hud.pull"));
            shell.SetMeter(1f);
            if (countT > 2.5f)
            {
                shell.ShowCountdown("");
                fsm.Change(playing);
            }
        }

        void TickPlaying()
        {
            float dt = Time.deltaTime;
            float viscosity = slime.Look.Viscosity;
            float slide = (config.climbSlide + stuckTime * config.climbSlideGain) * Mathf.Lerp(1.25f, 0.68f, viscosity);
            acid += (config.climbAcid + maxHeight * config.climbAcidGain) * dt;

            if (stuck)
            {
                stuckTime += dt;
                anchor.y -= slide * dt;
                ReadAim();
            }

            if (stuck)
            {
                slime.Body.position = anchor;
                slime.transform.position = anchor;
                float sway = Mathf.Sin(Time.time * 5.5f) * 0.03f;
                slime.transform.rotation = Quaternion.Euler(0f, 0f, sideNormal.x * (-6f + sway * 40f) + pull.y * -8f);
                if (pull.sqrMagnitude > 0.04f)
                {
                    float power = pull.magnitude / config.climbMaxPull;
                    slime.Deformer.SetFlow(pull);
                    slime.Deformer.Nudge(pull * 4.5f + Vector3.down * slide);
                    if (ServiceLocator.TryGet<IAudioService>(out var audio))
                        audio.SetStretchLoop(true, Mathf.Lerp(0.75f, 1.45f, power));
                }
                else
                {
                    slime.Deformer.SetFlow(Vector3.zero);
                    slime.Deformer.Nudge(Vector3.down * slide);
                    slime.ClearGameplayStretch();
                    if (ServiceLocator.TryGet<IAudioService>(out var audio))
                        audio.SetStretchLoop(false, 1f);
                }
            }
            else if (ServiceLocator.TryGet<IAudioService>(out var audio))
            {
                audio.SetStretchLoop(false, 1f);
            }

            float height = stuck ? anchor.y : slime.transform.position.y;
            maxHeight = Mathf.Max(maxHeight, height);
            if (height - 0.42f <= acid)
                fsm.Change(gameOver);

            UpdateAcidVisual();
            FadeGoo();
            Follow();
            float gap = Mathf.Max(0f, height - acid);
            shell.SetHud(maxHeight.ToString("0.0") + " m", Loc.Format("hud.acid", gap.ToString("0.0")));
            shell.SetMeter(Mathf.Clamp01(gap / 3.2f));
            ShowAim();
        }

        void ReadAim()
        {
            if (!ServiceLocator.TryGet<TouchInputService>(out var input))
                return;
            PointerState pointer = input.State;
            if (pointer.OverUi)
            {
                if (pointer.Ended)
                    ReleaseAim(false);
                return;
            }

            if (pointer.Began)
                aiming = true;
            if (!aiming || !pointer.Held)
            {
                if (pointer.Ended && aiming)
                    ReleaseAim(true);
                else if (!pointer.Held)
                    pull = Vector3.zero;
                return;
            }

            Vector3 world = ScreenToPlane(pointer.Position);
            Vector3 delta = world - anchor;
            delta.z = 0f;
            float outward = Vector3.Dot(delta, sideNormal);
            if (outward < 0.08f)
                delta += sideNormal * (0.08f - outward);
            if (delta.magnitude > config.climbMaxPull)
                delta = delta.normalized * config.climbMaxPull;
            pull = delta;

            if (pointer.Ended)
                ReleaseAim(true);
        }

        void ReleaseAim(bool launch)
        {
            aiming = false;
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.SetStretchLoop(false, 1f);
            if (!launch || pull.magnitude < 0.28f)
            {
                pull = Vector3.zero;
                slime.Deformer.SetFlow(Vector3.zero);
                slime.ClearGameplayStretch();
                return;
            }

            Vector3 velocity = pull * config.climbPower;
            velocity.z = 0f;
            Vector3 launched = pull;
            stuck = false;
            flying = true;
            ignoreUntil = Time.time + 0.12f;
            slime.Body.isKinematic = false;
            slime.Body.position = anchor + launched;
            slime.transform.position = anchor + launched;
            slime.Body.linearVelocity = velocity;
            slime.ClearGameplayStretch();
            slime.transform.rotation = Quaternion.identity;
            slime.Deformer.SnapFlow(-launched * 1.05f);
            slime.Deformer.Nudge(velocity);
            slime.Deformer.Pulse(1.5f);
            pull = Vector3.zero;
            if (ServiceLocator.TryGet<IAudioService>(out var pop))
                pop.Play(Sfx.Pop, Mathf.Lerp(1.3f, 0.8f, slime.Look.Viscosity));
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Light();
        }

        public void OnWallHit(Collision collision)
        {
            if (!flying || Time.time < ignoreUntil)
                return;
            if (collision.collider.GetComponent<ClimbWall>() == null || collision.contactCount == 0)
                return;
            ContactPoint contact = collision.GetContact(0);
            StickAt(contact.point + contact.normal * 0.42f, contact.normal, true);
        }

        void StickAt(Vector3 point, Vector3 normal, bool juice)
        {
            stuck = true;
            flying = false;
            aiming = false;
            stuckTime = 0f;
            pull = Vector3.zero;
            anchor = point;
            anchor.z = 0f;
            sideNormal = new Vector3(Mathf.Sign(normal.x == 0f ? 1f : normal.x), 0f, 0f);
            slime.Body.linearVelocity = Vector3.zero;
            slime.Body.isKinematic = true;
            slime.Body.position = anchor;
            slime.transform.position = anchor;
            slime.ClearGameplayStretch();
            slime.transform.rotation = Quaternion.identity;
            slime.Deformer.SetFlow(Vector3.zero);
            if (!juice)
                return;
            slime.Deformer.SnapFlow(-normal * 0.55f);
            slime.Deformer.Impact(point, -normal, 2.2f);
            slime.AddImpactFlatten(-normal, 0.22f);
            slime.Deformer.Pulse(0.8f);
            SpawnGoo();
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Slap, Mathf.Lerp(1.15f, 0.8f, slime.Look.Viscosity));
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Medium();
        }

        void SpawnGoo()
        {
            Transform patch = goo[gooCursor];
            gooCursor = (gooCursor + 1) % goo.Count;
            patch.gameObject.SetActive(true);
            patch.position = new Vector3(anchor.x - sideNormal.x * 0.18f, anchor.y, -0.05f);
            patch.localScale = new Vector3(0.62f, 0.34f, 0.28f);
            var renderer = patch.GetComponent<MeshRenderer>();
            Color color = slime.Look.Base;
            color.a = 0.5f;
            if (renderer.sharedMaterial != null)
            {
                var instance = renderer.material;
                instance.SetColor("_BaseColor", color);
            }
        }

        void FadeGoo()
        {
            for (int i = 0; i < goo.Count; i++)
            {
                if (!goo[i].gameObject.activeSelf)
                    continue;
                Vector3 scale = goo[i].localScale;
                scale.y = Mathf.MoveTowards(scale.y, 0.7f, Time.deltaTime * 0.05f);
                goo[i].localScale = scale;
            }
        }

        void UpdateAcidVisual()
        {
            if (acidView == null)
                return;
            float height = Mathf.Max(0.2f, acid + 6f);
            acidView.localScale = new Vector3(config.climbHalfWidth * 2f, height, 0.4f);
            acidView.position = new Vector3(0f, acid - height * 0.5f + 0.2f, 0.3f);
        }

        void Follow()
        {
            float y = stuck ? anchor.y : slime.transform.position.y;
            float focus = y + 1.3f;
            cam.transform.position = new Vector3(0f, focus, -12f);
            if (backdrop != null)
                backdrop.position = new Vector3(0f, focus, 3f);
        }

        void EnsureAim()
        {
            if (aimDots != null)
                return;
            Color color = slime != null ? slime.Look.Rim : new Color(0.85f, 1f, 0.7f, 1f);
            if (color.maxColorComponent < 0.35f)
                color = new Color(0.85f, 1f, 0.72f, 1f);
            color = Color.Lerp(color, new Color(0.92f, 1f, 0.78f, 1f), 0.35f);
            color.a = 0.92f;
            aimMaterial = SlimeMaterials.CreateUnlit(color, false, 0.65f);
            aimDots = new Transform[AimCount];
            Mesh mesh = SlimeMeshFactory.SharedBlob(0, 0.5f);
            for (int i = 0; i < AimCount; i++)
            {
                var dot = new GameObject("AimDot");
                var filter = dot.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = dot.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = aimMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                dot.SetActive(false);
                aimDots[i] = dot.transform;
            }
        }

        void HideAim()
        {
            if (aimDots == null)
                return;
            for (int i = 0; i < aimDots.Length; i++)
            {
                if (aimDots[i] != null)
                    aimDots[i].gameObject.SetActive(false);
            }
        }

        void ShowAim()
        {
            bool show = stuck && aiming && pull.magnitude >= 0.28f;
            if (!show)
            {
                HideAim();
                return;
            }

            EnsureAim();
            Vector3 position = anchor + pull;
            Vector3 velocity = pull * config.climbPower;
            velocity.z = 0f;
            Vector3 gravity = new Vector3(0f, -config.climbGravity, 0f);
            const float step = 0.05f;
            const float damp = 0.15f;
            float inset = 0.42f;
            float minX = -config.climbHalfWidth + inset;
            float maxX = config.climbHalfWidth - inset;
            float floor = acid + 0.3f;
            int shown = 0;
            for (int n = 0; n < 40 && shown < aimDots.Length; n++)
            {
                velocity += gravity * step;
                velocity /= 1f + damp * step;
                position += velocity * step;
                if (position.x < minX || position.x > maxX || position.y <= floor)
                    break;
                Transform dot = aimDots[shown];
                dot.gameObject.SetActive(true);
                float size = Mathf.Lerp(0.2f, 0.09f, shown / (float)(aimDots.Length - 1));
                dot.position = new Vector3(position.x, position.y, -0.2f);
                dot.localScale = Vector3.one * size;
                shown++;
            }

            for (int i = shown; i < aimDots.Length; i++)
                aimDots[i].gameObject.SetActive(false);
        }

        Vector3 ScreenToPlane(Vector2 screen)
        {
            Ray ray = cam.ScreenPointToRay(screen);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);
            return anchor;
        }

        void EnterOver()
        {
            overTime = 0f;
            resolved = false;
            flying = false;
            if (slime.Body != null)
            {
                slime.Body.linearVelocity = Vector3.zero;
                slime.Body.isKinematic = true;
            }
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.SetStretchLoop(false, 1f);
            HideAim();
            HitStop.Play(0.1f, 0.42f);
            score = Mathf.RoundToInt(maxHeight * 10f);
            reward = Mathf.Max(2, Mathf.RoundToInt(maxHeight * 2f));
            record = RunRewards.Commit(BoardIds.Climb, score, reward, MissionMetric.ClimbHeight, Mathf.FloorToInt(maxHeight));
        }

        void TickOver()
        {
            overTime += Time.unscaledDeltaTime;
            if (resolved || overTime < 0.42f)
                return;
            resolved = true;
            HitStop.Cancel();
            string body = Loc.Outcome(record, reward, maxHeight.ToString("0.0") + " m");
            shell.ShowGameOver(Loc.Get("over.acid"), body);
            shell.ShowCountdown("");
        }
    }

    public sealed class ClimbRelay : MonoBehaviour
    {
        public StickyClimbGame Game;

        void OnCollisionEnter(Collision collision)
        {
            Game?.OnWallHit(collision);
        }
    }
}
