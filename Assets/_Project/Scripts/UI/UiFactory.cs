using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using SlimeHub.Core;

namespace SlimeHub.UI
{
    public static class UiFactory
    {
        static Sprite rounded;
        static Font font;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Font Font
        {
            get
            {
                if (font != null)
                    return font;
                font = Font.CreateDynamicFontFromOSFont(new[] { "Arial Black", "Segoe UI Black", "Arial Rounded MT Bold", "Trebuchet MS", "Nunito", "Segoe UI", "Roboto", "Arial" }, 128);
                if (font == null)
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Rounded()
        {
            if (rounded != null)
                return rounded;
            const int size = 64;
            const int radius = 22;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x > size - radius - 1) dx = x - (size - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y > size - radius - 1) dy = y - (size - radius - 1);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = 255;
                    if (dist > radius)
                        alpha = dist > radius + 1.25f ? (byte)0 : (byte)(255f * (1f - (dist - radius) / 1.25f));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return rounded;
        }

        public static Sprite Load(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Rounded();
            if (sprites.TryGetValue(name, out Sprite cached) && cached != null)
                return cached;
            Sprite loaded = Resources.Load<Sprite>("SlimeHub/UI/" + name);
            if (loaded == null)
                return Rounded();
            sprites[name] = loaded;
            return loaded;
        }

        public static Image Picture(Transform parent, string spriteName, bool sliced)
        {
            RectTransform rect = Rect(spriteName, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Load(spriteName);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public static Canvas CreateCanvas(string name, int sort)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sort;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform SafeArea(Transform parent)
        {
            RectTransform rect = Rect("Safe", parent);
            ApplySafe(rect);
            return rect;
        }

        public static void ApplySafe(RectTransform rect)
        {
            Rect area = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            rect.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            rect.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Image Block(Transform parent, Color color)
        {
            RectTransform rect = Rect("Block", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Image Panel(Transform parent, Color color)
        {
            RectTransform rect = Rect("Panel", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        public static Text Label(Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = Rect("Label", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;
            return text;
        }

        public enum ButtonTone
        {
            Soft,
            Solid,
            Alert
        }

        public static Button Button(Transform parent, string label, Action onClick, float height = 108f, ButtonTone tone = ButtonTone.Soft)
        {
            Image image = Picture(parent, SpriteName(tone), true);
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Paint(button);

            Text text = Label(rect, label, 32, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(28f, 10f);
            text.rectTransform.offsetMax = new Vector2(-28f, -10f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 34;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            Outline(text);
            Bind(button, rect, onClick);
            return button;
        }

        public static Button Icon(Transform parent, string spriteName, Action onClick)
        {
            Image image = Picture(parent, spriteName, false);
            image.raycastTarget = true;
            image.preserveAspect = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Paint(button);
            Bind(button, image.rectTransform, onClick);
            return button;
        }

        public static Button ArtButton(Transform parent, string spriteName, string caption, Action onClick)
        {
            Button button = Icon(parent, spriteName, onClick);
            Text text = Label(button.transform, caption, 26, TextAnchor.MiddleCenter, Color.white);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.06f, 0.02f);
            rect.anchorMax = new Vector2(0.94f, 0.32f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 28;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            Outline(text);
            return button;
        }

        public static void Meter(Transform parent, float amount)
        {
            amount = Mathf.Clamp01(amount);
            Image track = Picture(parent, "bar_track", true);
            var layout = track.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 58f;
            layout.preferredHeight = 58f;
            RectTransform fillRect = Rect("Fill", track.transform);
            Stretch(fillRect);
            fillRect.offsetMin = new Vector2(16f, 11f);
            fillRect.offsetMax = new Vector2(-16f, -11f);
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = Load("bar_fill");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = amount;
            fill.color = Color.white;
            fill.raycastTarget = false;
        }

        static string SpriteName(ButtonTone tone)
        {
            if (tone == ButtonTone.Solid)
                return "btn_green";
            if (tone == ButtonTone.Alert)
                return "btn_red";
            return "plate";
        }

        static void Paint(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.62f, 0.64f, 0.66f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        static void Bind(Button button, RectTransform rect, Action onClick)
        {
            button.onClick.AddListener(() =>
            {
                JuiceTween.PunchScale(rect, 0.08f, 0.22f);
                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                    audio.Play(Sfx.Ui, 1f, 0.8f);
                onClick?.Invoke();
            });
        }

        public static void Tint(Button button, Color color)
        {
            if (button == null)
                return;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(color, Color.white, 0.22f);
            colors.selectedColor = color;
            button.colors = colors;
        }

        public static void Outline(Graphic graphic)
        {
            if (graphic == null)
                return;
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.1f, 0.18f, 0.95f);
            outline.effectDistance = new Vector2(3.5f, -3.5f);
        }

        public static (Slider slider, Text caption) Slider(Transform parent, string caption, float value, Action<float> changed)
        {
            RectTransform row = Rect("SliderRow", parent);
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 148f;
            rowLayout.preferredHeight = 148f;
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;

            Text label = Label(row, caption, 30, TextAnchor.MiddleLeft, Color.white);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minHeight = 36f;
            labelLayout.preferredHeight = 36f;

            RectTransform sliderRect = Rect("Slider", row);
            var sliderLayout = sliderRect.gameObject.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 72f;
            sliderLayout.preferredHeight = 72f;
            var slider = sliderRect.gameObject.AddComponent<Slider>();

            Image background = Picture(sliderRect, "bar_track", true);
            Stretch(background.rectTransform);
            background.raycastTarget = true;

            RectTransform fillArea = Rect("Fill", sliderRect);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(14f, 14f);
            fillArea.offsetMax = new Vector2(-14f, -14f);
            Image fill = Picture(fillArea, "bar_fill", true);
            Stretch(fill.rectTransform);
            fill.raycastTarget = false;

            RectTransform handle = Rect("Handle", sliderRect);
            handle.sizeDelta = new Vector2(72f, 72f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = Load("bubble");
            handleImage.type = Image.Type.Simple;
            handleImage.preserveAspect = true;
            handleImage.color = Color.white;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(next =>
            {
                label.text = caption + "  " + Mathf.RoundToInt(next * 100f) + "%";
                changed?.Invoke(next);
            });
            label.text = caption + "  " + Mathf.RoundToInt(value * 100f) + "%";
            return (slider, label);
        }

        public static RectTransform Scroll(Transform parent, out RectTransform content)
        {
            RectTransform root = Rect("Scroll", parent);
            var rootImage = root.gameObject.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.02f);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.flexibleHeight = 1f;
            rootLayout.minHeight = 200f;

            RectTransform viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 28);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48f;
            return root;
        }

        public static InputField Field(Transform parent, string placeholder)
        {
            Image image = Picture(parent, "field", true);
            image.raycastTarget = true;
            var layout = image.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 104f;
            layout.preferredHeight = 104f;
            Text text = Label(image.transform, "", 32, TextAnchor.MiddleLeft, new Color(0.08f, 0.16f, 0.28f, 1f));
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(28f, 8f);
            text.rectTransform.offsetMax = new Vector2(-28f, -8f);
            text.supportRichText = false;
            text.raycastTarget = true;

            Text hint = Label(image.transform, placeholder, 32, TextAnchor.MiddleLeft, new Color(0.35f, 0.48f, 0.58f, 1f));
            Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(28f, 8f);
            hint.rectTransform.offsetMax = new Vector2(-28f, -8f);

            var field = image.gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }
    }
}
