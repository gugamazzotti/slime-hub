using UnityEngine;
using SlimeHub.Core;

namespace SlimeHub.Data
{
    [CreateAssetMenu(menuName = "Slime Hub/Missão", fileName = "DailyMission")]
    public sealed class DailyMissionSO : ScriptableObject
    {
        public string id = "tube_50";
        public string description = "Chegue a 50 kg no Fuga do Tubo";
        public MissionMetric metric = MissionMetric.TubeMass;
        public int target = 50;
        public int reward = 40;

        public static DailyMissionSO[] Pool()
        {
            return new[]
            {
                Make("tube_40", "Chegue a 40 kg no Fuga do Tubo", MissionMetric.TubeMass, 40, 35),
                Make("tube_50", "Chegue a 50 kg no Fuga do Tubo", MissionMetric.TubeMass, 50, 45),
                Make("tube_65", "Chegue a 65 kg no Fuga do Tubo", MissionMetric.TubeMass, 65, 55),
                Make("crit_30", "Sobreviva 30 s na Massa Crítica", MissionMetric.CriticalSeconds, 30, 35),
                Make("crit_45", "Sobreviva 45 s na Massa Crítica", MissionMetric.CriticalSeconds, 45, 45),
                Make("crit_60", "Sobreviva 1 minuto na Massa Crítica", MissionMetric.CriticalSeconds, 60, 60),
                Make("climb_12", "Escale 12 m na Escalada Pegajosa", MissionMetric.ClimbHeight, 12, 35),
                Make("climb_20", "Escale 20 m na Escalada Pegajosa", MissionMetric.ClimbHeight, 20, 45),
                Make("climb_30", "Escale 30 m na Escalada Pegajosa", MissionMetric.ClimbHeight, 30, 60)
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
