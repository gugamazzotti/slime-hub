using System;
using System.Collections.Generic;

namespace SlimeHub.Data
{
    [Serializable]
    public class ScoreEntry
    {
        public string id;
        public int score;
    }

    [Serializable]
    public class MissionSave
    {
        public string id;
        public string description;
        public int metric;
        public int target;
        public int progress;
        public int reward;
        public bool claimed;
    }

    [Serializable]
    public class FusionSave
    {
        public string id;
        public string label;
        public float r;
        public float g;
        public float b;
        public int texture;
        public int core;
    }

    [Serializable]
    public class PlayerData
    {
        public int version = 1;
        public int bioMassCurrency = 80;
        public string activeColorID = "color_menta";
        public string activeTextureID = "tex_puro";
        public string activeCoreID = "core_none";
        public string activeFusionID = "";
        public float viscosity = 0.35f;
        public bool muted;
        public bool haptics = true;
        public string missionDate = "";
        public List<string> unlockedItems = new List<string>();
        public List<ScoreEntry> minigameHighScores = new List<ScoreEntry>();
        public List<MissionSave> missions = new List<MissionSave>();
        public List<FusionSave> fusions = new List<FusionSave>();

        public void EnsureCollections()
        {
            unlockedItems ??= new List<string>();
            minigameHighScores ??= new List<ScoreEntry>();
            missions ??= new List<MissionSave>();
            fusions ??= new List<FusionSave>();
            if (string.IsNullOrEmpty(activeColorID)) activeColorID = "color_menta";
            if (string.IsNullOrEmpty(activeTextureID)) activeTextureID = "tex_puro";
            if (string.IsNullOrEmpty(activeCoreID)) activeCoreID = "core_none";
            if (activeFusionID == null) activeFusionID = "";
        }

        public int GetScore(string id)
        {
            if (minigameHighScores == null) return 0;
            for (int i = 0; i < minigameHighScores.Count; i++)
            {
                if (minigameHighScores[i].id == id)
                    return minigameHighScores[i].score;
            }
            return 0;
        }

        public void SetScore(string id, int score)
        {
            minigameHighScores ??= new List<ScoreEntry>();
            for (int i = 0; i < minigameHighScores.Count; i++)
            {
                if (minigameHighScores[i].id == id)
                {
                    minigameHighScores[i].score = score;
                    return;
                }
            }
            minigameHighScores.Add(new ScoreEntry { id = id, score = score });
        }

        public bool IsUnlocked(string id)
        {
            if (unlockedItems == null) return false;
            for (int i = 0; i < unlockedItems.Count; i++)
            {
                if (unlockedItems[i] == id)
                    return true;
            }
            return false;
        }

        public void Unlock(string id)
        {
            unlockedItems ??= new List<string>();
            if (!IsUnlocked(id))
                unlockedItems.Add(id);
        }

        public FusionSave FindFusion(string id)
        {
            if (fusions == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < fusions.Count; i++)
            {
                if (fusions[i].id == id)
                    return fusions[i];
            }
            return null;
        }
    }
}
