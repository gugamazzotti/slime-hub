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
        HopLands = 0,
        MergeScore = 1,
        GulpEaten = 2
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
        public const string Hop = "hop";
        public const string Merge = "merge";
        public const string Gulp = "gulp";
    }
}
