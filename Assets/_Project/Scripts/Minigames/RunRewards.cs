using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Gameplay;

namespace SlimeHub.Minigames
{
    public static class RunRewards
    {
        public static bool Commit(string board, int score, int biomass, MissionMetric metric, int metricValue)
        {
            bool record = ServiceLocator.Get<ILeaderboardService>().Submit(board, score);
            ServiceLocator.Get<IDataService>().AddBiomass(Mathf.Max(1, biomass));
            ServiceLocator.Get<MissionService>().Submit(metric, metricValue);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(record ? Sfx.Merge : Sfx.Bloop, record ? 0.85f : 1f);
            return record;
        }
    }
}
