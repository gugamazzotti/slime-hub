using System.Collections.Generic;
using UnityEngine;

namespace SlimeHub.UI
{
    public enum Ease
    {
        Linear,
        OutQuad,
        OutBack,
        OutElastic
    }

    /// <summary>
    /// Tween leve no lugar do DOTween: PunchScale, EaseOutBack e OutElastic
    /// sem pacote externo nem alocação por quadro.
    /// </summary>
    public static class JuiceTween
    {
        struct Tween
        {
            public Transform Target;
            public CanvasGroup Group;
            public Vector3 FromScale;
            public Vector3 ToScale;
            public float FromAlpha;
            public float ToAlpha;
            public float Duration;
            public float Time;
            public Ease Ease;
            public bool Punch;
            public float PunchAmount;
            public bool Active;
        }

        static readonly List<Tween> Items = new List<Tween>(32);

        public static void PunchScale(Transform target, float amount, float duration)
        {
            if (target == null)
                return;
            Items.Add(new Tween
            {
                Target = target,
                Duration = Mathf.Max(0.05f, duration),
                Punch = true,
                PunchAmount = amount,
                Active = true
            });
        }

        public static void Scale(Transform target, Vector3 to, float duration, Ease ease)
        {
            if (target == null)
                return;
            Items.Add(new Tween
            {
                Target = target,
                FromScale = target.localScale,
                ToScale = to,
                Duration = Mathf.Max(0.05f, duration),
                Ease = ease,
                Active = true
            });
        }

        public static void Fade(CanvasGroup group, float alpha, float duration)
        {
            if (group == null)
                return;
            Items.Add(new Tween
            {
                Group = group,
                FromAlpha = group.alpha,
                ToAlpha = alpha,
                Duration = Mathf.Max(0.05f, duration),
                Ease = Ease.OutQuad,
                Active = true
            });
        }

        public static void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                Tween tween = Items[i];
                if (!tween.Active || (tween.Target == null && tween.Group == null))
                {
                    Items.RemoveAt(i);
                    continue;
                }

                tween.Time += dt;
                float u = Mathf.Clamp01(tween.Time / tween.Duration);
                if (tween.Punch && tween.Target != null)
                {
                    float shaped = Mathf.Sin(u * Mathf.PI * 3f) * tween.PunchAmount * (1f - u);
                    tween.Target.localScale = Vector3.one * (1f + shaped);
                }
                else
                {
                    float eased = Evaluate(tween.Ease, u);
                    if (tween.Target != null)
                        tween.Target.localScale = Vector3.LerpUnclamped(tween.FromScale, tween.ToScale, eased);
                    if (tween.Group != null)
                        tween.Group.alpha = Mathf.LerpUnclamped(tween.FromAlpha, tween.ToAlpha, eased);
                }

                if (u >= 1f)
                {
                    if (tween.Punch && tween.Target != null)
                        tween.Target.localScale = Vector3.one;
                    Items.RemoveAt(i);
                }
                else
                {
                    Items[i] = tween;
                }
            }
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.OutBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case Ease.OutElastic:
                    if (t <= 0f || t >= 1f)
                        return t;
                    const float p = 0.35f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
                default:
                    return t;
            }
        }
    }
}
