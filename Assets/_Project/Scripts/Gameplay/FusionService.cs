using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Data;

namespace SlimeHub.Gameplay
{
    public readonly struct FusionResult
    {
        public readonly string Id;
        public readonly string Label;
        public readonly bool Success;
        public readonly string Reason;

        public FusionResult(string id, string label, bool success, string reason)
        {
            Id = id;
            Label = label;
            Success = success;
            Reason = reason;
        }
    }

    public interface IFusionService
    {
        int Cost { get; }
        Awaitable<FusionResult> FuseAsync(string friendCode);
    }

    /// <summary>
    /// Mistura local. Quando a API existir, troque o registro no ServiceLocator
    /// por uma implementação que chame o servidor e devolva o mesmo FusionResult.
    /// </summary>
    public sealed class LocalFusionService : IFusionService
    {
        public const int FusionCost = 30;
        readonly IDataService save;
        readonly SlimeCustomizer customizer;

        public int Cost => FusionCost;

        public LocalFusionService(IDataService save, SlimeCustomizer customizer)
        {
            this.save = save;
            this.customizer = customizer;
        }

        public async Awaitable<FusionResult> FuseAsync(string friendCode)
        {
            await AsyncUtil.WaitUnscaled(0.45f);
            if (!save.TrySpend(FusionCost))
                return new FusionResult("", "", false, Loc.Get("fusion.broke"));

            customizer.RebuildLook();
            Color mine = customizer.Current.Base;
            Color friend = FriendColor(friendCode);
            Color mix = Color.Lerp(mine, friend, 0.55f);
            mix.a = 0.9f;

            int textures = Mathf.Max(1, customizer.TextureCount());
            int cores = 3;
            var random = new System.Random(StableHash(string.IsNullOrWhiteSpace(friendCode) ? System.Guid.NewGuid().ToString("N") : friendCode.Trim()));
            int texture = random.Next(0, textures);
            int core = random.Next(0, cores);

            var entry = new FusionSave
            {
                id = "fusion_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
                label = string.IsNullOrWhiteSpace(friendCode) ? "" : friendCode.Trim(),
                r = mix.r,
                g = mix.g,
                b = mix.b,
                texture = texture,
                core = core
            };

            save.Data.fusions.Add(entry);
            while (save.Data.fusions.Count > 12)
                save.Data.fusions.RemoveAt(0);
            save.Data.Unlock(entry.id);
            save.Data.activeFusionID = entry.id;
            save.SaveSoon();
            customizer.RebuildLook();
            customizer.Apply();
            return new FusionResult(entry.id, entry.label, true, "");
        }

        static Color FriendColor(string code)
        {
            int hash = StableHash(string.IsNullOrWhiteSpace(code) ? "anon" : code.Trim().ToLowerInvariant());
            var random = new System.Random(hash);
            return new Color(random.Next(25, 100) / 100f, random.Next(25, 100) / 100f, random.Next(25, 100) / 100f, 1f);
        }

        static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash == int.MinValue ? 1 : System.Math.Abs(hash);
            }
        }
    }
}
