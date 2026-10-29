using UnityEngine;
using UnityEngine.UI;
using SlimeHub.UI;

namespace SlimeHub.Core
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        async void Start()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = false;
            }

            GameServices.Ensure();

            Canvas canvas = UiFactory.CreateCanvas("BootUI", 40);
            Text slime = UiFactory.Label(canvas.transform, "SLIME", 96, TextAnchor.MiddleCenter, new Color(0.42f, 0.9f, 0.32f));
            var slimeRect = slime.rectTransform;
            slimeRect.anchorMin = new Vector2(0f, 0.5f);
            slimeRect.anchorMax = new Vector2(1f, 0.5f);
            slimeRect.sizeDelta = new Vector2(0f, 120f);
            slimeRect.anchoredPosition = new Vector2(0f, 70f);
            slimeRect.localScale = Vector3.one * 0.82f;
            UiFactory.Outline(slime);
            JuiceTween.Scale(slimeRect, Vector3.one, 0.48f, Ease.OutBack);

            Text hub = UiFactory.Label(canvas.transform, "HUB", 96, TextAnchor.MiddleCenter, new Color(1f, 0.58f, 0.16f));
            var hubRect = hub.rectTransform;
            hubRect.anchorMin = new Vector2(0f, 0.5f);
            hubRect.anchorMax = new Vector2(1f, 0.5f);
            hubRect.sizeDelta = new Vector2(0f, 120f);
            hubRect.anchoredPosition = new Vector2(0f, -40f);
            hubRect.localScale = Vector3.one * 0.82f;
            UiFactory.Outline(hub);
            JuiceTween.Scale(hubRect, Vector3.one, 0.48f, Ease.OutBack);

            Text subtitle = UiFactory.Label(canvas.transform, Loc.Get("boot.subtitle"), 32, TextAnchor.MiddleCenter, Color.white);
            var subRect = subtitle.rectTransform;
            subRect.anchorMin = new Vector2(0f, 0.5f);
            subRect.anchorMax = new Vector2(1f, 0.5f);
            subRect.sizeDelta = new Vector2(0f, 60f);
            subRect.anchoredPosition = new Vector2(0f, -140f);
            UiFactory.Outline(subtitle);

            await AsyncUtil.WaitUnscaled(0.85f);
            SceneFlow.Load(SceneNames.Hub);
        }
    }
}
