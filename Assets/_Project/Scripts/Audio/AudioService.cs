using UnityEngine;
using SlimeHub.Core;

namespace SlimeHub.Audio
{
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        const int Voices = 8;
        const int Rate = 22050;

        AudioSource[] voices;
        AudioSource music;
        AudioSource stretch;
        AudioClip pop, bubble, bloop, slap, schlupp, merge, ui, squeezer, stretchClip, pad;
        int cursor;
        bool muted;

        public bool Muted => muted;

        void Awake()
        {
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
                voices[i] = CreateSource(false);

            music = CreateSource(true);
            stretch = CreateSource(true);
            BuildClips();
            music.clip = pad;
            music.volume = 0.045f;
            music.loop = true;
            music.Play();
            stretch.clip = stretchClip;
            stretch.loop = true;
            stretch.volume = 0f;
            stretch.Play();
        }

        AudioSource CreateSource(bool loopSource)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loopSource;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        public void Play(Sfx sfx, float pitch = 1f, float volume = 1f)
        {
            if (muted)
                return;
            AudioClip clip = Clip(sfx);
            if (clip == null)
                return;
            float varied = pitch;
            if (sfx == Sfx.Pop || sfx == Sfx.Bubble || sfx == Sfx.Bloop || sfx == Sfx.Slap)
                varied *= Random.Range(0.92f, 1.12f);
            AudioSource voice = voices[cursor];
            cursor = (cursor + 1) % voices.Length;
            voice.pitch = Mathf.Clamp(varied, 0.45f, 1.8f);
            voice.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void PlayGiggle(float pitch)
        {
            Play(Sfx.Bubble, pitch * 1.05f, 0.85f);
            StartCoroutine(Delayed(0.07f, Sfx.Bubble, pitch * 1.28f));
            StartCoroutine(Delayed(0.15f, Sfx.Pop, pitch * 1.45f));
        }

        System.Collections.IEnumerator Delayed(float delay, Sfx sfx, float pitch)
        {
            yield return new WaitForSeconds(delay);
            Play(sfx, pitch, 0.75f);
        }

        public void SetStretchLoop(bool active, float pitch)
        {
            if (stretch == null)
                return;
            stretch.pitch = Mathf.Clamp(pitch, 0.6f, 1.6f);
            stretch.volume = muted || !active ? 0f : 0.22f;
        }

        public void SetMuted(bool value)
        {
            muted = value;
            AudioListener.volume = value ? 0f : 1f;
            if (music != null)
                music.volume = value ? 0f : 0.045f;
            if (value)
                SetStretchLoop(false, 1f);
        }

        AudioClip Clip(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Pop: return pop;
                case Sfx.Bubble: return bubble;
                case Sfx.Bloop: return bloop;
                case Sfx.Slap: return slap;
                case Sfx.Schlupp: return schlupp;
                case Sfx.Merge: return merge;
                case Sfx.Ui: return ui;
                case Sfx.Squeezer: return squeezer;
                case Sfx.Stretch: return stretchClip;
                default: return pop;
            }
        }

        void BuildClips()
        {
            pop = Tone("pop", 520f, 0.09f, 18f, false, 0.35f);
            bubble = Sweep("bubble", 680f, 240f, 0.16f, 8f, false);
            bloop = Tone("bloop", 160f, 0.28f, 6f, false, 0.4f);
            slap = Sweep("slap", 180f, 70f, 0.12f, 10f, true);
            schlupp = Sweep("schlupp", 300f, 60f, 0.32f, 5f, true);
            merge = Tone("merge", 220f, 0.18f, 9f, false, 0.4f);
            ui = Tone("ui", 880f, 0.05f, 28f, false, 0.22f);
            squeezer = Sweep("squeeze", 140f, 40f, 0.45f, 4f, true);
            stretchClip = Tone("stretch", 90f, 0.4f, 1.2f, true, 0.18f);
            pad = Pad();
        }

        static AudioClip Tone(string name, float freq, float duration, float decay, bool noise, float amp)
        {
            int samples = Mathf.Max(1, (int)(Rate * duration));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-decay * t);
                float wave = noise ? Hash(i) * 2f - 1f : Mathf.Sin(t * freq * Mathf.PI * 2f);
                data[i] = wave * env * amp;
            }
            return ClipFrom(name, data);
        }

        static AudioClip Sweep(string name, float from, float to, float duration, float decay, bool noise)
        {
            int samples = Mathf.Max(1, (int)(Rate * duration));
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float u = i / (float)samples;
                float freq = Mathf.Lerp(from, to, u);
                phase += freq / Rate;
                float env = Mathf.Exp(-decay * u);
                float wave = noise ? Hash(i * 3 + 1) * 2f - 1f : Mathf.Sin(phase * Mathf.PI * 2f);
                float body = Mathf.Sin(phase * Mathf.PI * 2f) * (noise ? 0.65f : 1f);
                data[i] = (wave * 0.45f + body * 0.55f) * env * 0.38f;
            }
            return ClipFrom(name, data);
        }

        static AudioClip Pad()
        {
            int samples = Rate * 4;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)Rate;
                float trem = 0.8f + 0.2f * Mathf.Sin(t * 0.7f);
                float wave = Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.4f
                    + Mathf.Sin(t * 277.18f * Mathf.PI * 2f) * 0.25f
                    + Mathf.Sin(t * 329.63f * Mathf.PI * 2f) * 0.2f;
                data[i] = wave * trem * 0.18f;
            }
            return ClipFrom("pad", data);
        }

        static AudioClip ClipFrom(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Hash(int n)
        {
            n = (n << 13) ^ n;
            return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483648f;
        }
    }
}
