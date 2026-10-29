using UnityEngine;

namespace SlimeHub
{
    public sealed class SlimeBlob : MonoBehaviour
    {
        public int Tier;
        public bool Held;
        public bool Dead;
        public float Age;
        public float AboveTime;
        public float Radius;
        public Rigidbody2D Body;

        private Transform visual;
        private float squash;
        private float pop = 1f;

        public void Init(int tier, bool held, Material shared, Mesh mesh, PhysicsMaterial2D physics)
        {
            Tier = tier;
            Held = held;
            Radius = SlimeTiers.Radius(tier);
            float diameter = Radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);

            var visualGo = new GameObject("visual");
            visualGo.transform.SetParent(transform, false);
            visual = visualGo.transform;
            var filter = visualGo.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = visualGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = shared;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            var block = new MaterialPropertyBlock();
            var color = SlimeTiers.ColorOf(tier);
            color *= Random.Range(0.94f, 1.04f);
            color.a = 1f;
            block.SetColor("_Color", color);
            block.SetColor("_Highlight", new Color(1f, 1f, 1f, 0.72f));
            block.SetFloat("_Round", 1f);
            renderer.SetPropertyBlock(block);

            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f;
            circle.sharedMaterial = physics;

            if (held) Hold();
            else
            {
                Body.bodyType = RigidbodyType2D.Dynamic;
                Body.gravityScale = 1f;
                pop = 0.35f;
            }
        }

        public void Hold()
        {
            Held = true;
            Age = 0f;
            AboveTime = 0f;
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.gravityScale = 0f;
            Body.velocity = Vector2.zero;
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null) circle.enabled = false;
        }

        public void Release()
        {
            Held = false;
            Age = 0f;
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null) circle.enabled = true;
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 1f;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.velocity = new Vector2(0f, -1.2f);
        }

        public void MarkDead()
        {
            if (Dead) return;
            Dead = true;
            Held = false;
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null) circle.enabled = false;
            if (Body != null) Body.simulated = false;
        }

        public void Place(Vector2 position)
        {
            Body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        public void Tick(float dt)
        {
            if (!Held && !Dead) Age += dt;
            squash = Mathf.Lerp(squash, 0f, 1f - Mathf.Exp(-9f * dt));
            pop = Mathf.MoveTowards(pop, 1f, dt * 4.5f);
            float wobble = 0f;
            if (Held || (Body != null && Body.velocity.magnitude < 0.4f))
                wobble = Mathf.Sin(Time.time * 3.2f + Tier) * 0.035f;
            float sx = (1f + squash * 0.85f + wobble) * pop;
            float sy = (1f - squash + wobble * 0.4f) * pop;
            if (Dead) sy = sx = Mathf.MoveTowards(visual.localScale.x, 0f, dt * 6f);
            visual.localScale = new Vector3(sx, sy, 1f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Dead || Held) return;
            float mag = collision.relativeVelocity.magnitude;
            squash = Mathf.Max(squash, Mathf.Clamp(mag / 8f, 0.04f, 0.48f));

            var other = collision.collider.GetComponent<SlimeBlob>();
            if (other == null || other.Dead || other.Held || other.Tier != Tier) return;
            if (GetInstanceID() > other.GetInstanceID()) return;
            SlimeGame.Instance.QueueMerge(this, other);
        }
    }
}
