using UnityEngine;

namespace SlimeHub.Core
{
    public static class AsyncUtil
    {
        public static async Awaitable WaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.unscaledDeltaTime;
            }
        }
    }
}
