using UnityEngine;

namespace SlimeHub
{
    public static class SlimeAudio
    {
        private static AudioClip drop;
        private static AudioClip over;
        private static AudioClip hub;
        private static readonly AudioClip[] merges = new AudioClip[SlimeTiers.Count];

        public static void Warmup()
        {
            if (drop != null) return;
            drop = Tone("drop", 180f, 0.07f, 0.22f, noise: 0.65f);
            over = Tone("over", 140f, 0.45f, 0.3f, slide: 0.45f);
            hub = Tone("hub", 520f, 0.28f, 0.34f);
            for (int i = 0; i < merges.Length; i++)
                merges[i] = Tone("merge" + i, 280f * Mathf.Pow(1.18f, i), 0.12f, 0.28f);
        }

        public static void PlayDrop(AudioSource source) => Play(source, drop, 0.9f);
        public static void PlayMerge(AudioSource source, int tier) => Play(source, merges[Mathf.Clamp(tier, 0, merges.Length - 1)], 1f);
        public static void PlayHub(AudioSource source) => Play(source, hub, 1f);
        public static void PlayOver(AudioSource source) => Play(source, over, 1f);

        private static void Play(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, volume);
        }

        private static AudioClip Tone(string name, float freq, float duration, float volume, float noise = 0f, float slide = 1f)
        {
            const int rate = 22050;
            int n = Mathf.Max(1, Mathf.CeilToInt(rate * duration));
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float k = t / duration;
                float attack = Mathf.Clamp01(t / 0.012f);
                float release = Mathf.Clamp01((duration - t) / (duration * 0.55f));
                float f = freq * Mathf.Lerp(1f, slide, k);
                float sine = Mathf.Sin(2f * Mathf.PI * f * t);
                float nse = ((float)rng.NextDouble() * 2f - 1f);
                data[i] = (sine * (1f - noise) + nse * noise) * attack * release * volume;
            }

            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
