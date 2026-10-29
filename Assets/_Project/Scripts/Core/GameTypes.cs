namespace SlimeHub.Core
{
    public enum Sfx
    {
        Pop,
        Bubble,
        Bloop,
        Slap,
        Schlupp,
        Merge,
        Ui,
        Squeezer,
        Stretch
    }

    public enum MissionMetric
    {
        TubeMass = 0,
        CriticalSeconds = 1,
        ClimbHeight = 2
    }

    public enum CoreKind
    {
        None = 0,
        Duck = 1,
        Skull = 2,
        Galaxy = 3
    }

    public enum CosmeticSlot
    {
        Color = 0,
        Texture = 1,
        Core = 2
    }

    public static class BoardIds
    {
        public const string Tube = "tube";
        public const string Critical = "critical";
        public const string Climb = "climb";
    }
}
