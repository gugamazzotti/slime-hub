using UnityEngine;
using SlimeHub.Core;

namespace SlimeHub.Data
{
    [CreateAssetMenu(menuName = "Slime Hub/Missão", fileName = "DailyMission")]
    public sealed class DailyMissionSO : ScriptableObject
    {
        public string id = "hop_8";
        public string description = "Quique em 8 plataformas";
        public MissionMetric metric = MissionMetric.HopLands;
        public int target = 8;
        public int reward = 40;

        public static DailyMissionSO[] Pool()
        {
            return new[]
            {
                Make("hop_6", "Quique em 6 plataformas", MissionMetric.HopLands, 6, 30),
                Make("hop_8", "Quique em 8 plataformas", MissionMetric.HopLands, 8, 40),
                Make("hop_10", "Quique em 10 plataformas", MissionMetric.HopLands, 10, 55),
                Make("merge_40", "Faça 40 pontos na Fusão", MissionMetric.MergeScore, 40, 30),
                Make("merge_80", "Faça 80 pontos na Fusão", MissionMetric.MergeScore, 80, 45),
                Make("merge_120", "Faça 120 pontos na Fusão", MissionMetric.MergeScore, 120, 60),
                Make("gulp_4", "Absorva 4 petiscos", MissionMetric.GulpEaten, 4, 30),
                Make("gulp_6", "Absorva 6 petiscos", MissionMetric.GulpEaten, 6, 40),
                Make("gulp_8", "Absorva 8 petiscos", MissionMetric.GulpEaten, 8, 55)
            };
        }

        static DailyMissionSO Make(string id, string description, MissionMetric metric, int target, int reward)
        {
            var mission = CreateInstance<DailyMissionSO>();
            mission.id = id;
            mission.description = description;
            mission.metric = metric;
            mission.target = target;
            mission.reward = reward;
            mission.name = id;
            return mission;
        }
    }
}
