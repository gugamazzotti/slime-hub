using UnityEngine;
using UnityEngine.UI;
using SlimeHub.Core;
using SlimeHub.Data;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.UI;

namespace SlimeHub.Hub
{
    public sealed class TerrariumTilt : MonoBehaviour
    {
        public Transform Bowl;
        public SlimeActor Slime;
        public SlimeInteractable Touch;
        public float LimitX = 0.45f;
        Vector2 current;
        Vector2 velocity;

        void Update()
        {
            if (Bowl == null || !ServiceLocator.TryGet<TouchInputService>(out var input))
                return;
            Vector2 target = input.Tilt();
            current = Vector2.SmoothDamp(current, target, ref velocity, 0.16f, 8f, Time.deltaTime);
            Bowl.localRotation = Quaternion.Euler(current.y * 13f, 0f, -current.x * 13f);
        }

        void FixedUpdate()
        {
            if (Slime == null || Slime.Body == null)
                return;
            if (Touch == null || !Touch.Dragging)
            {
                Vector3 downhill = new Vector3(current.x, 0f, current.y);
                if (downhill.sqrMagnitude >= 0.004f)
                {
                    float visc = Slime.Look.Viscosity;
                    float accel = Mathf.Lerp(1.6f, 0.55f, visc) * Mathf.Clamp01(downhill.magnitude);
                    Slime.Body.AddForce(downhill.normalized * accel, ForceMode.Acceleration);
                    Slime.Body.WakeUp();
                }
            }

            Vector3 position = Slime.Body.position;
            float clamped = Mathf.Clamp(position.x, -LimitX, LimitX);
            if (Mathf.Abs(clamped - position.x) <= 0.0001f)
                return;
            position.x = clamped;
            Slime.Body.position = position;
            Vector3 velocity = Slime.Body.linearVelocity;
            velocity.x = 0f;
            Slime.Body.linearVelocity = velocity;
        }
    }

    public sealed class HubComposer : MonoBehaviour
    {
        const float HalfX = 1.22f;
        const float HalfZ = 0.82f;
        const float LidY = 3.05f;

        Transform bowl;
        Transform wallL;
        Transform wallR;
        Transform wallBack;
        Transform glassFront;
        Transform floor;
        Transform lid;
        Transform backdrop;
        Material labMaterial;
        Texture labPhoto;
        TerrariumTilt tilt;
        SlimeInteractable touch;
        float centerLimitX = 0.45f;
        float aspect;
        float idle;
        int width;
        int height;
        UIManager ui;

        void Awake()
        {
            GameServices.Ensure();
            Build();
        }

        void Build()
        {
            Camera cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.32f, 0.55f);
            cam.orthographic = false;
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            cam.transform.position = new Vector3(0f, 1.22f, -6.15f);
            cam.transform.LookAt(new Vector3(0f, 0.9f, 0f));
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.09f, 0.1f);

            var backdropObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdropObject.name = "Backdrop";
            Destroy(backdropObject.GetComponent<Collider>());
            backdrop = backdropObject.transform;
            labPhoto = Resources.Load<Texture2D>("SlimeHub/UI/bg_lab");
            labMaterial = SlimeMaterials.CreatePhoto(labPhoto);
            var backRenderer = backdropObject.GetComponent<MeshRenderer>();
            backRenderer.sharedMaterial = labMaterial;
            backRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            backRenderer.receiveShadows = false;

            bowl = new GameObject("Bowl").transform;
            var bowlBody = bowl.gameObject.AddComponent<Rigidbody>();
            bowlBody.isKinematic = true;
            bowlBody.useGravity = false;
            tilt = gameObject.AddComponent<TerrariumTilt>();
            tilt.Bowl = bowl;

            floor = Box(bowl, "Floor", new Vector3(0f, 0f, 0f), new Vector3(HalfX * 2f + 0.2f, 0.16f, HalfZ * 2f + 0.2f), new Color(0.08f, 0.1f, 0.11f, 1f), true, true);
            wallL = Box(bowl, "WallL", new Vector3(-HalfX - 0.06f, 1.45f, 0f), new Vector3(0.1f, 2.9f, HalfZ * 2f), new Color(0.45f, 0.86f, 0.8f, 0.42f), false, true);
            wallR = Box(bowl, "WallR", new Vector3(HalfX + 0.06f, 1.45f, 0f), new Vector3(0.1f, 2.9f, HalfZ * 2f), new Color(0.45f, 0.86f, 0.8f, 0.42f), false, true);
            wallBack = Box(bowl, "WallBack", new Vector3(0f, 1.45f, HalfZ + 0.06f), new Vector3(HalfX * 2f, 2.9f, 0.1f), new Color(0.5f, 0.84f, 0.8f, 0.22f), false, true);
            lid = Box(bowl, "Lid", new Vector3(0f, LidY, 0f), new Vector3(HalfX * 2f, 0.08f, HalfZ * 2f), new Color(0.5f, 0.8f, 0.8f, 0.05f), false, true);
            glassFront = Box(bowl, "GlassFront", new Vector3(0f, 1.45f, -HalfZ - 0.08f), new Vector3(HalfX * 2f + 0.2f, 3.1f, 0.04f), new Color(0.7f, 0.95f, 0.9f, 0.08f), false, false);
            FitJar();
            HidePiece(floor);
            HidePiece(wallL);
            HidePiece(wallR);
            HidePiece(wallBack);
            HidePiece(lid);
            HidePiece(glassFront);

            int subdiv = AppQuality.LowMemory ? 2 : 3;
            SlimeActor slime = SlimeActor.Create(new Vector3(0f, 0.85f, 0f), 0.48f, subdiv, true, true);
            touch = slime.gameObject.AddComponent<SlimeInteractable>();
            touch.Bind(slime, cam);
            touch.SetPlayBounds(centerLimitX, 0.32f);
            touch.SetHeightRange(0.55f, 1.48f);
            ServiceLocator.Get<SlimeCustomizer>().Bind(slime);
            slime.Body.useGravity = false;
            slime.Body.linearDamping = Mathf.Lerp(0.45f, 1.35f, slime.Look.Viscosity);
            slime.Sphere.material.dynamicFriction = 0.18f;
            slime.Sphere.material.staticFriction = 0.24f;
            tilt.Slime = slime;
            tilt.Touch = touch;
            tilt.LimitX = centerLimitX;

            for (int i = 0; i < 8; i++)
            {
                var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bubble.name = "Bubble";
                Destroy(bubble.GetComponent<Collider>());
                bubble.transform.SetParent(bowl, false);
                bubble.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.04f, 0.1f);
                bubble.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.34f, 0.34f), UnityEngine.Random.Range(0.4f, 1.5f), UnityEngine.Random.Range(-0.2f, 0.2f));
                var renderer = bubble.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = SlimeMaterials.CreateUnlit(new Color(0.8f, 1f, 0.95f, 0.28f), false);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bubble.AddComponent<RisingBubble>();
            }

            BuildTankFrame();
            ui = gameObject.AddComponent<UIManager>();
            ui.Build();
        }

        void BuildTankFrame()
        {
            Canvas canvas = UiFactory.CreateCanvas("TankFrame", 8);
            Image tank = UiFactory.Picture(canvas.transform, "tank", false);
            tank.preserveAspect = true;
            RectTransform rect = tank.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float frameWidth = 1000f;
            float aspectRatio = tank.sprite != null ? tank.sprite.rect.height / Mathf.Max(1f, tank.sprite.rect.width) : 1.5f;
            rect.sizeDelta = new Vector2(frameWidth, frameWidth * aspectRatio);
            rect.anchoredPosition = new Vector2(0f, 64f);
        }

        void FitJar()
        {
            width = Screen.width;
            height = Screen.height;
            Camera cam = Camera.main;
            if (cam != null)
                aspect = cam.aspect;
            const float radius = 0.48f;
            const float thick = 0.09f;
            float play = 1.05f;
            if (cam != null)
                play = Mathf.Max(0.5f, VisibleHalf(cam, 0f) - 0.02f);
            centerLimitX = Mathf.Max(0.16f, play - radius * 1.05f);
            float inner = play;
            Place(wallL, new Vector3(-inner - thick * 0.5f, 1.45f, 0f), new Vector3(thick, 2.95f, HalfZ * 2f));
            Place(wallR, new Vector3(inner + thick * 0.5f, 1.45f, 0f), new Vector3(thick, 2.95f, HalfZ * 2f));
            Place(wallBack, new Vector3(0f, 1.45f, HalfZ + 0.06f), new Vector3(inner * 2f, 2.95f, 0.1f));
            Place(floor, new Vector3(0f, 0f, 0f), new Vector3(inner * 2f + thick, 0.16f, HalfZ * 2f + 0.2f));
            Place(lid, new Vector3(0f, LidY, 0f), new Vector3(inner * 2f, 0.08f, HalfZ * 2f));
            Place(glassFront, new Vector3(0f, 1.45f, -HalfZ - 0.08f), new Vector3(inner * 2f + thick, 3.1f, 0.04f));
            if (touch != null)
            {
                touch.SetPlayBounds(centerLimitX, 0.32f);
                touch.SetHeightRange(0.55f, 1.48f);
            }
            FitBackdrop();
            if (tilt != null)
                tilt.LimitX = centerLimitX;
        }

        static float VisibleHalf(Camera cam, float planeZ)
        {
            float left = EdgeX(cam, 0.16f, planeZ);
            float right = EdgeX(cam, 0.84f, planeZ);
            return Mathf.Min(Mathf.Abs(left), Mathf.Abs(right));
        }

        static float EdgeX(Camera cam, float viewportX, float planeZ)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(viewportX, 0.5f, 0f));
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
                return 1f;
            float distance = (planeZ - ray.origin.z) / ray.direction.z;
            return ray.origin.x + ray.direction.x * distance;
        }

        void FitBackdrop()
        {
            Camera cam = Camera.main;
            if (cam == null || backdrop == null)
                return;
            const float distance = 12f;
            float halfHeight = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float halfWidth = halfHeight * cam.aspect;
            backdrop.SetParent(cam.transform, false);
            backdrop.localPosition = new Vector3(0f, 0f, distance);
            backdrop.localRotation = Quaternion.identity;
            backdrop.localScale = new Vector3(-halfWidth * 2f, halfHeight * 2f, 1f);
            if (labMaterial == null || labPhoto == null || labPhoto.height < 1)
                return;
            float imageAspect = labPhoto.width / (float)labPhoto.height;
            float viewAspect = halfWidth / Mathf.Max(0.01f, halfHeight);
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;
            if (imageAspect > viewAspect)
            {
                scale.x = viewAspect / imageAspect;
                offset.x = (1f - scale.x) * 0.5f;
            }
            else
            {
                scale.y = imageAspect / viewAspect;
                offset.y = (1f - scale.y) * 0.5f;
            }
            labMaterial.SetTextureScale("_BaseMap", scale);
            labMaterial.SetTextureOffset("_BaseMap", offset);
        }

        static void HidePiece(Transform piece)
        {
            if (piece == null)
                return;
            MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        static void Place(Transform piece, Vector3 localPos, Vector3 size)
        {
            if (piece == null)
                return;
            piece.localPosition = localPos;
            piece.localScale = size;
        }

        static Transform Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color, bool zWrite, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider)
                Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SlimeMaterials.CreateUnlit(color, zWrite, zWrite ? 0f : 1f, 0f, new Color(0.85f, 1f, 0.95f, 1f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        void Update()
        {
            Camera view = Camera.main;
            float viewAspect = view != null ? view.aspect : aspect;
            if (Screen.width != width || Screen.height != height || Mathf.Abs(viewAspect - aspect) > 0.01f)
                FitJar();
            bool touching = ServiceLocator.TryGet<TouchInputService>(out var input) && (input.State.Held || input.Tilt().sqrMagnitude > 0.04f);
            idle = touching ? 0f : idle + Time.unscaledDeltaTime;
            AppQuality.SetFrameRate(idle > 10f ? 30 : 60);
        }
    }

    public sealed class RisingBubble : MonoBehaviour
    {
        float speed;

        void Awake()
        {
            speed = UnityEngine.Random.Range(0.08f, 0.2f);
        }

        void Update()
        {
            Vector3 position = transform.localPosition;
            position.y += speed * Time.deltaTime;
            if (position.y > 1.72f)
                position.y = 0.32f;
            transform.localPosition = position;
        }
    }
}
