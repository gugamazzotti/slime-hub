using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Input;

namespace SlimeHub.Gameplay
{
    public sealed class SlimeInteractable : MonoBehaviour, IInteractable
    {
        SlimeActor actor;
        Camera cam;
        int finger = int.MinValue;
        bool dragging;
        bool dragged;
        float nextSound;
        float nextGiggle;
        float pressAt;
        float lastTapAt = -1f;
        float limitX = 0.45f;
        float limitZ = 0.48f;
        float minY = 0.42f;
        float maxY = 2.5f;
        Vector2 pressScreen;
        Vector2 lastTapPos;

        public void SetPlayBounds(float x, float z)
        {
            limitX = Mathf.Max(0.15f, x);
            limitZ = Mathf.Max(0.15f, z);
        }

        public void SetHeightRange(float min, float max)
        {
            minY = min;
            maxY = Mathf.Max(min + 0.2f, max);
        }

        public bool Dragging => dragging;

        public void Bind(SlimeActor slime, Camera camera)
        {
            actor = slime;
            cam = camera;
        }

        void Update()
        {
            if (actor == null || cam == null)
                return;
            if (!ServiceLocator.TryGet<TouchInputService>(out var input))
                return;
            if (UI.UIManager.ModalOpen)
            {
                if (dragging)
                    OnRelease();
                return;
            }

            PointerState pointer = input.State;
            if (pointer.Began && !pointer.OverUi && HitSlime(pointer.Position, out _))
            {
                if (IsDoubleTap(pointer.Position))
                {
                    lastTapAt = -1f;
                    OnDoubleTap();
                }
                else
                {
                    finger = pointer.FingerId;
                    dragging = true;
                    dragged = false;
                    pressAt = Time.unscaledTime;
                    pressScreen = pointer.Position;
                    OnPress(HitPoint(pointer.Position), Vector3.zero);
                }
            }
            else if (dragging && pointer.Held && pointer.FingerId == finger)
            {
                float slop = TapSlop();
                if ((pointer.Position - pressScreen).sqrMagnitude > slop * slop)
                    dragged = true;
                Vector3 velocity = ScreenDeltaToWorld(pointer.Delta) / Mathf.Max(Time.deltaTime, 0.0008f);
                OnDrag(FingerPoint(pointer.Position), velocity);
            }
            else if (dragging && (pointer.Ended || !pointer.Has))
            {
                NoteTap(pointer.Position);
                OnRelease();
            }
        }

        public void OnPress(Vector3 worldPoint, Vector3 worldVelocity)
        {
            actor.Boop(worldPoint);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Pop, 1.05f, 0.8f);
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Light();
        }

        public void OnDrag(Vector3 worldPoint, Vector3 worldVelocity)
        {
            actor.Deformer.Poke(worldPoint, worldVelocity, actor.Deformer.Radius * 0.55f);
            actor.Pull(worldVelocity);
            if (actor.Body != null)
                SlideBody(worldPoint);

            float speed = worldVelocity.magnitude;
            if (speed < 0.4f || Time.unscaledTime < nextSound)
                return;
            nextSound = Time.unscaledTime + 0.07f;
            float sizePitch = Mathf.Lerp(1.2f, 0.75f, Mathf.Clamp01(transform.lossyScale.x));
            float speedPitch = Mathf.Lerp(0.9f, 1.25f, Mathf.Clamp01(speed / 8f));
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Bubble, sizePitch * speedPitch, Mathf.Lerp(0.35f, 0.9f, Mathf.Clamp01(speed / 6f)));
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Light();
            if (speed > 6f && Time.unscaledTime > nextGiggle)
            {
                nextGiggle = Time.unscaledTime + 1.4f;
                if (ServiceLocator.TryGet<IAudioService>(out var giggle))
                    giggle.PlayGiggle(sizePitch);
            }
        }

        public void OnRelease()
        {
            dragging = false;
            finger = int.MinValue;
            actor.ReleaseTouch();
        }

        void OnDoubleTap()
        {
            actor.Delight();
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.PlayGiggle(1.12f);
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Medium();
        }

        bool IsDoubleTap(Vector2 pos)
        {
            if (lastTapAt < 0f || Time.unscaledTime - lastTapAt > 0.36f)
                return false;
            float slop = TapSlop();
            return (pos - lastTapPos).sqrMagnitude <= slop * slop;
        }

        void NoteTap(Vector2 pos)
        {
            float slop = TapSlop();
            bool quick = !dragged
                && Time.unscaledTime - pressAt <= 0.24f
                && (pos - pressScreen).sqrMagnitude <= slop * slop;
            if (quick)
            {
                lastTapAt = Time.unscaledTime;
                lastTapPos = pos;
            }
            else
            {
                lastTapAt = -1f;
            }
        }

        static float TapSlop()
        {
            return Mathf.Max(56f, Screen.height * 0.04f);
        }

        void SlideBody(Vector3 worldPoint)
        {
            Rigidbody body = actor.Body;
            Vector3 target = worldPoint;
            target.x = Mathf.Clamp(target.x, -limitX, limitX);
            target.y = Mathf.Clamp(target.y, minY, maxY);
            target.z = Mathf.Clamp(target.z, -limitZ, limitZ);
            Vector3 lead = target - body.position;
            float visc = actor.Look.Viscosity;
            float follow = Mathf.Lerp(7.2f, 3.2f, visc);
            float maxSpeed = Mathf.Lerp(3.2f, 1.45f, visc);
            Vector3 desired = Vector3.ClampMagnitude(lead * follow, maxSpeed);
            float blend = 1f - Mathf.Exp(-Mathf.Lerp(8f, 3.6f, visc) * Time.deltaTime);
            body.linearVelocity = Vector3.Lerp(body.linearVelocity, desired, blend);
            body.WakeUp();
        }

        bool HitSlime(Vector2 screen, out RaycastHit hit)
        {
            Ray ray = cam.ScreenPointToRay(screen);
            if (UnityEngine.Physics.Raycast(ray, out hit, 40f) && hit.collider.GetComponentInParent<SlimeInteractable>() == this)
                return true;
            hit = default;
            return false;
        }

        Vector3 FingerPoint(Vector2 screen)
        {
            Plane plane = new Plane(cam.transform.forward, transform.position);
            Ray ray = cam.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float distance))
                return ray.GetPoint(Mathf.Min(distance, 40f));
            return transform.position;
        }

        Vector3 HitPoint(Vector2 screen)
        {
            if (HitSlime(screen, out RaycastHit hit))
                return hit.point;
            Plane plane = new Plane(cam.transform.forward, transform.position);
            Ray ray = cam.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);
            return transform.position;
        }

        Vector3 ScreenDeltaToWorld(Vector2 delta)
        {
            float depth = Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward);
            Vector3 a = cam.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 b = cam.ScreenToWorldPoint(new Vector3(delta.x, delta.y, depth));
            return b - a;
        }
    }
}
