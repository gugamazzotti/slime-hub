using System;
using System.Globalization;
using SlimeHub.Core;
using SlimeHub.Data;

namespace SlimeHub.Gameplay
{
    public sealed class MissionService
    {
        readonly IDataService save;

        public MissionService(IDataService save)
        {
            this.save = save;
        }

        public void EnsureToday()
        {
            string today = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            PlayerData data = save.Data;
            if (data.missionDate == today && data.missions != null && data.missions.Count == 3)
                return;

            DailyMissionSO[] pool = DailyMissionSO.Pool();
            int seed = int.Parse(today, CultureInfo.InvariantCulture);
            var random = new System.Random(seed);
            data.missions.Clear();
            data.missionDate = today;
            AddRolled(data, pool, MissionMetric.TubeMass, random);
            AddRolled(data, pool, MissionMetric.CriticalSeconds, random);
            AddRolled(data, pool, MissionMetric.ClimbHeight, random);
            save.SaveSoon();
            GameEvents.RaiseMissions();
        }

        static void AddRolled(PlayerData data, DailyMissionSO[] pool, MissionMetric metric, System.Random random)
        {
            int matches = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].metric == metric)
                    matches++;
            }
            int pick = random.Next(matches);
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].metric != metric)
                    continue;
                if (pick-- > 0)
                    continue;
                data.missions.Add(new MissionSave
                {
                    id = pool[i].id,
                    description = pool[i].description,
                    metric = (int)pool[i].metric,
                    target = pool[i].target,
                    reward = pool[i].reward
                });
                return;
            }
        }

        public void Submit(MissionMetric metric, int value)
        {
            bool changed = false;
            var missions = save.Data.missions;
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i].metric != (int)metric)
                    continue;
                if (value > missions[i].progress)
                {
                    missions[i].progress = value;
                    changed = true;
                }
            }
            if (!changed)
                return;
            save.SaveSoon();
            GameEvents.RaiseMissions();
        }

        public bool TryClaim(string id)
        {
            var missions = save.Data.missions;
            for (int i = 0; i < missions.Count; i++)
            {
                MissionSave mission = missions[i];
                if (mission.id != id || mission.claimed || mission.progress < mission.target)
                    continue;
                mission.claimed = true;
                save.AddBiomass(mission.reward);
                save.SaveSoon();
                GameEvents.RaiseMissions();
                return true;
            }
            return false;
        }
    }
}
