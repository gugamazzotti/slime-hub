#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

namespace SlimeHub.EditorTools
{
    public sealed class AndroidVibratePostprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 50;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest))
                manifest = Path.Combine(path, "unityLibrary", "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest))
                return;

            string text = File.ReadAllText(manifest);
            const string permission = "android.permission.VIBRATE";
            if (text.Contains(permission))
                return;

            text = text.Replace("<application", "<uses-permission android:name=\"" + permission + "\" />\n  <application");
            File.WriteAllText(manifest, text);
        }
    }
}
#endif
