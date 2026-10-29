using System;
using UnityEngine;
using UnityEngine.UI;
using SlimeHub.Core;

namespace SlimeHub.UI
{
    public sealed class MinigameShell : MonoBehaviour
    {
        public event Action Retry;
        public event Action Home;
        public event Action PausePressed;
        public event Action ResumePressed;

        Text scoreText;
        Text subText;
        Text countdown;
        Image flash;
        GameObject pauseRoot;
        GameObject overRoot;
        Text overTitle;
        Text overBody;
        Text popup;
        Text hintText;
        Image meterFill;
        float popupLeft;
        bool overShown;

        public void Build(string modeName)
        {
            Canvas canvas = UiFactory.CreateCanvas("MinigameUI", 30);
            canvas.transform.SetParent(transform, false);
            RectTransform safe = UiFactory.SafeArea(canvas.transform);

            var plate = UiFactory.Picture(safe, "plate", true);
            var plateRect = plate.rectTransform;
            plateRect.anchorMin = new Vector2(0f, 1f);
            plateRect.anchorMax = new Vector2(1f, 1f);
            plateRect.pivot = new Vector2(0.5f, 1f);
            plateRect.offsetMin = new Vector2(16f, -168f);
            plateRect.offsetMax = new Vector2(-16f, -12f);

            scoreText = UiFactory.Label(plate.transform, "0", 58, TextAnchor.MiddleLeft, new Color(0.08f, 0.16f, 0.28f, 1f));
            var scoreRect = scoreText.rectTransform;
            scoreRect.anchorMin = new Vector2(0f, 0.42f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.offsetMin = new Vector2(36f, 0f);
            scoreRect.offsetMax = new Vector2(-180f, -8f);

            subText = UiFactory.Label(plate.transform, modeName, 26, TextAnchor.MiddleLeft, new Color(0.16f, 0.32f, 0.42f, 1f));
            var subRect = subText.rectTransform;
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.46f);
            subRect.offsetMin = new Vector2(36f, 12f);
            subRect.offsetMax = new Vector2(-180f, 0f);

            var pause = UiFactory.Icon(safe, "btn_pause", () => PausePressed?.Invoke());
            var pauseRect = pause.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.sizeDelta = new Vector2(168f, 112f);
            pauseRect.anchoredPosition = new Vector2(-24f, -28f);

            Image meter = UiFactory.Picture(safe, "bar_track", true);
            var meterRect = meter.rectTransform;
            meterRect.anchorMin = new Vector2(0f, 1f);
            meterRect.anchorMax = new Vector2(1f, 1f);
            meterRect.pivot = new Vector2(0.5f, 1f);
            meterRect.offsetMin = new Vector2(28f, -236f);
            meterRect.offsetMax = new Vector2(-210f, -180f);
            var fillRect = UiFactory.Rect("Fill", meter.transform);
            UiFactory.Stretch(fillRect);
            fillRect.offsetMin = new Vector2(14f, 10f);
            fillRect.offsetMax = new Vector2(-14f, -10f);
            meterFill = fillRect.gameObject.AddComponent<Image>();
            meterFill.sprite = UiFactory.Load("bar_fill");
            meterFill.type = Image.Type.Filled;
            meterFill.fillMethod = Image.FillMethod.Horizontal;
            meterFill.fillAmount = 0f;
            meterFill.color = Color.white;
            meterFill.raycastTarget = false;

            Image hintPlate = UiFactory.Picture(safe, "plate", true);
            var hintRect = hintPlate.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(920f, 108f);
            hintRect.anchoredPosition = new Vector2(0f, 22f);
            hintText = UiFactory.Label(hintPlate.transform, "", 30, TextAnchor.MiddleCenter, new Color(0.08f, 0.16f, 0.28f, 1f));
            UiFactory.Stretch(hintText.rectTransform);
            hintText.rectTransform.offsetMin = new Vector2(36f, 12f);
            hintText.rectTransform.offsetMax = new Vector2(-36f, -12f);
            hintText.resizeTextForBestFit = true;
            hintText.resizeTextMinSize = 18;
            hintText.resizeTextMaxSize = 32;

            countdown = UiFactory.Label(safe, "", 140, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(countdown.rectTransform);
            UiFactory.Outline(countdown);

            var flashRect = UiFactory.Rect("Flash", canvas.transform);
            UiFactory.Stretch(flashRect);
            flash = flashRect.gameObject.AddComponent<Image>();
            flash.color = new Color(1f, 0.15f, 0.2f, 0f);
            flash.raycastTarget = false;

            popup = UiFactory.Label(safe, "", 42, TextAnchor.MiddleCenter, Color.white);
            var popupRect = popup.rectTransform;
            popupRect.anchorMin = new Vector2(0.2f, 0.55f);
            popupRect.anchorMax = new Vector2(0.8f, 0.7f);
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;
            popup.gameObject.SetActive(false);
            UiFactory.Outline(popup);

            pauseRoot = BuildModal(safe, Loc.Get("shell.pause"), out _, out var pauseBody);
            UiFactory.Button(pauseBody, Loc.Get("shell.resume"), () => ResumePressed?.Invoke(), 112f, UiFactory.ButtonTone.Solid);
            UiFactory.Button(pauseBody, Loc.Get("shell.home"), () => Home?.Invoke(), 112f, UiFactory.ButtonTone.Alert);
            pauseRoot.SetActive(false);

            overRoot = BuildModal(safe, "", out overTitle, out var overBodyRoot);
            overBody = UiFactory.Label(overBodyRoot, "", 32, TextAnchor.UpperCenter, new Color(0.9f, 0.96f, 1f));
            var bodyLayout = overBody.gameObject.AddComponent<LayoutElement>();
            bodyLayout.minHeight = 180f;
            bodyLayout.preferredHeight = 220f;
            UiFactory.Button(overBodyRoot, Loc.Get("shell.retry"), () => Retry?.Invoke(), 112f, UiFactory.ButtonTone.Solid);
            UiFactory.Button(overBodyRoot, Loc.Get("shell.home"), () => Home?.Invoke(), 112f, UiFactory.ButtonTone.Alert);
            overRoot.SetActive(false);
        }

        static GameObject BuildModal(Transform parent, string title, out Text titleText, out RectTransform body)
        {
            var rootImage = UiFactory.Block(parent, new Color(0.03f, 0.06f, 0.12f, 0.72f));
            UiFactory.Stretch(rootImage.rectTransform);
            var window = UiFactory.Picture(rootImage.transform, "panel_monitor", true);
            window.raycastTarget = true;
            var rect = window.rectTransform;
            rect.anchorMin = new Vector2(0.06f, 0.22f);
            rect.anchorMax = new Vector2(0.94f, 0.78f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(88, 88, 96, 72);
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            titleText = UiFactory.Label(rect, title, 52, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Outline(titleText);
            var titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = 80f;
            titleLayout.preferredHeight = 80f;
            body = rect;
            return rootImage.gameObject;
        }

        public void SetHud(string main, string sub)
        {
            if (scoreText != null)
                scoreText.text = main;
            if (subText != null)
                subText.text = sub;
        }

        public void SetMeter(float amount)
        {
            if (meterFill != null)
                meterFill.fillAmount = Mathf.Clamp01(amount);
        }

        public void SetHint(string value)
        {
            if (hintText != null)
                hintText.text = value ?? "";
        }

        public void ShowCountdown(string text)
        {
            countdown.text = text ?? "";
            if (!string.IsNullOrEmpty(text))
                JuiceTween.PunchScale(countdown.rectTransform, 0.18f, 0.28f);
        }

        public void Flash(Color color)
        {
            color.a = 0.45f;
            flash.color = color;
        }

        public void Popup(string text, Color color)
        {
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.color = color;
            popupLeft = 0.7f;
            JuiceTween.PunchScale(popup.rectTransform, 0.16f, 0.25f);
        }

        public void ShowPause(bool show)
        {
            pauseRoot.SetActive(show);
        }

        public void ShowGameOver(string title, string body)
        {
            if (overShown)
                return;
            overShown = true;
            overTitle.text = title;
            overBody.text = body;
            overRoot.SetActive(true);
            overRoot.transform.localScale = Vector3.one * 0.86f;
            JuiceTween.Scale(overRoot.transform, Vector3.one, 0.32f, Ease.OutBack);
        }

        void Update()
        {
            if (flash != null && flash.color.a > 0f)
            {
                Color color = flash.color;
                color.a = Mathf.MoveTowards(color.a, 0f, Time.unscaledDeltaTime * 1.6f);
                flash.color = color;
            }

            if (popupLeft > 0f)
            {
                popupLeft -= Time.unscaledDeltaTime;
                Color color = popup.color;
                color.a = Mathf.Clamp01(popupLeft / 0.25f);
                popup.color = color;
                if (popupLeft <= 0f)
                    popup.gameObject.SetActive(false);
            }
        }
    }
}
