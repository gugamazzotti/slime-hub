using System;

namespace SlimeHub.Core
{
    public static class GameEvents
    {
        public static event Action<int> BiomassChanged;
        public static event Action LookChanged;
        public static event Action MissionsChanged;

        public static void RaiseBiomass(int value) => BiomassChanged?.Invoke(value);
        public static void RaiseLook() => LookChanged?.Invoke();
        public static void RaiseMissions() => MissionsChanged?.Invoke();

        public static void Clear()
        {
            BiomassChanged = null;
            LookChanged = null;
            MissionsChanged = null;
        }
    }
}
