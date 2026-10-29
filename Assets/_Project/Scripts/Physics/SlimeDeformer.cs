using System.Collections.Generic;
using UnityEngine;

namespace SlimeHub.Physics
{
    /// <summary>
    /// Fluido viscoso: partículas com pressão, viscosidade e tensão superficial.
    /// A malha é uma superfície só, que afina, rompe e volta a grudar.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class SlimeDeformer : MonoBehaviour
    {
        public float ContainerRadius;

        const float Iso = 0.18f;

        Mesh mesh;
        Vector3[] pos;
        Vector3[] vel;
        Vector3[] shown;
        Vector3[] sharedVel;
        int[] lobeMark;
        int[] lobeStack;
        float[] grab;
        float[] field;
        int[] cellVertex;
        List<Vector3> verts;
        List<Vector3> normals;
        List<int> tris;
        int count;
        int res;
        float radius = 0.5f;
        float spacing = 0.2f;
        float blobR = 0.35f;
        float restDist = 0.18f;
        float linkDist = 0.45f;
        float damping = 2.5f;
        float viscosity = 0.35f;
        float extent;
        float cell;
        Vector3 origin;
        float sit;
        float sitTarget;
        Vector3 stretchAxis = Vector3.up;
        float stretch = 1f;
        Vector3 flow;
        Vector3 flowTarget;
        Vector3 flowRest;
        bool flowHeld;
        Vector3 motionVel;
        float motionUntil;
        float restExtent;
        float baseBlob;
        float shell;
        float fluidSpan;
        float sleepTimer;
        float swirlUntil;
        float swirlDuration;
        float swirlPower;
        Vector3 swirlAxis;
        bool sleeping;
        bool bodySearched;
        Rigidbody body;
        Vector3 lastVel;
        float grabLeft;
        public Vector3 FaceLocal { get; private set; }
        public Vector3 EyeAnchor { get; private set; }

        public float Radius => radius;
        public bool Sleeping => sleeping;
        public float Sit => sit;

        public void Initialize(Mesh source, float sourceRadius)
        {
            mesh = source;
            radius = Mathf.Max(0.08f, sourceRadius);
            int hint = source != null ? source.vertexCount : 0;
            if (hint > 400)
            {
                count = 64;
                res = 26;
            }
            else if (hint > 100)
            {
                count = 40;
                res = 24;
            }
            else
            {
                count = 24;
                res = 16;
            }

            if (Core.AppQuality.LowMemory)
            {
                count = Mathf.Min(count, 28);
                res = Mathf.Min(res, 16);
            }

            pos = new Vector3[count];
            vel = new Vector3[count];
            shown = new Vector3[count];
            sharedVel = new Vector3[count];
            lobeMark = new int[count];
            lobeStack = new int[count];
            grab = new float[count];
            field = new float[res * res * res];
            int cells = res - 1;
            cellVertex = new int[cells * cells * cells];
            verts = new List<Vector3>(2400);
            normals = new List<Vector3>(2400);
            tris = new List<int>(4800);
            extent = radius * 2.45f;
            origin = Vector3.one * -extent;
            cell = (extent * 2f) / (res - 1);
            spacing = radius * 0.3f * Mathf.Pow(48f / count, 1f / 3f);
            blobR = spacing * 1.95f;
            restDist = spacing * 0.88f;
            restExtent = extent;
            baseBlob = blobR;
            shell = radius * 1.15f;
            ApplyViscosity();
            Seed();
            if (mesh != null)
            {
                mesh.Clear();
                mesh.MarkDynamic();
            }

            for (int i = 0; i < 24; i++)
                Step(0.016f, false);
            for (int i = 0; i < count; i++)
            {
                vel[i] = Vector3.zero;
                shown[i] = pos[i];
            }
            Rebuild();
            sleeping = true;
        }

        public void SetViscosity(float value)
        {
            viscosity = Mathf.Clamp01(value);
            ApplyViscosity();
            Wake();
        }

        public void SetSit(float amount)
        {
            amount = Mathf.Clamp01(amount);
            sitTarget = amount;
        }

        public void SetStretch(Vector3 worldAxis, float amount)
        {
            Vector3 local = transform.InverseTransformDirection(worldAxis);
            if (local.sqrMagnitude < 0.0001f)
            {
                stretchAxis = Vector3.up;
                stretch = 1f;
                return;
            }

            stretchAxis = local.normalized;
            stretch = Mathf.Clamp(amount, 0.45f, 1.9f);
            if (Mathf.Abs(stretch - 1f) > 0.03f)
                Wake();
        }

        public void SetFlow(Vector3 worldOffset)
        {
            flowTarget = ClampLocal(transform.InverseTransformVector(worldOffset), radius * 6.2f);
            flowHeld = flowTarget.sqrMagnitude > 0.0001f;
            flowRest = Vector3.zero;
            Wake();
        }

        public void Nudge(Vector3 worldVelocity)
        {
            motionVel = worldVelocity;
            motionUntil = Time.time + 0.12f;
            if (worldVelocity.sqrMagnitude > 0.2f)
                Wake();
        }

        public void SnapFlow(Vector3 worldOffset)
        {
            flow = ClampLocal(transform.InverseTransformVector(worldOffset), radius * 6.2f);
            flowTarget = Vector3.zero;
            flowRest = Vector3.zero;
            flowHeld = false;
            Wake();
        }

        public void PushFlow(Vector3 worldKick, Vector3 worldRest)
        {
            if (flowHeld)
                return;
            flowRest = ClampLocal(transform.InverseTransformVector(worldRest), radius * 0.55f);
            flowTarget += transform.InverseTransformVector(worldKick);
            flowTarget = ClampLocal(flowTarget, radius * 1.9f);
            Wake();
        }

        static Vector3 ClampLocal(Vector3 local, float max)
        {
            if (local.sqrMagnitude <= max * max)
                return local;
            return local.normalized * max;
        }

        public void Wake()
        {
            sleeping = false;
            sleepTimer = 0f;
        }

        public void Poke(Vector3 worldPoint, Vector3 worldVelocity, float worldRadius)
        {
            if (count == 0)
                return;
            Wake();
            float scale = Mathf.Max(0.001f, transform.lossyScale.x);
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector3 localVelocity = transform.InverseTransformVector(worldVelocity);
            float speed = localVelocity.magnitude;
            float maxSpeed = radius * 11f;
            if (speed > maxSpeed)
                localVelocity *= maxSpeed / speed;

            float carryCap = radius * Mathf.Lerp(6f, 2.4f, viscosity);
            if (speed > carryCap)
                localVelocity *= carryCap / speed;

            float reach = Mathf.Clamp(worldRadius / scale, radius * 0.34f, radius * 0.58f);
            float soft = reach * 2.15f;
            float reachSq = reach * reach;
            float softSq = soft * soft;
            float follow = Mathf.Lerp(0.5f, 0.22f, viscosity);
            float cap = radius * Mathf.Lerp(0.14f, 0.05f, viscosity);
            float stick = Mathf.Lerp(0.28f, 0.7f, viscosity);
            for (int i = 0; i < count; i++)
            {
                float distSq = (pos[i] - localPoint).sqrMagnitude;
                if (distSq > softSq)
                    continue;
                float dist = Mathf.Sqrt(distSq);
                if (dist < reach)
                {
                    float weight = 1f - dist / reach;
                    weight *= weight;
                    Vector3 toFinger = localPoint - pos[i];
                    pos[i] += Vector3.ClampMagnitude(toFinger * (follow * weight), cap);
                    vel[i] = Vector3.Lerp(vel[i], localVelocity * (stick * weight), 0.4f);
                    grab[i] = 0.36f;
                }
                else
                {
                    float weight = 1f - (dist - reach) / Mathf.Max(0.0001f, soft - reach);
                    weight *= weight;
                    vel[i] = Vector3.Lerp(vel[i], localVelocity * (stick * 0.42f * weight), 0.28f);
                    grab[i] = Mathf.Max(grab[i], 0.18f * weight);
                }
            }

            grabLeft = 0.36f;
        }

        public void Impact(Vector3 worldPoint, Vector3 worldNormal, float strength)
        {
            if (count == 0)
                return;
            Wake();
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector3 localDir = transform.InverseTransformDirection(-worldNormal);
            if (localDir.sqrMagnitude < 0.0001f)
                localDir = Vector3.down;
            localDir.Normalize();
            float reach = radius * 0.95f;
            float reachSq = reach * reach;
            float power = Mathf.Clamp(strength, 0f, 8f) * Mathf.Lerp(1.35f, 0.4f, viscosity);
            float spread = power * Mathf.Lerp(0.62f, 0.2f, viscosity);
            for (int i = 0; i < count; i++)
            {
                float distSq = (pos[i] - localPoint).sqrMagnitude;
                if (distSq > reachSq)
                    continue;
                float weight = 1f - Mathf.Sqrt(distSq) / reach;
                vel[i] += localDir * (power * 0.7f * weight * weight);
                Vector3 offset = pos[i] - localPoint;
                Vector3 tangent = offset - localDir * Vector3.Dot(offset, localDir);
                float tangentLen = tangent.magnitude;
                if (tangentLen > 0.0001f)
                    vel[i] += tangent * (spread * weight / tangentLen);
            }
        }

        public void Swirl(Vector3 worldAxis, float strength)
        {
            if (count == 0)
                return;
            Vector3 axis = transform.InverseTransformDirection(worldAxis);
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.back;
            Wake();
            swirlAxis = axis.normalized;
            swirlPower = Mathf.Clamp(strength, 0.2f, 3f);
            swirlDuration = Mathf.Lerp(0.32f, 0.5f, viscosity);
            swirlUntil = Time.time + swirlDuration;
        }

        public void Pulse(float strength)
        {
            if (count == 0)
                return;
            Wake();
            float kick = strength * radius * Mathf.Lerp(0.45f, 0.16f, viscosity);
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = pos[i].sqrMagnitude > 0.0001f ? pos[i].normalized : Vector3.up;
                vel[i] += dir * kick;
            }
        }

        void LateUpdate()
        {
            if (count == 0 || mesh == null)
                return;

            CacheBody();
            if (body != null && body.linearVelocity.sqrMagnitude > 0.45f)
                Wake();
            if (Mathf.Abs(stretch - 1f) > 0.04f)
                Wake();
            if (flow.sqrMagnitude > 0.0006f || flowTarget.sqrMagnitude > 0.0006f || Time.time < swirlUntil)
                Wake();

            if (sleeping)
                return;

            float dt = Mathf.Min(Time.deltaTime, 0.033f);
            if (dt <= 0f)
                return;

            Step(dt, true);
            Rebuild();

            float energy = 0f;
            for (int i = 0; i < count; i++)
                energy += vel[i].sqrMagnitude;
            bool calm = energy < 0.008f && grabLeft <= 0f && Mathf.Abs(stretch - 1f) < 0.04f
                && flow.sqrMagnitude < 0.0008f && flowTarget.sqrMagnitude < 0.0008f
                && Time.time >= swirlUntil;
            if (body != null && body.linearVelocity.sqrMagnitude > 0.45f)
                calm = false;
            sleepTimer = calm ? sleepTimer + dt : 0f;
            if (sleepTimer > 0.35f)
                sleeping = true;
        }

        void Step(float h, bool useBody)
        {
            sit = Mathf.Lerp(sit, sitTarget, 1f - Mathf.Exp(-3f * h));
            float follow = Mathf.Lerp(18f, 8f, viscosity);
            float recover = Mathf.Lerp(3.1f, 1.25f, viscosity);
            if (!flowHeld)
                flowTarget = Vector3.Lerp(flowTarget, flowRest, 1f - Mathf.Exp(-recover * h));
            float ease = flowHeld ? follow : recover;
            flow = Vector3.Lerp(flow, flowTarget, 1f - Mathf.Exp(-ease * h));
            Vector3 slosh = Vector3.zero;
            bool physical = useBody && body != null && !body.isKinematic;
            bool carried = !physical && Time.time < motionUntil;
            if (physical || carried)
            {
                Vector3 worldVel = physical ? body.linearVelocity : motionVel;
                if (worldVel.sqrMagnitude > 0.35f || lastVel.sqrMagnitude > 0.35f)
                {
                    Vector3 smoothed = Vector3.Lerp(lastVel, worldVel, 0.18f);
                    Vector3 accel = (smoothed - lastVel) / Mathf.Max(h, 0.008f);
                    lastVel = smoothed;
                    if (accel.sqrMagnitude > 36f)
                        accel = accel.normalized * 6f;
                    slosh = transform.InverseTransformVector(accel) * 0.14f;
                    if (slosh.sqrMagnitude > radius * radius * 9f)
                        slosh = slosh.normalized * radius * 3f;
                }
                else
                {
                    lastVel = Vector3.Lerp(lastVel, worldVel, 0.2f);
                }
            }
            else if (body != null)
            {
                lastVel = Vector3.Lerp(lastVel, body.linearVelocity, 0.2f);
            }

            Viscous();
            float keep = Mathf.Exp(-Mathf.Max(6f, damping) * h);
            float flowLen = flow.magnitude;
            Vector3 flowDir = flowLen > 1e-5f ? flow / flowLen : Vector3.zero;
            float flowSpan = Mathf.Clamp01(flowLen / (radius * 1.15f));
            float flowAccel = flowSpan * Mathf.Lerp(22f, 9f, viscosity);
            grabLeft = 0f;
            for (int i = 0; i < count; i++)
            {
                if (grab[i] > 0f)
                    grab[i] -= h;
                if (grab[i] > grabLeft)
                    grabLeft = grab[i];
                vel[i] -= slosh * Mathf.Clamp01((pos[i].y / radius) + 0.35f);
                if (flowSpan > 0.02f)
                {
                    float along = Vector3.Dot(pos[i], flowDir);
                    float front = Mathf.Clamp01(along / radius + 0.2f);
                    float room = 1f - Mathf.Clamp01(along / Mathf.Max(0.05f, shell * 0.92f));
                    vel[i] += flowDir * (flowAccel * front * room * h);
                }

                vel[i] *= keep;
                if (Time.time < swirlUntil && swirlPower > 0.01f)
                {
                    float fade = Mathf.Clamp01((swirlUntil - Time.time) / Mathf.Max(0.05f, swirlDuration));
                    float omega = swirlPower * fade * Mathf.Lerp(4.2f, 1.6f, viscosity);
                    float take = 1f - Mathf.Exp(-7f * h);
                    vel[i] = Vector3.Lerp(vel[i], Vector3.Cross(swirlAxis, pos[i]) * omega, take);
                }
                if (vel[i].sqrMagnitude < 0.0004f)
                    vel[i] = Vector3.zero;
                pos[i] += vel[i] * h;
            }

            Constrain();
            Cohesion(h);
            Ooze(h);
            PullToCenter(h);
            Collide(h);
        }

        void Viscous()
        {
            float near = linkDist;
            float nearSq = near * near;
            float couple = Mathf.Lerp(0.1f, 0.34f, viscosity);
            for (int i = 0; i < count; i++)
            {
                Vector3 acc = Vector3.zero;
                float weightSum = 0f;
                Vector3 pi = pos[i];
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                        continue;
                    float distSq = (pos[j] - pi).sqrMagnitude;
                    if (distSq > nearSq)
                        continue;
                    float weight = 1f - Mathf.Sqrt(distSq) / near;
                    acc += vel[j] * weight;
                    weightSum += weight;
                }

                sharedVel[i] = weightSum > 0.001f
                    ? Vector3.Lerp(vel[i], acc / weightSum, couple)
                    : vel[i];
            }

            for (int i = 0; i < count; i++)
                vel[i] = sharedVel[i];
        }

        void Constrain()
        {
            float near = linkDist * 1.15f;
            float nearSq = near * near;
            float pushLimit = spacing * 0.13f;
            float pullLimit = spacing * Mathf.Lerp(0.045f, 0.15f, viscosity);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 pi = pos[i];
                    for (int j = i + 1; j < count; j++)
                    {
                        Vector3 delta = pos[j] - pi;
                        float distSq = delta.sqrMagnitude;
                        if (distSq > nearSq || distSq < 1e-8f)
                            continue;
                        float dist = Mathf.Sqrt(distSq);
                        Vector3 normal = delta / dist;
                        bool gripped = grab[i] > 0f || grab[j] > 0f;
                        if (dist < restDist)
                        {
                            float push = Mathf.Min((restDist - dist) * 0.42f, pushLimit);
                            pi -= normal * (push * 0.5f);
                            pos[j] += normal * (push * 0.5f);
                        }
                        else if (!gripped)
                        {
                            float pull = Mathf.Min((dist - restDist) * Mathf.Lerp(0.1f, 0.26f, viscosity), pullLimit);
                            pi += normal * (pull * 0.5f);
                            pos[j] -= normal * (pull * 0.5f);
                        }
                    }

                    pos[i] = pi;
                }
            }
        }

        void Cohesion(float h)
        {
            bool calm = grabLeft <= 0.02f
                && fluidSpan < radius * 1.02f
                && flow.sqrMagnitude < radius * radius * 0.008f;
            if (calm)
                return;
            float near = linkDist;
            float nearSq = near * near;
            float gain = Mathf.Lerp(2.2f, 4.8f, viscosity) * h;
            float cap = spacing * 0.07f;
            for (int i = 0; i < count; i++)
            {
                if (grab[i] > 0.05f)
                    continue;
                Vector3 centroid = Vector3.zero;
                int neighbors = 0;
                Vector3 pi = pos[i];
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                        continue;
                    Vector3 delta = pos[j] - pi;
                    if (delta.sqrMagnitude > nearSq)
                        continue;
                    centroid += pos[j];
                    neighbors++;
                }

                float surface = Mathf.Clamp01((6.5f - neighbors) / 5f);
                if (neighbors == 0 || surface <= 0.04f)
                    continue;
                centroid /= neighbors;
                Vector3 to = centroid - pi;
                pos[i] += Vector3.ClampMagnitude(to * (gain * surface), cap);
            }
        }

        void Ooze(float h)
        {
            Vector3 pile = Vector3.zero;
            int pileCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (grab[i] > 0f)
                    continue;
                pile += pos[i];
                pileCount++;
            }

            if (pileCount == 0)
                return;
            pile /= pileCount;
            float strain = Mathf.Clamp01((fluidSpan - radius * 0.82f) / Mathf.Max(0.05f, radius * 0.75f));
            float reel = grabLeft > 0.02f ? 0.2f : Mathf.Lerp(1f, 0.42f, strain);
            if (flow.sqrMagnitude > radius * radius * 0.04f)
                reel *= 0.38f;
            float speed = radius * Mathf.Lerp(0.9f, 0.34f, viscosity) * reel;
            float home = radius * 0.7f;
            const float wide = 1.18f;
            const float tall = 0.66f;
            for (int i = 0; i < count; i++)
            {
                if (grab[i] > 0f)
                    continue;
                Vector3 delta = pos[i] - pile;
                float shaped = Mathf.Sqrt((delta.x * delta.x + delta.z * delta.z) / (wide * wide) + (delta.y * delta.y) / (tall * tall));
                if (shaped <= home || shaped < 1e-4f)
                    continue;
                float step = Mathf.Min(shaped - home, speed * h);
                pos[i] -= delta * (step / shaped);
            }
        }

        void PullToCenter(float h)
        {
            Vector3 com = Vector3.zero;
            for (int i = 0; i < count; i++)
                com += pos[i];
            com /= count;
            float rate = grabLeft > 0f ? 0.45f : 1.8f;
            Vector3 shift = com * (1f - Mathf.Exp(-rate * h));
            for (int i = 0; i < count; i++)
                pos[i] -= shift;
        }

        void Collide(float h)
        {
            bool open = grabLeft > 0.02f || flow.sqrMagnitude > radius * radius * 0.008f;
            float reach = Mathf.Clamp01(flow.magnitude / (radius * 2.2f));
            float grabOpen = radius * Mathf.Lerp(1.55f, 2.05f, viscosity);
            float flowOpen = radius * Mathf.Lerp(1.45f, 3.35f, reach);
            float want = open
                ? Mathf.Max(grabOpen, flowOpen)
                : radius * Mathf.Lerp(1.22f, 1.48f, sit);
            shell = Mathf.Lerp(shell, want, 1f - Mathf.Exp(-2.6f * h));
            float span = fluidSpan;
            ExpandGrid();
            fluidSpan = 0f;
            float floorY = -radius * 0.46f;
            float fence = shell;
            float fenceSq = fence * fence;
            float limit = Mathf.Max(span + radius * 0.2f, fence + radius * 0.28f);
            float flowReach = Mathf.Clamp(flow.magnitude / Mathf.Max(0.05f, radius), 0f, 3.2f);
            float cap = radius * Mathf.Lerp(2.7f, 4.4f, Mathf.Clamp01((flowReach - 0.35f) / 2f));
            limit = Mathf.Min(limit, cap);
            float limitSq = limit * limit;
            bool bowl = ContainerRadius > 0.05f;
            float bowlSq = ContainerRadius * ContainerRadius;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = pos[i];
                if (p.y < floorY)
                {
                    p.y = floorY;
                    if (vel[i].y < 0f)
                        vel[i].y = 0f;
                    vel[i].x *= 0.92f;
                    vel[i].z *= 0.92f;
                }

                float flat = p.x * p.x + p.z * p.z;
                if (flat > fenceSq && grab[i] <= 0f)
                {
                    float dist = Mathf.Sqrt(flat);
                    float step = Mathf.Min(dist - fence, spacing * 0.32f);
                    float keep = (dist - step) / dist;
                    p.x *= keep;
                    p.z *= keep;
                    Vector3 flatDir = new Vector3(p.x, 0f, p.z);
                    float flatLen = flatDir.magnitude;
                    if (flatLen > 0.0001f)
                    {
                        flatDir /= flatLen;
                        float outward = vel[i].x * flatDir.x + vel[i].z * flatDir.z;
                        if (outward > 0f)
                        {
                            vel[i].x -= flatDir.x * outward;
                            vel[i].z -= flatDir.z * outward;
                        }
                    }
                }

                if (bowl && p.sqrMagnitude > bowlSq)
                    p = p.normalized * ContainerRadius;
                if (p.sqrMagnitude > limitSq)
                {
                    p = p.normalized * limit;
                    float outward = Vector3.Dot(vel[i], p);
                    if (outward > 0f)
                        vel[i] -= p * (outward / limitSq);
                }
                pos[i] = p;
                fluidSpan = Mathf.Max(fluidSpan, p.magnitude);
            }
        }

        void Rebuild()
        {
            FitGrid();
            System.Array.Clear(field, 0, field.Length);
            float radiusSq = blobR * blobR;
            int reach = Mathf.Clamp(Mathf.CeilToInt(blobR / cell), 1, res);
            Vector3 face = Vector3.zero;
            float faceWeight = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 target = Display(pos[i]);
                shown[i] = Vector3.Lerp(shown[i], target, 0.66f);
                Vector3 p = shown[i];
                float front = Mathf.Max(0f, -p.z + radius * 0.15f);
                face += p * front;
                faceWeight += front;
                Splat(p, radiusSq, reach);
            }

            FaceLocal = faceWeight > 0.001f
                ? face / faceWeight + new Vector3(0f, radius * 0.02f, -blobR * 0.65f)
                : new Vector3(0f, radius * 0.1f, -radius * 0.75f);
            PlaceEyes();
            SlimeMetaball.Build(field, res, origin, cell, Iso, mesh, verts, normals, tris, cellVertex);
        }

        void Splat(Vector3 p, float radiusSq, int reach)
        {
            Vector3 q = (p - origin) / cell;
            int cx = Mathf.RoundToInt(q.x);
            int cy = Mathf.RoundToInt(q.y);
            int cz = Mathf.RoundToInt(q.z);
            int x0 = Mathf.Max(0, cx - reach);
            int y0 = Mathf.Max(0, cy - reach);
            int z0 = Mathf.Max(0, cz - reach);
            int x1 = Mathf.Min(res - 1, cx + reach);
            int y1 = Mathf.Min(res - 1, cy + reach);
            int z1 = Mathf.Min(res - 1, cz + reach);
            for (int z = z0; z <= z1; z++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    int row = res * (y + res * z);
                    for (int x = x0; x <= x1; x++)
                    {
                        Vector3 sample = origin + new Vector3(x, y, z) * cell;
                        float distSq = (sample - p).sqrMagnitude;
                        if (distSq >= radiusSq)
                            continue;
                        float influence = 1f - distSq / radiusSq;
                        field[x + row] += influence * influence * influence;
                    }
                }
            }
        }

        void FitGrid()
        {
            ExpandGrid();
        }

        void ExpandGrid()
        {
            float stretchExtra = radius * Mathf.Max(0f, Mathf.Abs(stretch) - 1f);
            float extra = Mathf.Max(fluidSpan, shell) + stretchExtra;
            float need = Mathf.Max(restExtent, extra + baseBlob * 1.45f);
            need = Mathf.Min(need, radius * 4.6f);
            extent = Mathf.Lerp(extent, need, 0.55f);
            if (Mathf.Abs(extent - need) < 0.03f)
                extent = need;
            origin = Vector3.one * -extent;
            cell = (extent * 2f) / Mathf.Max(1, res - 1);
            blobR = baseBlob;
        }

        void PlaceEyes()
        {
            Vector3 toCam = Vector3.back;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 local = transform.InverseTransformPoint(cam.transform.position);
                if (local.sqrMagnitude > 0.0001f)
                    toCam = local.normalized;
            }

            if (count == 0 || shown == null)
            {
                EyeAnchor = toCam * radius;
                return;
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < count; i++)
                center += shown[i];

            center = MainBody(toCam, center / count);
            EyeAnchor = ExitSurface(center, toCam);
        }

        Vector3 MainBody(Vector3 toCam, Vector3 fallback)
        {
            if (lobeMark == null || shown == null)
                return fallback;
            System.Array.Clear(lobeMark, 0, count);
            float join = Mathf.Max(blobR * 1.35f, spacing * 2.2f);
            float joinSq = join * join;
            int bestCount = 0;
            float bestFacing = float.NegativeInfinity;
            Vector3 bestCenter = fallback;
            int label = 0;
            for (int seed = 0; seed < count; seed++)
            {
                if (lobeMark[seed] != 0)
                    continue;
                label++;
                int top = 0;
                lobeStack[top++] = seed;
                lobeMark[seed] = label;
                int members = 0;
                Vector3 sum = Vector3.zero;
                while (top > 0)
                {
                    int i = lobeStack[--top];
                    sum += shown[i];
                    members++;
                    Vector3 pi = shown[i];
                    for (int j = 0; j < count; j++)
                    {
                        if (lobeMark[j] != 0)
                            continue;
                        if ((shown[j] - pi).sqrMagnitude > joinSq)
                            continue;
                        lobeMark[j] = label;
                        lobeStack[top++] = j;
                    }
                }

                Vector3 lobeCenter = sum / members;
                float facing = Vector3.Dot(lobeCenter, toCam);
                if (members > bestCount || (members == bestCount && facing > bestFacing))
                {
                    bestCount = members;
                    bestFacing = facing;
                    bestCenter = lobeCenter;
                }
            }

            return bestCenter;
        }

        Vector3 ExitSurface(Vector3 center, Vector3 toCam)
        {
            const int steps = 22;
            float maxDist = radius * 2.5f;
            Vector3 inside = center - toCam * (radius * 0.2f);
            float previous = SampleField(inside);
            Vector3 previousPoint = inside;
            if (previous < Iso)
            {
                inside = center;
                previous = SampleField(inside);
                previousPoint = inside;
            }

            for (int s = 1; s <= steps; s++)
            {
                Vector3 point = inside + toCam * (maxDist * s / (float)steps);
                float density = SampleField(point);
                if (previous >= Iso && density < Iso)
                {
                    float span = previous - density;
                    float blend = span > 1e-5f ? Mathf.Clamp01((previous - Iso) / span) : 0f;
                    Vector3 hit = Vector3.Lerp(previousPoint, point, blend);
                    return hit - toCam * (radius * 0.045f);
                }

                previous = density;
                previousPoint = point;
            }

            return center + toCam * (radius * 0.62f);
        }

        float SampleField(Vector3 point)
        {
            if (field == null || cell < 1e-5f)
                return 0f;
            Vector3 grid = (point - origin) / cell;
            int x0 = Mathf.FloorToInt(grid.x);
            int y0 = Mathf.FloorToInt(grid.y);
            int z0 = Mathf.FloorToInt(grid.z);
            float tx = grid.x - x0;
            float ty = grid.y - y0;
            float tz = grid.z - z0;
            float c000 = FieldAt(x0, y0, z0);
            float c100 = FieldAt(x0 + 1, y0, z0);
            float c010 = FieldAt(x0, y0 + 1, z0);
            float c110 = FieldAt(x0 + 1, y0 + 1, z0);
            float c001 = FieldAt(x0, y0, z0 + 1);
            float c101 = FieldAt(x0 + 1, y0, z0 + 1);
            float c011 = FieldAt(x0, y0 + 1, z0 + 1);
            float c111 = FieldAt(x0 + 1, y0 + 1, z0 + 1);
            float c00 = Mathf.Lerp(c000, c100, tx);
            float c10 = Mathf.Lerp(c010, c110, tx);
            float c01 = Mathf.Lerp(c001, c101, tx);
            float c11 = Mathf.Lerp(c011, c111, tx);
            return Mathf.Lerp(Mathf.Lerp(c00, c10, ty), Mathf.Lerp(c01, c11, ty), tz);
        }

        float FieldAt(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= res || y >= res || z >= res)
                return 0f;
            return field[x + res * (y + res * z)];
        }

        Vector3 Display(Vector3 p)
        {
            if (Mathf.Abs(stretch - 1f) < 0.025f)
                return p;
            float squash = 1f / Mathf.Sqrt(Mathf.Max(0.45f, stretch));
            float alongAxis = Vector3.Dot(p, stretchAxis);
            return stretchAxis * (alongAxis * stretch) + (p - stretchAxis * alongAxis) * squash;
        }

        void Seed()
        {
            float seedRadius = radius * 0.66f;
            for (int i = 0; i < count; i++)
            {
                float u = (i + 0.5f) / count;
                float y = (1f - 2f * u) * 0.78f;
                float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float angle = i * 2.39996323f;
                int scrambled = (i * 17) % count;
                float radial = Mathf.Lerp(0.18f, 1f, scrambled / (float)(count - 1));
                pos[i] = new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring) * (seedRadius * radial);
                vel[i] = Vector3.zero;
            }
        }

        void ApplyViscosity()
        {
            float v = viscosity;
            linkDist = spacing * Mathf.Lerp(2.15f, 3.15f, v);
            damping = Mathf.Lerp(6f, 10f, v);
        }

        void CacheBody()
        {
            if (bodySearched)
                return;
            bodySearched = true;
            body = GetComponent<Rigidbody>();
            if (body != null)
                lastVel = body.linearVelocity;
        }
    }
}
