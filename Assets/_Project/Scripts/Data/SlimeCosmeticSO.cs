using System.Collections.Generic;
using UnityEngine;
using SlimeHub.Core;

namespace SlimeHub.Data
{
    [CreateAssetMenu(menuName = "Slime Hub/Cosmético", fileName = "SlimeCosmetic")]
    public sealed class SlimeCosmeticSO : ScriptableObject
    {
        public string id = "color_menta";
        public string displayName = "Menta";
        public CosmeticSlot slot = CosmeticSlot.Color;
        public int cost;
        public bool unlockedByDefault = true;
        public Color color = new Color(0.45f, 0.95f, 0.62f, 0.9f);
        public Color rim = new Color(0.85f, 1f, 0.82f, 1f);
        public float glitter;
        public float emission = 0.15f;
        public float metallic;
        public float pulse;
        public CoreKind core = CoreKind.None;

        public static List<SlimeCosmeticSO> RuntimeCatalog()
        {
            return new List<SlimeCosmeticSO>
            {
                ColorItem("color_menta", "Menta", 0, true, new Color(0.42f, 0.95f, 0.62f, 0.9f), new Color(0.82f, 1f, 0.78f, 1f)),
                ColorItem("color_cereja", "Cereja", 30, false, new Color(0.95f, 0.28f, 0.4f, 0.9f), new Color(1f, 0.72f, 0.78f, 1f)),
                ColorItem("color_oceano", "Oceano", 30, false, new Color(0.22f, 0.72f, 0.98f, 0.9f), new Color(0.7f, 0.95f, 1f, 1f)),
                ColorItem("color_uva", "Uva", 40, false, new Color(0.62f, 0.36f, 0.96f, 0.9f), new Color(0.86f, 0.72f, 1f, 1f)),
                ColorItem("color_lava", "Lava", 50, false, new Color(0.98f, 0.42f, 0.12f, 0.9f), new Color(1f, 0.82f, 0.45f, 1f)),
                ColorItem("color_fantasma", "Fantasma", 40, false, new Color(0.86f, 0.96f, 0.96f, 0.55f), new Color(1f, 1f, 1f, 1f)),

                TextureItem("tex_puro", "Puro", 0, true, 0f, 0.12f, 0f, 0f),
                TextureItem("tex_glitter", "Glitter", 60, false, 1f, 0.28f, 0.08f, 0f),
                TextureItem("tex_radioativo", "Radioativo", 100, false, 0.2f, 1.5f, 0f, 1f),
                TextureItem("tex_neon", "Neon", 80, false, 0.4f, 1.25f, 0.05f, 0.25f),
                TextureItem("tex_metalico", "Metálico", 120, false, 0.08f, 0.08f, 1f, 0f),

                CoreItem("core_none", "Sem núcleo", 0, true, CoreKind.None),
                CoreItem("core_duck", "Pato", 70, false, CoreKind.Duck),
                CoreItem("core_skull", "Caveirinha", 90, false, CoreKind.Skull),
                CoreItem("core_galaxy", "Galáxia", 150, false, CoreKind.Galaxy)
            };
        }

        static SlimeCosmeticSO ColorItem(string id, string name, int cost, bool free, Color color, Color rim)
        {
            var item = CreateInstance<SlimeCosmeticSO>();
            item.id = id;
            item.displayName = name;
            item.slot = CosmeticSlot.Color;
            item.cost = cost;
            item.unlockedByDefault = free;
            item.color = color;
            item.rim = rim;
            item.name = name;
            return item;
        }

        static SlimeCosmeticSO TextureItem(string id, string name, int cost, bool free, float glitter, float emission, float metallic, float pulse)
        {
            var item = CreateInstance<SlimeCosmeticSO>();
            item.id = id;
            item.displayName = name;
            item.slot = CosmeticSlot.Texture;
            item.cost = cost;
            item.unlockedByDefault = free;
            item.glitter = glitter;
            item.emission = emission;
            item.metallic = metallic;
            item.pulse = pulse;
            item.name = name;
            return item;
        }

        static SlimeCosmeticSO CoreItem(string id, string name, int cost, bool free, CoreKind core)
        {
            var item = CreateInstance<SlimeCosmeticSO>();
            item.id = id;
            item.displayName = name;
            item.slot = CosmeticSlot.Core;
            item.cost = cost;
            item.unlockedByDefault = free;
            item.core = core;
            item.name = name;
            return item;
        }
    }
}
