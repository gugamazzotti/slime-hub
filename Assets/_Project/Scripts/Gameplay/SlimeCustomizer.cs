using System.Collections.Generic;
using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Data;

namespace SlimeHub.Gameplay
{
    public sealed class SlimeCustomizer
    {
        readonly IDataService save;
        readonly List<SlimeCosmeticSO> catalog;
        SlimeActor actor;

        public SlimeLook Current { get; private set; }
        public IReadOnlyList<SlimeCosmeticSO> Catalog => catalog;

        public SlimeCustomizer(IDataService save)
        {
            this.save = save;
            catalog = SlimeCosmeticSO.RuntimeCatalog();
            RebuildLook();
        }

        public void Bind(SlimeActor slime)
        {
            actor = slime;
            Apply();
        }

        public SlimeCosmeticSO Find(string id)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].id == id)
                    return catalog[i];
            }
            return null;
        }

        public bool IsOwned(SlimeCosmeticSO item)
        {
            if (item == null)
                return false;
            if (item.unlockedByDefault || save.Data.IsUnlocked(item.id))
                return true;
            return false;
        }

        public bool IsSelected(SlimeCosmeticSO item)
        {
            if (item == null)
                return false;
            PlayerData data = save.Data;
            if (!string.IsNullOrEmpty(data.activeFusionID))
                return false;
            switch (item.slot)
            {
                case CosmeticSlot.Color: return data.activeColorID == item.id;
                case CosmeticSlot.Texture: return data.activeTextureID == item.id;
                case CosmeticSlot.Core: return data.activeCoreID == item.id;
                default: return false;
            }
        }

        public bool TrySelectOrBuy(SlimeCosmeticSO item)
        {
            if (item == null)
                return false;
            if (!IsOwned(item))
            {
                if (!save.TrySpend(item.cost))
                    return false;
                save.Data.Unlock(item.id);
            }

            save.Data.activeFusionID = "";
            switch (item.slot)
            {
                case CosmeticSlot.Color: save.Data.activeColorID = item.id; break;
                case CosmeticSlot.Texture: save.Data.activeTextureID = item.id; break;
                case CosmeticSlot.Core: save.Data.activeCoreID = item.id; break;
            }
            save.SaveSoon();
            RebuildLook();
            Apply();
            actor?.Deformer.Pulse(2.2f);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Pop, 1.1f);
            return true;
        }

        public void SetViscosity(float value)
        {
            save.Data.viscosity = Mathf.Clamp01(value);
            save.SaveSoon();
            RebuildLook();
            Apply();
        }

        public void SelectFusion(string id)
        {
            if (save.Data.FindFusion(id) == null)
                return;
            save.Data.activeFusionID = id;
            save.SaveSoon();
            RebuildLook();
            Apply();
            actor?.Deformer.Pulse(2.6f);
        }

        public void RebuildLook()
        {
            PlayerData data = save.Data;
            FusionSave fusion = data.FindFusion(data.activeFusionID);
            if (fusion != null)
            {
                SlimeCosmeticSO texture = TextureByIndex(fusion.texture);
                Current = new SlimeLook
                {
                    Base = new Color(fusion.r, fusion.g, fusion.b, 0.9f),
                    Rim = Color.Lerp(new Color(fusion.r, fusion.g, fusion.b, 1f), Color.white, 0.45f),
                    Glitter = texture != null ? texture.glitter : 0.3f,
                    Emission = texture != null ? texture.emission : 0.4f,
                    Metallic = texture != null ? texture.metallic : 0f,
                    Pulse = texture != null ? texture.pulse : 0.2f,
                    Viscosity = data.viscosity,
                    Core = (CoreKind)Mathf.Clamp(fusion.core, 0, 3)
                };
                return;
            }

            SlimeCosmeticSO color = Find(data.activeColorID) ?? Find("color_menta");
            SlimeCosmeticSO surface = Find(data.activeTextureID) ?? Find("tex_puro");
            SlimeCosmeticSO core = Find(data.activeCoreID) ?? Find("core_none");
            Current = new SlimeLook
            {
                Base = color.color,
                Rim = color.rim,
                Glitter = surface.glitter,
                Emission = surface.emission,
                Metallic = surface.metallic,
                Pulse = surface.pulse,
                Viscosity = data.viscosity,
                Core = core.core
            };
        }

        public void Apply()
        {
            actor?.ApplyLook(Current);
            GameEvents.RaiseLook();
        }

        public void ApplyTo(SlimeActor slime)
        {
            RebuildLook();
            slime.ApplyLook(Current);
        }

        SlimeCosmeticSO TextureByIndex(int index)
        {
            int cursor = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].slot != CosmeticSlot.Texture)
                    continue;
                if (cursor == index)
                    return catalog[i];
                cursor++;
            }
            return Find("tex_puro");
        }

        public int TextureCount()
        {
            int count = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].slot == CosmeticSlot.Texture)
                    count++;
            }
            return count;
        }

        public int TextureIndex(string id)
        {
            int cursor = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].slot != CosmeticSlot.Texture)
                    continue;
                if (catalog[i].id == id)
                    return cursor;
                cursor++;
            }
            return 0;
        }
    }
}
