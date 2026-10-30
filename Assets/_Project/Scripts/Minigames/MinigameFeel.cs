using UnityEngine;
using UnityEngine.Rendering;
using SlimeHub.Core;
using SlimeHub.Gameplay;
using SlimeHub.Physics;

namespace SlimeHub.Minigames
{
    public static class LabPalette
    {
        public static readonly Color Ink = new Color(0.05f, 0.07f, 0.13f, 1f);
        public static readonly Color TubeSky = new Color(0.06f, 0.09f, 0.22f, 1f);
        public static readonly Color TubeGrid = new Color(0.32f, 0.86f, 1f, 1f);
        public static readonly Color CritSky = new Color(0.11f, 0.05f, 0.18f, 1f);
        public static readonly Color CritGrid = new Color(0.86f, 0.48f, 1f, 1f);
        public static readonly Color ClimbSky = new Color(0.04f, 0.09f, 0.14f, 1f);
        public static readonly Color ClimbGrid = new Color(0.55f, 0.95f, 0.48f, 1f);
        public static readonly Color Mint = new Color(0.62f, 1f, 0.74f, 1f);
        public static readonly Color Coral = new Color(1f, 0.42f, 0.36f, 1f);
        public static readonly Color Cream = new Color(0.97f, 0.98f, 0.93f, 1f);
    }

    public static class LabStage
    {
        public static Transform Build(Color sky, Color grid, float width, float height)
        {
            var root = new GameObject("Stage").transform;
            Mesh quad = SlimeMeshFactory.Quad();
            Paint(root, quad, "Sky", Vector3.zero, new Vector3(width, height, 1f), sky, grid, 0.38f, 4f);
            Color wash = grid;
            wash.a = 0.14f;
            Paint(root, quad, "Wash", new Vector3(0f, height * 0.12f, -0.15f), new Vector3(width * 0.62f, height * 0.22f, 1f), wash, grid, 0f, -0.15f);
            int orbs = AppQuality.LowMemory ? 4 : 8;
            Mesh blob = SlimeMeshFactory.SharedBlob(0, 0.5f);
            for (int i = 0; i < orbs; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (width * 0.28f + (i % 3) * 0.35f);
                float y = (i - orbs * 0.5f) * (height / (orbs + 2f));
                float size = 0.35f + (i % 4) * 0.18f;
                var orb = new GameObject("Orb");
                orb.transform.SetParent(root, false);
                orb.transform.localPosition = new Vector3(x, y, -0.35f);
                orb.transform.localScale = Vector3.one * size;
                var filter = orb.AddComponent<MeshFilter>();
                filter.sharedMesh = blob;
                var renderer = orb.AddComponent<MeshRenderer>();
                Color glow = grid;
                glow.a = 0.16f;
                renderer.sharedMaterial = SlimeMaterials.CreateUnlit(glow, false, 0.9f);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var pulse = orb.AddComponent<LabOrb>();
                pulse.Phase = i * 0.7f;
                pulse.BaseScale = size;
            }

            return root;
        }

        static void Paint(Transform parent, Mesh mesh, string name, Vector3 local, Vector3 scale, Color color, Color grid, float gridStrength, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(local.x, local.y, z);
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SlimeMaterials.CreateUnlit(color, true, 0f, gridStrength, grid);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    public sealed class LabOrb : MonoBehaviour
    {
        public float Phase;
        public float BaseScale = 0.4f;
        Vector3 origin;

        void Start()
        {
            origin = transform.localPosition;
        }

        void Update()
        {
            float t = Time.time + Phase;
            transform.localPosition = origin + new Vector3(Mathf.Sin(t * 0.55f) * 0.12f, Mathf.Sin(t * 0.37f + Phase) * 0.18f, 0f);
            float breathe = 1f + Mathf.Sin(t * 1.4f) * 0.1f;
            transform.localScale = Vector3.one * (BaseScale * breathe);
        }
    }

    public sealed class FeelBurst : MonoBehaviour
    {
        const int Pool = 20;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        struct Mote
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Vector3 Velocity;
            public float Life;
            public float Max;
        }

        readonly Mote[] motes = new Mote[Pool];
        Material material;
        bool ready;

        public static FeelBurst Create()
        {
            var go = new GameObject("Juice");
            return go.AddComponent<FeelBurst>();
        }

        void Awake()
        {
            material = SlimeMaterials.CreateUnlit(Color.white, false, 0.85f);
            Mesh mesh = SlimeMeshFactory.SharedBlob(0, 0.5f);
            for (int i = 0; i < Pool; i++)
            {
                var dot = new GameObject("Spark");
                dot.transform.SetParent(transform, false);
                dot.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = dot.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                dot.SetActive(false);
                motes[i].Transform = dot.transform;
                motes[i].Renderer = renderer;
            }

            ready = true;
        }

        public void Pop(Vector3 at, Color color, int count, float speed)
        {
            if (!ready)
                return;
            if (AppQuality.LowMemory)
                count = Mathf.Min(count, 6);
            count = Mathf.Clamp(count, 1, 10);
            color.a = 0.95f;
            int spawned = 0;
            for (int i = 0; i < motes.Length && spawned < count; i++)
            {
                if (motes[i].Life > 0f)
                    continue;
                float ang = (spawned / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float mag = speed * Random.Range(0.65f, 1.15f);
                var block = new MaterialPropertyBlock();
                block.SetColor(ColorId, color);
                block.SetFloat("_Fresnel", 0.85f);
                motes[i].Renderer.SetPropertyBlock(block);
                motes[i].Transform.gameObject.SetActive(true);
                motes[i].Transform.position = at;
                motes[i].Velocity = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * mag;
                motes[i].Max = Random.Range(0.28f, 0.48f);
                motes[i].Life = motes[i].Max;
                spawned++;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            for (int i = 0; i < motes.Length; i++)
            {
                if (motes[i].Life <= 0f)
                    continue;
                motes[i].Life -= dt;
                motes[i].Velocity += Vector3.down * (4.5f * dt);
                motes[i].Transform.position += motes[i].Velocity * dt;
                float u = Mathf.Clamp01(motes[i].Life / Mathf.Max(0.05f, motes[i].Max));
                motes[i].Transform.localScale = Vector3.one * Mathf.Lerp(0.03f, 0.2f, u);
                if (motes[i].Life <= 0f)
                    motes[i].Transform.gameObject.SetActive(false);
            }
        }
    }

    public sealed class RingPulse : MonoBehaviour
    {
        public Vector3 Base = Vector3.one;
        public bool Hazard;
        public bool Hot;

        void Update()
        {
            float speed = Hazard ? (Hot ? 14f : 5f) : 2.4f;
            float amp = Hazard ? (Hot ? 0.18f : 0.06f) : 0.045f;
            float s = 1f + Mathf.Sin(Time.time * speed + transform.localPosition.x * 3f) * amp;
            transform.localScale = Base * s;
        }
    }

    public struct SoftCamera
    {
        public float Shake;
        public float FocusY;

        public void Kick(float amount)
        {
            Shake = Mathf.Clamp(Mathf.Max(Shake, amount), 0f, 0.28f);
        }

        public void Follow(Camera cam, float targetY, float lead, float z)
        {
            float dt = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            FocusY = Mathf.Lerp(FocusY, targetY + lead, 1f - Mathf.Exp(-7f * dt));
            Shake = Mathf.MoveTowards(Shake, 0f, Time.unscaledDeltaTime * 1.7f);
            float mag = Shake * Shake * 4.2f;
            float ox = Mathf.Sin(Time.unscaledTime * 57f) * mag;
            float oy = Mathf.Cos(Time.unscaledTime * 49f) * mag * 0.55f;
            cam.transform.position = new Vector3(ox, FocusY + oy, z);
        }
    }
}
