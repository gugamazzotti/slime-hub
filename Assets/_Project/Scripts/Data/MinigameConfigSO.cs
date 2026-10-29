using UnityEngine;

namespace SlimeHub.Data
{
    [CreateAssetMenu(menuName = "Slime Hub/Config de Minigame", fileName = "MinigameConfig")]
    public sealed class MinigameConfigSO : ScriptableObject
    {
        [Header("Fuga do Tubo")]
        public float tubeBaseSpeed = 7.4f;
        public float tubeSpeedGain = 0.05f;
        public float tubeHalfWidth = 2.45f;
        public float tubeStartMass = 18f;
        public float tubeMinMass = 5f;
        public float tubeHitLoss = 3.6f;
        public float tubePickup = 3.5f;
        public float tubeSplitCost = 0.04f;
        public int tubeSections = 12;
        public float tubeSectionLength = 16f;
        public float tubeDamageBoost = 2.6f;
        public float tubeDamageBoostTime = 1.35f;

        [Header("Massa Crítica")]
        public float critRestRadius = 0.62f;
        public float critMaxRadius = 1.7f;
        public float critChamberRadius = 2.12f;
        public float critIntegrity = 100f;
        public float critHazardDegrees = 86f;
        public float critSpin = 46f;
        public float critSpinGain = 3.4f;
        public float critDamage = 26f;

        [Header("Escalada Pegajosa")]
        public float climbHalfWidth = 1.8f;
        public float climbMaxPull = 2.2f;
        public float climbPower = 4.9f;
        public float climbGravity = 18f;
        public float climbSlide = 0.2f;
        public float climbSlideGain = 0.32f;
        public float climbAcid = 0.36f;
        public float climbAcidGain = 0.014f;

        public static MinigameConfigSO Load()
        {
            var asset = Resources.Load<MinigameConfigSO>("SlimeHub/MinigameConfig");
            if (asset != null)
                return asset;
            return CreateInstance<MinigameConfigSO>();
        }
    }
}
