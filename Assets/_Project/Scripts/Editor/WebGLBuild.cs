#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlimeHub.EditorTools
{
    public static class WebGLBuild
    {
        public static void Build()
        {
            string output = ResolveOutput();
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);

            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.WebGL.template = "PROJECT:SlimeHub";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            EditorUserBuildSettings.development = false;

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                string failure = "failed " + report.summary.result + " errors=" + report.summary.totalErrors;
                File.WriteAllText(StatusPath(), failure);
                throw new Exception("WebGL build failed: " + failure);
            }

            File.WriteAllText(Path.Combine(output, ".nojekyll"), string.Empty);
            File.WriteAllText(StatusPath(), "ready " + output);
            Debug.Log("WebGL build ready at " + output);
        }

        static string ResolveOutput()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-customBuildPath" && !string.IsNullOrEmpty(args[i + 1]))
                    return Path.GetFullPath(args[i + 1]);
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));
        }

        static string StatusPath()
        {
            string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(logs);
            return Path.Combine(logs, "webgl-status.txt");
        }
    }
}
#endif
