namespace SlimeHub.Core
{
    public static class SceneNames
    {
        public const string Boot = "0_Boot";
        public const string Hub = "1_Terrarium";
        public const string Hop = "2_MG_JellyHop";
        public const string Merge = "3_MG_SlimeMerge";
        public const string Gulp = "4_MG_SlimeGulp";
    }

    public static class SceneFlow
    {
        public static void Load(string scene)
        {
            HitStop.Cancel();
            TimeScaleSafe();
            AppQuality.SetFrameRate(60);
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        static void TimeScaleSafe()
        {
            UnityEngine.Time.timeScale = 1f;
        }
    }
}
