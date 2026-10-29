using UnityEngine;
using SlimeHub.Audio;
using SlimeHub.Data;
using SlimeHub.Gameplay;
using SlimeHub.Haptics;
using SlimeHub.Input;
using SlimeHub.UI;

namespace SlimeHub.Core
{
    public sealed class GameServicesHost : MonoBehaviour
    {
        public static GameServicesHost Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            HitStop.Tick();
            JuiceTween.Tick();
            AppQuality.Tick();
            if (ServiceLocator.TryGet<IDataService>(out var save))
                save.TickSave();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Flush();
        }

        void OnApplicationQuit()
        {
            Flush();
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;
            Flush();
            Instance = null;
            ServiceLocator.Clear();
            GameEvents.Clear();
        }

        static void Flush()
        {
            if (ServiceLocator.TryGet<IDataService>(out var save))
                save.Flush();
        }
    }

    public static class GameServices
    {
        public static void Ensure()
        {
            Loc.ApplySystem();
            if (GameServicesHost.Instance != null)
                return;

            ServiceLocator.Clear();
            GameEvents.Clear();
            AppQuality.Apply();

            var root = new GameObject("[SlimeHub]");
            root.AddComponent<GameServicesHost>();

            var save = new SaveSystem();
            save.LoadSync();
            var audio = root.AddComponent<AudioService>();
            audio.SetMuted(save.Data.muted);
            var haptics = new HapticsService { Enabled = save.Data.haptics };
            var input = root.AddComponent<TouchInputService>();
            var customizer = new SlimeCustomizer(save);
            var missions = new MissionService(save);
            missions.EnsureToday();
            var fusion = new LocalFusionService(save, customizer);
            var board = new LocalLeaderboard(save);

            ServiceLocator.Register<IDataService>(save);
            ServiceLocator.Register<IAudioService>(audio);
            ServiceLocator.Register<IHapticsService>(haptics);
            ServiceLocator.Register(input);
            ServiceLocator.Register(customizer);
            ServiceLocator.Register(missions);
            ServiceLocator.Register<IFusionService>(fusion);
            ServiceLocator.Register<ILeaderboardService>(board);
            UiFactory.EnsureEventSystem();
        }
    }
}
