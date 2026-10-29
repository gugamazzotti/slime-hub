using UnityEngine;
using UnityEngine.Rendering;

namespace SlimeHub.Core
{
    /// <summary>
    /// Trava o jogo em 60 fps, desliga sombra em tempo real e reduz a simulação
    /// do slime se o quadro passar de ~20 ms por mais de um segundo.
    /// </summary>
    public static class AppQuality
    {
        public static int DeformStride { get; private set; } = 1;
        public static bool LowMemory { get; private set; }

        static float average = 0.016f;
        static float sampleTimer;

        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.pixelLightCount = 1;
            SetFrameRate(60);
            UnityEngine.Physics.defaultSolverIterations = 12;
            UnityEngine.Physics.defaultSolverVelocityIterations = 2;
            Time.fixedDeltaTime = 1f / 60f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            LowMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 2800;

#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
#endif
            if (GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 0f;
                urp.supportsCameraDepthTexture = false;
                urp.supportsCameraOpaqueTexture = false;
            }
        }

        public static void SetFrameRate(int fps)
        {
            Application.targetFrameRate = fps;
        }

        public static void Tick()
        {
            average = Mathf.Lerp(average, Time.unscaledDeltaTime, 0.04f);
            sampleTimer += Time.unscaledDeltaTime;
            if (sampleTimer < 1.2f)
                return;
            if (average > 0.0205f)
                DeformStride = 2;
            else if (average < 0.0155f)
                DeformStride = 1;
        }
    }
}
