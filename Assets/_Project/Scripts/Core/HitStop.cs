using UnityEngine;

namespace SlimeHub.Core
{
    public static class HitStop
    {
        static float restore = 1f;
        static float until;

        public static void Play(float scale, float seconds)
        {
            if (until <= 0f)
                restore = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = Mathf.Clamp(scale, 0.01f, 1f);
            until = Time.unscaledTime + seconds;
        }

        public static void Cancel()
        {
            until = 0f;
            Time.timeScale = 1f;
            restore = 1f;
        }

        public static void Tick()
        {
            if (until <= 0f)
                return;
            if (Time.unscaledTime < until)
                return;
            Time.timeScale = restore <= 0f ? 1f : restore;
            until = 0f;
        }
    }
}
