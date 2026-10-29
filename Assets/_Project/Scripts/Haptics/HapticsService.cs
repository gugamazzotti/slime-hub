using UnityEngine;
using SlimeHub.Core;

namespace SlimeHub.Haptics
{
    public sealed class HapticsService : IHapticsService
    {
        float nextLight;

        public bool Enabled { get; set; } = true;

        public void Light() => Pulse(18, 50, true);
        public void Medium() => Pulse(32, 140, false);
        public void Heavy() => Pulse(55, 230, false);

        void Pulse(long milliseconds, int amplitude, bool throttle)
        {
            if (!Enabled)
                return;
            if (throttle && Time.unscaledTime < nextLight)
                return;
            if (throttle)
                nextLight = Time.unscaledTime + 0.08f;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null)
                        return;
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        int sdk = version.GetStatic<int>("SDK_INT");
                        if (sdk >= 26)
                        {
                            using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                            using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                                vibrator.Call("vibrate", effect);
                        }
                        else
                        {
                            vibrator.Call("vibrate", milliseconds);
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Haptic indisponível: " + exception.Message);
                Enabled = false;
            }
#endif
        }
    }
}
