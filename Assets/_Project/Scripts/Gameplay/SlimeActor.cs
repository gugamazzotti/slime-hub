using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Physics;

namespace SlimeHub.Gameplay
{
    public struct SlimeLook
    {
        public Color Base;
        public Color Rim;
        public float Glitter;
        public float Emission;
        public float Metallic;
        public float Pulse;
        public float Viscosity;
        public CoreKind Core;
    }

    public sealed class SlimeActor : MonoBehaviour
    {
        public SlimeDeformer Deformer { get; private set; }
        public Rigidbody Body { get; private set; }
        public SphereCollider Sphere { get; private set; }
        public SlimeLook Look { get; private set; }

        Material jelly;
        Material glow;
        Transform coreRoot;
        Transform eyes;
        Transform shadow;
        Transform pupilL;
        Transform pupilR;
        Transform[] blinkParts;
        Vector3[] blinkScales;
        Vector3 pupilBaseL;
        Vector3 pupilBaseR;
        Renderer[] innards;
        JellyMote[] motes;
        bool gameplayStretch;
        bool grounded;
        float flatten;
        Vector3 flattenAxis = Vector3.up;
        float groundedUntil;
        float nextImpactSound;
        float blink = -1f;
        float nextBlink = 1.2f;
        int pendingBlinks;
        float moteUntil;
        IAudioService voice;
        IHapticsService haptics;
        static readonly RaycastHit[] groundHits = new RaycastHit[8];

        public static SlimeActor Create(Vector3 position, float radius, int subdivisions, bool physicsBody, bool glowShell)
        {
            var go = new GameObject("Slime");
            go.transform.position = position;
            Mesh mesh = SlimeMeshFactory.CreateUnique(subdivisions, radius);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var deformer = go.AddComponent<SlimeDeformer>();
            deformer.Initialize(mesh, radius);
            var actor = go.AddComponent<SlimeActor>();
            actor.Deformer = deformer;
            actor.jelly = SlimeMaterials.CreateJelly();
            renderer.sharedMaterial = actor.jelly;
            PrepareRenderer(renderer);

            if (glowShell)
            {
                var shell = new GameObject("Glow");
                shell.transform.SetParent(go.transform, false);
                shell.transform.localScale = Vector3.one * 1.04f;
                var shellFilter = shell.AddComponent<MeshFilter>();
                shellFilter.sharedMesh = mesh;
                var shellRenderer = shell.AddComponent<MeshRenderer>();
                actor.glow = SlimeMaterials.CreateGlow();
                shellRenderer.sharedMaterial = actor.glow;
                PrepareRenderer(shellRenderer);
            }

            actor.coreRoot = new GameObject("Core").transform;
            actor.coreRoot.SetParent(go.transform, false);
            actor.BuildEyes(radius);
            actor.BuildInnards(radius);
            if (physicsBody)
                actor.BuildShadow();

            if (physicsBody)
            {
                var body = go.AddComponent<Rigidbody>();
                body.useGravity = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                body.mass = 1.15f;
                body.linearDamping = 2.4f;
                body.angularDamping = 6f;
                var sphere = go.AddComponent<SphereCollider>();
                sphere.radius = radius * 1.02f;
                sphere.material = PhysicsBlob();
                actor.Body = body;
                actor.Sphere = sphere;
            }

            actor.ApplyLook(DefaultLook());
            return actor;
        }

        static void PrepareRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        static PhysicsMaterial PhysicsBlob()
        {
            var material = new PhysicsMaterial("SlimeBody")
            {
                bounciness = 0.04f,
                dynamicFriction = 0.42f,
                staticFriction = 0.5f,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                frictionCombine = PhysicsMaterialCombine.Average
            };
            return material;
        }

        public static SlimeLook DefaultLook()
        {
            return new SlimeLook
            {
                Base = new Color(0.42f, 0.95f, 0.62f, 0.9f),
                Rim = new Color(0.82f, 1f, 0.78f, 1f),
                Emission = 0.12f,
                Viscosity = 0.35f,
                Core = CoreKind.None
            };
        }

        public void ApplyLook(SlimeLook look)
        {
            Look = look;
            Deformer.SetViscosity(look.Viscosity);
            float wobble = 0f;
            SlimeMaterials.ApplyJelly(jelly, look.Base, look.Rim, look.Glitter, look.Emission, look.Metallic, look.Pulse, wobble, Deformer.Radius);
            SlimeMaterials.ApplyGlow(glow, look.Rim, wobble);
            RebuildCore(look.Core);
            TintInnards();
            if (Body != null)
                Body.linearDamping = Mathf.Lerp(1.8f, 6.2f, look.Viscosity);
        }

        public void Boop(Vector3 worldPoint)
        {
            Vector3 normal = worldPoint - transform.position;
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.up;
            normal.Normalize();
            AddImpactFlatten(normal, 0.22f);
            Deformer.Impact(worldPoint, normal, 1.4f);
            if (Body != null)
                Body.WakeUp();
        }

        public void Pull(Vector3 worldVelocity)
        {
            if (worldVelocity.sqrMagnitude < 0.04f || Deformer == null)
                return;
            Deformer.Wake();
        }

        public void ReleaseTouch()
        {
            Deformer?.Pulse(0.8f);
        }

        public void Delight()
        {
            blink = 1f;
            pendingBlinks = 1;
            nextBlink = Time.time + 2.4f;
            moteUntil = Time.time + 0.7f;
            Vector3 axis = Vector3.back;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toward = transform.position - cam.transform.position;
                if (toward.sqrMagnitude > 0.0001f)
                    axis = toward;
            }

            Deformer?.Swirl(axis, 1.55f);
            Deformer?.Pulse(0.65f);
        }

        public void SetGameplayStretch(Vector3 worldAxis, float amount)
        {
            gameplayStretch = true;
            Deformer.SetStretch(worldAxis, amount);
        }

        public void ClearGameplayStretch()
        {
            gameplayStretch = false;
            if (Deformer != null)
                Deformer.SetStretch(Vector3.up, 1f);
        }

        public void AddImpactFlatten(Vector3 worldNormal, float amount)
        {
            if (Deformer == null || amount <= 0f)
                return;
            Deformer.Wake();
            if (worldNormal.sqrMagnitude > 0.0001f)
                flattenAxis = worldNormal.normalized;
            flatten = Mathf.Clamp(Mathf.Max(flatten, amount), 0f, 0.42f);
        }

        void Update()
        {
            Blink();
            RefreshMotes();
            if (Deformer == null || gameplayStretch)
                return;
            StepSpring();
            if (flatten <= 0.001f)
                return;
            float along = Mathf.Lerp(1f, 0.64f, flatten / 0.42f);
            Deformer.SetStretch(flattenAxis, along);
            flatten = Mathf.MoveTowards(flatten, 0f, Time.deltaTime * 1.7f);
            if (flatten <= 0.001f)
                Deformer.SetStretch(Vector3.up, 1f);
        }

        void LateUpdate()
        {
            PlaceShadow();
            LookPupils();
            grounded = Time.time < groundedUntil;
        }

        void StepSpring()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            Deformer.SetSit(grounded ? 1f : 0f);
        }

        void OnCollisionStay(Collision collision)
        {
            if (collision.contactCount == 0)
                return;
            if (collision.GetContact(0).normal.y > 0.55f)
            {
                grounded = true;
                groundedUntil = Time.time + 0.2f;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Deformer == null || collision.contactCount == 0)
                return;
            ContactPoint contact = collision.GetContact(0);
            float magnitude = collision.relativeVelocity.magnitude;
            if (magnitude < 0.45f)
                return;
            Deformer.Impact(contact.point, contact.normal, magnitude);
            AddImpactFlatten(contact.normal, Mathf.Clamp(magnitude * 0.085f, 0.08f, 0.42f));
            if (magnitude < 1.05f || Time.unscaledTime < nextImpactSound)
                return;
            nextImpactSound = Time.unscaledTime + 0.08f;
            ResolveAudio();
            float pitch = Mathf.Lerp(1.25f, 0.72f, Mathf.Clamp01(transform.lossyScale.x));
            voice?.Play(Sfx.Slap, pitch * Mathf.Lerp(1.1f, 0.85f, magnitude / 8f));
            haptics?.Light();
        }

        void ResolveAudio()
        {
            if (voice == null)
                ServiceLocator.TryGet(out voice);
            if (haptics == null)
                ServiceLocator.TryGet(out haptics);
        }

        void BuildEyes(float radius)
        {
            eyes = new GameObject("Eyes").transform;
            eyes.SetParent(transform, false);
            var parts = new System.Collections.Generic.List<Transform>(6);
            float eyeR = radius * 0.155f;
            float pupilSize = radius * 0.072f;
            float gap = radius * 0.17f;
            float y = radius * 0.03f;
            float whiteZ = eyeR * 0.28f;
            float pupilZ = whiteZ - eyeR + pupilSize * 0.38f;
            float glintZ = pupilZ - pupilSize * 0.55f;
            parts.Add(Eye("WhiteL", new Vector3(-gap, y, whiteZ), eyeR, new Color(0.96f, 1f, 0.98f, 1f), 2450));
            parts.Add(Eye("WhiteR", new Vector3(gap, y, whiteZ), eyeR, new Color(0.96f, 1f, 0.98f, 1f), 2450));
            pupilL = Eye("PupilL", new Vector3(-gap, y - radius * 0.012f, pupilZ), pupilSize, new Color(0.05f, 0.07f, 0.09f, 1f), 2460);
            pupilR = Eye("PupilR", new Vector3(gap, y - radius * 0.012f, pupilZ), pupilSize, new Color(0.05f, 0.07f, 0.09f, 1f), 2460);
            parts.Add(pupilL);
            parts.Add(pupilR);
            parts.Add(Eye("GlintL", new Vector3(-gap + radius * 0.035f, y + radius * 0.04f, glintZ), radius * 0.026f, Color.white, 2470));
            parts.Add(Eye("GlintR", new Vector3(gap + radius * 0.035f, y + radius * 0.04f, glintZ), radius * 0.026f, Color.white, 2470));
            blinkParts = parts.ToArray();
            blinkScales = new Vector3[blinkParts.Length];
            for (int i = 0; i < blinkParts.Length; i++)
                blinkScales[i] = blinkParts[i].localScale;
            pupilBaseL = pupilL.localPosition;
            pupilBaseR = pupilR.localPosition;
        }

        Transform Eye(string name, Vector3 localPos, float radius, Color color, int queue)
        {
            MeshRenderer renderer = CreateSphere(eyes, name, localPos, radius, color, true);
            renderer.sharedMaterial.renderQueue = queue;
            return renderer.transform;
        }

        void BuildInnards(float radius)
        {
            var root = new GameObject("Innards").transform;
            root.SetParent(transform, false);
            innards = new Renderer[5];
            for (int i = 0; i < innards.Length; i++)
            {
                float angle = i * 1.2566f;
                Vector3 local = new Vector3(Mathf.Cos(angle), (i - 2) * 0.16f, Mathf.Sin(angle)) * radius * 0.32f;
                Color bubble = new Color(1f, 1f, 1f, 0.42f);
                MeshRenderer renderer = CreateSphere(root, "Mote" + i, local, radius * 0.07f, bubble, false);
                renderer.sharedMaterial.renderQueue = 2980;
                innards[i] = renderer;
                var mote = renderer.gameObject.AddComponent<JellyMote>();
                if (motes == null)
                    motes = new JellyMote[innards.Length];
                motes[i] = mote;
                mote.Radius = radius;
                mote.Speed = 0.65f + i * 0.12f;
                mote.Seed = i * 1.37f;
            }
        }

        void BuildShadow()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = "BlobShadow";
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SlimeMaterials.CreateUnlit(new Color(0.01f, 0.04f, 0.035f, 0.42f), false);
            PrepareRenderer(renderer);
            shadow = go.transform;
            shadow.gameObject.SetActive(false);
        }

        void Blink()
        {
            if (blinkParts == null)
                return;
            if (blink < 0f && Time.time > nextBlink)
            {
                blink = 1f;
                nextBlink = Time.time + Random.Range(2.2f, 5.4f);
            }

            float open = 1f;
            if (blink >= 0f)
            {
                open = Mathf.Lerp(0.08f, 1f, Mathf.Abs(blink - 0.5f) * 2f);
                blink -= Time.unscaledDeltaTime * 7.2f;
                if (blink < 0f && pendingBlinks > 0)
                {
                    pendingBlinks--;
                    blink = 1f;
                }
            }

            for (int i = 0; i < blinkParts.Length; i++)
            {
                if (blinkParts[i] == null)
                    continue;
                Vector3 scale = blinkScales[i];
                scale.y *= open;
                blinkParts[i].localScale = scale;
            }
        }

        void RefreshMotes()
        {
            if (motes == null)
                return;
            float excite = Time.time < moteUntil ? 2.5f : 1f;
            for (int i = 0; i < motes.Length; i++)
            {
                if (motes[i] != null)
                    motes[i].Excitement = excite;
            }
        }

        void LookPupils()
        {
            if (pupilL == null || Deformer == null || eyes == null)
                return;
            Camera cam = Camera.main;
            Vector3 toCam = Vector3.back;
            Vector3 up = Vector3.up;
            if (cam != null)
            {
                Vector3 local = transform.InverseTransformPoint(cam.transform.position);
                if (local.sqrMagnitude > 0.0001f)
                    toCam = local.normalized;
                up = transform.InverseTransformDirection(cam.transform.up);
                if (up.sqrMagnitude < 0.0001f)
                    up = Vector3.up;
                up.Normalize();
            }

            Vector3 forward = -toCam;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.98f)
                up = Mathf.Abs(forward.y) < 0.9f ? Vector3.up : Vector3.right;
            eyes.localRotation = Quaternion.LookRotation(forward, up);
            eyes.localPosition = Deformer.EyeAnchor;

            Vector3 glance = Vector3.zero;
            if (cam != null)
            {
                glance = eyes.InverseTransformPoint(cam.transform.position);
                glance.z = 0f;
                float max = Deformer.Radius * 0.03f;
                if (glance.sqrMagnitude > max * max)
                    glance = glance.normalized * max;
            }

            pupilL.localPosition = pupilBaseL + glance;
            pupilR.localPosition = pupilBaseR + glance;
        }

        void PlaceShadow()
        {
            if (shadow == null || Deformer == null)
                return;

            Vector3 origin = transform.position;
            int count = UnityEngine.Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, Deformer.Radius * 4f);
            float best = float.MaxValue;
            Vector3 point = origin;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider hit = groundHits[i].collider;
                if (hit == null || hit == Sphere || hit.transform.IsChildOf(transform))
                    continue;
                if (groundHits[i].distance >= best)
                    continue;
                best = groundHits[i].distance;
                point = groundHits[i].point;
                found = true;
            }

            shadow.gameObject.SetActive(found);
            if (!found)
                return;

            float spread = Deformer.Radius * Mathf.Max(0.2f, transform.lossyScale.x);
            spread *= Mathf.Lerp(0.9f, 1.45f, Deformer.Sit);
            shadow.SetPositionAndRotation(point + Vector3.up * 0.02f, Quaternion.Euler(90f, 0f, 0f));
            shadow.localScale = new Vector3(spread * 2.4f, spread * 2.4f, 1f);
        }

        void TintInnards()
        {
            if (innards == null)
                return;
            Color color = Color.Lerp(Look.Base, Color.white, 0.55f);
            color.a = 0.4f;
            for (int i = 0; i < innards.Length; i++)
            {
                if (innards[i] != null && innards[i].sharedMaterial != null)
                    innards[i].sharedMaterial.SetColor("_BaseColor", color);
            }
        }

        void RebuildCore(CoreKind kind)
        {
            for (int i = coreRoot.childCount - 1; i >= 0; i--)
                Destroy(coreRoot.GetChild(i).gameObject);

            var spin = coreRoot.GetComponent<CoreSpin>();
            if (kind == CoreKind.Galaxy)
            {
                if (spin == null)
                    spin = coreRoot.gameObject.AddComponent<CoreSpin>();
                spin.DegreesPerSecond = 40f;
            }
            else if (spin != null)
            {
                spin.DegreesPerSecond = kind == CoreKind.Duck ? 18f : 12f;
            }

            float scale = Deformer != null ? Deformer.Radius : 0.5f;
            switch (kind)
            {
                case CoreKind.Duck:
                    BuildDuck(scale);
                    break;
                case CoreKind.Skull:
                    BuildSkull(scale);
                    break;
                case CoreKind.Galaxy:
                    BuildGalaxy(scale);
                    break;
            }
        }

        void BuildDuck(float radius)
        {
            CreateSphere(coreRoot, "Body", new Vector3(0f, -0.05f, 0f) * radius, radius * 0.28f, new Color(1f, 0.86f, 0.2f, 1f), true);
            CreateSphere(coreRoot, "Head", new Vector3(0f, 0.18f, -0.08f) * radius, radius * 0.18f, new Color(1f, 0.9f, 0.35f, 1f), true);
            CreateBox(coreRoot, "Beak", new Vector3(0f, 0.16f, -0.28f) * radius, new Vector3(radius * 0.16f, radius * 0.08f, radius * 0.18f), new Color(1f, 0.45f, 0.12f, 1f));
        }

        void BuildSkull(float radius)
        {
            CreateSphere(coreRoot, "Cranium", Vector3.zero, radius * 0.3f, new Color(0.92f, 0.9f, 0.82f, 1f), true);
            CreateSphere(coreRoot, "SocketL", new Vector3(-0.1f, 0.04f, -0.18f) * radius, radius * 0.08f, new Color(0.12f, 0.08f, 0.1f, 1f), true);
            CreateSphere(coreRoot, "SocketR", new Vector3(0.1f, 0.04f, -0.18f) * radius, radius * 0.08f, new Color(0.12f, 0.08f, 0.1f, 1f), true);
            CreateBox(coreRoot, "Jaw", new Vector3(0f, -0.16f, -0.02f) * radius, new Vector3(radius * 0.22f, radius * 0.08f, radius * 0.16f), new Color(0.85f, 0.82f, 0.74f, 1f));
        }

        void BuildGalaxy(float radius)
        {
            Color a = new Color(0.45f, 0.7f, 1f, 1f);
            Color b = new Color(1f, 0.55f, 0.9f, 1f);
            Color c = new Color(0.7f, 1f, 0.85f, 1f);
            CreateSphere(coreRoot, "Star", Vector3.zero, radius * 0.08f, Color.white, true);
            var arm = new GameObject("Arm").transform;
            arm.SetParent(coreRoot, false);
            CreateSphere(arm, "P1", new Vector3(0.22f, 0.02f, 0f) * radius, radius * 0.06f, a, true);
            CreateSphere(arm, "P2", new Vector3(-0.16f, 0.08f, 0.12f) * radius, radius * 0.05f, b, true);
            CreateSphere(arm, "P3", new Vector3(0.02f, -0.05f, -0.24f) * radius, radius * 0.045f, c, true);
        }

        static MeshRenderer CreateSphere(Transform parent, string name, Vector3 localPos, float radius, Color color, bool zWrite = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * (radius * 2f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SlimeMaterials.CreateUnlit(color, zWrite);
            PrepareRenderer(renderer);
            return renderer;
        }

        static void CreateBox(Transform parent, string name, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SlimeMaterials.CreateUnlit(color, true);
            PrepareRenderer(renderer);
        }

        void OnDestroy()
        {
            if (jelly != null)
                Destroy(jelly);
            if (glow != null)
                Destroy(glow);
            if (shadow != null)
                Destroy(shadow.gameObject);
        }
    }

    public sealed class JellyMote : MonoBehaviour
    {
        public float Radius = 0.4f;
        public float Speed = 1f;
        public float Seed;
        public float Excitement = 1f;

        Vector3 origin;

        void Awake()
        {
            origin = transform.localPosition;
        }

        void Update()
        {
            float t = Time.time * Speed * Mathf.Max(0.2f, Excitement) + Seed;
            Vector3 p = origin;
            p.y = Mathf.PingPong(origin.y + t * Radius * 0.22f, Radius * 0.7f) - Radius * 0.28f;
            p.x += Mathf.Sin(t * 1.4f) * Radius * 0.16f;
            p.z += Mathf.Cos(t * 0.95f) * Radius * 0.12f;
            float limit = Radius * 0.58f;
            if (p.sqrMagnitude > limit * limit)
                p = p.normalized * limit;
            transform.localPosition = p;
        }
    }

    public sealed class CoreSpin : MonoBehaviour
    {
        public float DegreesPerSecond = 20f;

        void Update()
        {
            transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, 12f * Time.deltaTime, Space.Self);
        }
    }
}
