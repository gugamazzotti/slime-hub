using UnityEngine;

namespace SlimeHub
{
    public static class SlimeTiers
    {
        public const int Count = 7;

        // Raio no mundo. A largura interna do pote cabe dois do maior.
        private static readonly float[] Radii = { 0.36f, 0.48f, 0.62f, 0.80f, 1.02f, 1.26f, 1.52f };

        private static readonly Color[] Colors =
        {
            new Color(0.78f, 0.95f, 0.36f),
            new Color(0.42f, 0.88f, 0.46f),
            new Color(0.18f, 0.78f, 0.62f),
            new Color(0.28f, 0.74f, 0.90f),
            new Color(0.98f, 0.74f, 0.28f),
            new Color(0.96f, 0.46f, 0.64f),
            new Color(0.74f, 0.50f, 0.98f)
        };

        public static float Radius(int tier)
        {
            return Radii[Mathf.Clamp(tier, 0, Count - 1)];
        }

        public static Color ColorOf(int tier)
        {
            return Colors[Mathf.Clamp(tier, 0, Count - 1)];
        }

        public static int RollDrop()
        {
            float r = Random.value;
            if (r < 0.55f) return 0;
            if (r < 0.88f) return 1;
            return 2;
        }
    }
}
