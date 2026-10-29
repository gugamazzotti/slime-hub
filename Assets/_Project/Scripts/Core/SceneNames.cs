namespace SlimeHub.Core
{
    public static class SceneNames
    {
        public const string Boot = "0_Boot";
        public const string Hub = "1_Terrarium";
        public const string Tube = "2_MG_TubeEscape";
        public const string Critical = "3_MG_CriticalMass";
        public const string Climb = "4_MG_StickyClimb";
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
