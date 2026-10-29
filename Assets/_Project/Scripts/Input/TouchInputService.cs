using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SlimeHub.Input
{
    public struct PointerState
    {
        public bool Has;
        public bool Began;
        public bool Held;
        public bool Ended;
        public int FingerId;
        public Vector2 Position;
        public Vector2 Delta;
        public bool OverUi;
        public bool Pinch;
        public float Pinch01;
        public Vector2 PinchCenter;
    }

    public sealed class TouchInputService : MonoBehaviour
    {
        public PointerState State { get; private set; }

        readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
        PointerEventData uiData;
        bool tracking;
        int trackedId = -1;
        Vector2 last;
        bool pinching;
        float pinchStart;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (Accelerometer.current != null)
                InputSystem.EnableDevice(Accelerometer.current);
        }

        void OnDisable()
        {
            if (EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            Poll();
        }

        void Poll()
        {
            var touches = Touch.activeTouches;
            if (touches.Count > 0)
            {
                ReadTouch(touches);
                return;
            }

            ReadMouse();
        }

        void ReadTouch(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches)
        {
            var primary = touches[0];
            bool pinch = touches.Count >= 2;
            Vector2 position = primary.screenPosition;
            Vector2 center = position;
            float pinch01 = 0f;
            if (pinch)
            {
                Vector2 a = touches[0].screenPosition;
                Vector2 b = touches[1].screenPosition;
                center = (a + b) * 0.5f;
                position = center;
                float distance = Vector2.Distance(a, b);
                if (!pinching)
                {
                    pinching = true;
                    pinchStart = distance;
                }
                pinch01 = Mathf.Clamp01((distance - pinchStart) / 150f);
            }
            else
            {
                pinching = false;
            }

            bool began = primary.phase == UnityEngine.InputSystem.TouchPhase.Began && !tracking;
            bool ended = primary.phase == UnityEngine.InputSystem.TouchPhase.Ended
                || primary.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
            if (pinch && tracking)
                began = false;

            Vector2 delta = tracking ? position - last : Vector2.zero;
            bool over = OverUi(position);
            ApplyKeyboardSplit(ref pinch, ref pinch01);

            State = new PointerState
            {
                Has = !ended,
                Began = began && !ended,
                Held = !ended && (tracking || began),
                Ended = ended,
                FingerId = primary.touchId,
                Position = position,
                Delta = delta,
                OverUi = over,
                Pinch = pinch && pinch01 > 0.08f,
                Pinch01 = pinch01,
                PinchCenter = center
            };

            if (ended)
            {
                tracking = false;
                trackedId = -1;
            }
            else
            {
                tracking = true;
                trackedId = primary.touchId;
                last = position;
            }
        }

        void ReadMouse()
        {
            pinching = false;
            Mouse mouse = Mouse.current;
            bool keySplit = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            if (mouse == null)
            {
                State = new PointerState { Pinch = keySplit, Pinch01 = keySplit ? 0.85f : 0f };
                tracking = false;
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            bool began = mouse.leftButton.wasPressedThisFrame;
            bool held = mouse.leftButton.isPressed;
            bool ended = mouse.leftButton.wasReleasedThisFrame;
            Vector2 delta = tracking ? position - last : Vector2.zero;
            bool over = OverUi(position);
            float pinch01 = keySplit ? 0.85f : 0f;

            State = new PointerState
            {
                Has = held,
                Began = began,
                Held = held,
                Ended = ended,
                FingerId = -1,
                Position = position,
                Delta = held ? delta : Vector2.zero,
                OverUi = over,
                Pinch = keySplit,
                Pinch01 = pinch01,
                PinchCenter = position
            };

            tracking = held;
            last = position;
            trackedId = -1;
        }

        static void ApplyKeyboardSplit(ref bool pinch, ref float pinch01)
        {
            if (Keyboard.current == null || !Keyboard.current.spaceKey.isPressed)
                return;
            pinch = true;
            pinch01 = Mathf.Max(pinch01, 0.85f);
        }

        bool OverUi(Vector2 screen)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return false;
            if (uiData == null)
                uiData = new PointerEventData(events);
            uiData.position = screen;
            uiHits.Clear();
            events.RaycastAll(uiData, uiHits);
            return uiHits.Count > 0;
        }

        public Vector2 Tilt()
        {
            Accelerometer accelerometer = Accelerometer.current;
            if (accelerometer != null && accelerometer.enabled)
            {
                Vector3 gravity = accelerometer.acceleration.ReadValue();
                return new Vector2(
                    Mathf.Clamp(gravity.x * 1.35f, -1f, 1f),
                    Mathf.Clamp(-gravity.z * 1.35f, -1f, 1f));
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
    }
}
