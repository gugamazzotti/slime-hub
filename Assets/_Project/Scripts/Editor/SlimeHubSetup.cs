#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SlimeHub.EditorTools
{
    public static class SlimeHubSetup
    {
        const string SettingsDir = "Assets/_Project/Settings";
        const string RendererPath = SettingsDir + "/SlimeHub_Renderer.asset";
        const string PipelinePath = SettingsDir + "/SlimeHub_URP.asset";
        const string ResourceDir = "Assets/_Project/Resources/SlimeHub";

        [MenuItem("Slime Hub/Instalar pipeline e materiais")]
        public static void InstallPipeline()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(ResourceDir);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.msaaSampleCount = (int)MsaaQuality._2x;
            pipeline.shadowDistance = 0f;
            pipeline.supportsHDR = false;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.renderScale = 1f;

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(Mathf.Clamp(current, 0, names.Length - 1), false);

            CreateMaterial("SlimeHub/SlimeJelly", ResourceDir + "/M_SlimeJelly.mat");
            CreateMaterial("SlimeHub/SlimeGlow", ResourceDir + "/M_SlimeGlow.mat");
            CreateMaterial("SlimeHub/SlimeUnlit", ResourceDir + "/M_SlimeUnlit.mat");

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log("Slime Hub: pipeline 3D e materiais prontos.");
        }

        static void CreateMaterial(string shaderName, string path)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("Shader ausente: " + shaderName);
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }
    }

    [InitializeOnLoad]
    static class SlimeHubPipelineBootstrap
    {
        static SlimeHubPipelineBootstrap()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_Project/Settings/SlimeHub_URP.asset") != null)
                    return;
                SlimeHubSetup.InstallPipeline();
            };
        }
    }
}
#endif
