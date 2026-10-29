using UnityEngine;

namespace SlimeHub.Gameplay
{
    public static class SlimeMaterials
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        static readonly int GlitterId = Shader.PropertyToID("_Glitter");
        static readonly int EmissionId = Shader.PropertyToID("_Emission");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int WobbleId = Shader.PropertyToID("_WobbleAmp");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int GridColorId = Shader.PropertyToID("_GridColor");
        static readonly int GridStrengthId = Shader.PropertyToID("_GridStrength");
        static readonly int FresnelId = Shader.PropertyToID("_Fresnel");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        static Shader jellyShader;
        static Shader glowShader;
        static Shader unlitShader;
        static Shader photoShader;
        static bool missingLogged;

        public static Material CreateJelly()
        {
            Material material = New(ref jellyShader, "SlimeHub/SlimeJelly");
            material.renderQueue = 3000;
            return material;
        }

        public static Material CreateGlow()
        {
            Material material = New(ref glowShader, "SlimeHub/SlimeGlow");
            material.renderQueue = 3100;
            return material;
        }

        public static Material CreateUnlit(Color color, bool zWrite, float fresnel = 0f, float grid = 0f, Color? gridColor = null)
        {
            Material material = New(ref unlitShader, "SlimeHub/SlimeUnlit");
            material.SetColor(BaseColorId, color);
            material.SetFloat(ZWriteId, zWrite ? 1f : 0f);
            material.SetFloat(FresnelId, fresnel);
            material.SetFloat(GridStrengthId, grid);
            material.SetColor(GridColorId, gridColor ?? new Color(0.45f, 0.95f, 0.85f, 1f));
            material.renderQueue = zWrite ? 2000 : 3000;
            return material;
        }

        public static Material CreatePhoto(Texture texture)
        {
            Material material = New(ref photoShader, "SlimeHub/LabPhoto");
            material.SetColor(BaseColorId, texture != null ? Color.white : new Color(0.36f, 0.32f, 0.55f, 1f));
            if (texture != null)
                material.SetTexture("_BaseMap", texture);
            material.renderQueue = 1000;
            return material;
        }

        public static void ApplyJelly(Material material, Color color, Color rim, float glitter, float emission, float metallic, float pulse, float wobble, float radius)
        {
            if (material == null)
                return;
            material.SetColor(BaseColorId, color);
            material.SetColor(RimColorId, rim);
            material.SetFloat(GlitterId, glitter);
            material.SetFloat(EmissionId, emission);
            material.SetFloat(MetallicId, metallic);
            material.SetFloat(PulseId, pulse);
            material.SetFloat(WobbleId, wobble);
            material.SetFloat(RadiusId, Mathf.Max(0.05f, radius));
        }

        public static void ApplyGlow(Material material, Color color, float wobble)
        {
            if (material == null)
                return;
            color.a = 0.16f;
            material.SetColor(BaseColorId, color);
            material.SetFloat(WobbleId, wobble);
        }

        static Material New(ref Shader shader, string shaderName)
        {
            string file = shaderName.Substring(shaderName.LastIndexOf('/') + 1);
            Material source = Resources.Load<Material>("SlimeHub/M_" + file);
            if (source != null)
                return new Material(source);

            if (shader == null)
                shader = Shader.Find(shaderName);
            if (shader == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogError("Shader não encontrado: " + shaderName);
                }
                shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Color");
            }
            return new Material(shader);
        }
    }
}
