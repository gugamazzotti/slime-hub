using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Data;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.UI;

namespace SlimeHub.Minigames
{
    public sealed class CriticalMassGame : MonoBehaviour
    {
        GameStateMachine fsm;
        IGameState countdown;
        IGameState playing;
        IGameState paused;
        IGameState gameOver;
        MinigameShell shell;
        MinigameConfigSO config;
        SlimeActor slime;
        Transform ring;
        Camera cam;
        float radius;
        float radiusVelocity;
        float spin;
        float angle;
        float integrity;
        float survived;
        float pressure;
        float overTime;
        bool resolved;
        bool record;
        int score;
        int reward;
        float countT;
        int countStep = -1;
        float nextHurt;
        float dentUntil;

        void Awake()
        {
            GameServices.Ensure();
            config = MinigameConfigSO.Load();
            radius = config.critRestRadius;
            integrity = config.critIntegrity;
            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.06f, 0.18f);
            cam.transform.position = new Vector3(0f, 0f, -12f);
            cam.transform.rotation = Quaternion.identity;

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(backdrop.GetComponent<Collider>());
            backdrop.transform.position = new Vector3(0f, 0f, 2f);
            backdrop.transform.localScale = new Vector3(14f, 18f, 1f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(
                new Color(0.12f, 0.07f, 0.22f, 1f), true, 0f, 0.18f, new Color(0.65f, 0.4f, 1f, 1f));

            ring = new GameObject("Ring").transform;
            BuildRing();

            slime = SlimeActor.Create(Vector3.zero, 0.5f, AppQuality.LowMemory ? 1 : 2, false, true);
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(slime);

            shell = gameObject.AddComponent<MinigameShell>();
            shell.Build(Loc.Get("game.critical"));
            shell.PausePressed += () => { if (fsm.Current == playing) fsm.Change(paused); };
            shell.ResumePressed += () => fsm.Change(playing);
            shell.Home += () => SceneFlow.Load(SceneNames.Hub);
            shell.Retry += () => SceneFlow.Load(SceneNames.Critical);
            shell.SetHint(Loc.Get("hud.hold"));

            countdown = new LambdaState(TickCountdown);
            playing = new LambdaState(TickPlaying);
            paused = new LambdaState(null, () => { Time.timeScale = 0f; shell.ShowPause(true); }, () => { Time.timeScale = 1f; shell.ShowPause(false); });
            gameOver = new LambdaState(TickOver, EnterOver);
            fsm = new GameStateMachine();
            fsm.Change(countdown);
        }

        void BuildRing()
        {
            int count = 28;
            float chamber = config.critChamberRadius;
            for (int i = 0; i < count; i++)
            {
                float deg = i * (360f / count);
                bool hazard = deg > 180f && deg < 180f + config.critHazardDegrees;
                var block = GameObject.CreatePrimitive(hazard ? PrimitiveType.Cube : PrimitiveType.Sphere);
                Destroy(block.GetComponent<Collider>());
                block.name = hazard ? "Blade" : "Glass";
                block.transform.SetParent(ring, false);
                float rad = deg * Mathf.Deg2Rad;
                float depth = hazard ? 0.72f : 0.34f;
                float dist = chamber - depth * 0.35f;
                block.transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * dist;
                block.transform.localRotation = Quaternion.Euler(0f, 0f, deg);
                block.transform.localScale = hazard
                    ? new Vector3(0.22f, depth, 0.22f)
                    : Vector3.one * 0.42f;
                Color color = hazard
                    ? new Color(1f, 0.28f, 0.22f, 1f)
                    : new Color(0.62f, 0.92f, 1f, 0.55f);
                block.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(color, false, hazard ? 0.15f : 0.85f, 0f, Color.white);
            }
        }

        void Update() => fsm.Update();

        void TickCountdown()
        {
            countT += Time.unscaledDeltaTime;
            int step = countT < 0.7f ? 3 : countT < 1.4f ? 2 : countT < 2.1f ? 1 : 0;
            if (step != countStep)
            {
                countStep = step;
                shell.ShowCountdown(step > 0 ? step.ToString() : Loc.Get("shell.go"));
                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                    audio.Play(Sfx.Ui, step > 0 ? 1f : 1.3f);
            }
            PlaceSlime(false);
            shell.SetHud(Mathf.CeilToInt(integrity) + "%", Loc.Get("hud.hold"));
            shell.SetMeter(1f);
            if (countT > 2.5f)
            {
                shell.ShowCountdown("");
                fsm.Change(playing);
            }
        }

        void TickPlaying()
        {
            float dt = Time.deltaTime;
            survived += dt;
            bool hold = false;
            if (ServiceLocator.TryGet<TouchInputService>(out var input))
            {
                PointerState pointer = input.State;
                hold = pointer.Held && !pointer.OverUi;
            }

            float target = hold ? config.critMaxRadius : config.critRestRadius;
            radius = Mathf.SmoothDamp(radius, target, ref radiusVelocity, hold ? 0.09f : 0.14f);
            spin = config.critSpin + survived * config.critSpinGain;
            angle += spin * dt;
            ring.localRotation = Quaternion.Euler(0f, 0f, angle);

            bool inBlade = BladeAtContact();
            float limit = config.critChamberRadius - (inBlade ? 0.78f : 0.05f);
            bool crushed = radius > limit;
            if (crushed && inBlade)
            {
                integrity -= config.critDamage * dt;
                if (Time.unscaledTime > nextHurt)
                {
                    nextHurt = Time.unscaledTime + 0.28f;
                    shell.Flash(new Color(1f, 0.1f, 0.15f, 0.45f));
                    slime.Deformer.Impact(slime.transform.position + Vector3.right * radius, Vector3.left, 2.4f);
                    slime.Deformer.Pulse(0.9f);
                    dentUntil = Time.time + 0.16f;
                    if (ServiceLocator.TryGet<IAudioService>(out var audio))
                        audio.Play(Sfx.Schlupp, 1.1f, 0.8f);
                    if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                        haptics.Heavy();
                }
            }
            else if (radius > 1.2f && !inBlade)
            {
                pressure += dt * radius;
                if (Time.unscaledTime > nextHurt)
                {
                    nextHurt = Time.unscaledTime + 0.22f;
                    if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                        haptics.Light();
                }
            }

            PlaceSlime(radius > 1.15f);
            shell.SetHud(Mathf.CeilToInt(Mathf.Max(0f, integrity)) + "%", Mathf.FloorToInt(survived) + " s");
            shell.SetMeter(Mathf.Clamp01(integrity / Mathf.Max(1f, config.critIntegrity)));
            if (integrity <= 0f)
                fsm.Change(gameOver);
        }

        bool BladeAtContact()
        {
            float contact = -angle;
            float wrapped = Mathf.Repeat(contact, 360f);
            return wrapped > 180f && wrapped < 180f + config.critHazardDegrees;
        }

        void PlaceSlime(bool pressed)
        {
            float center = Mathf.Max(0.2f, config.critChamberRadius - radius);
            float dt = Mathf.Max(Time.deltaTime, Time.unscaledDeltaTime);
            Vector3 target = new Vector3(center, Mathf.Sin(Time.time * 2.2f) * 0.04f, 0f);
            slime.transform.position = Vector3.Lerp(slime.transform.position, target, 1f - Mathf.Exp(-10f * dt));
            float scale = radius / 0.5f;
            slime.transform.localScale = Vector3.Lerp(slime.transform.localScale, Vector3.one * scale, 1f - Mathf.Exp(-7f * dt));
            slime.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.6f) * 7f);
            slime.ClearGameplayStretch();
            if (Time.time < dentUntil)
                slime.Deformer.SetFlow(Vector3.left * 0.75f);
            else if (pressed)
            {
                float bulge = Mathf.Lerp(0.4f, 1.15f, radius / Mathf.Max(0.2f, config.critMaxRadius));
                slime.Deformer.SetFlow(Vector3.right * bulge);
            }
            else
            {
                slime.Deformer.SetFlow(Vector3.zero);
            }

            slime.Deformer.Nudge(new Vector3(-radiusVelocity * 3.2f, 0f, 0f));
        }

        void EnterOver()
        {
            overTime = 0f;
            resolved = false;
            HitStop.Play(0.1f, 0.42f);
            score = Mathf.RoundToInt(survived * 10f + pressure * 8f);
            reward = Mathf.Max(2, Mathf.RoundToInt(survived * 1.4f));
            record = RunRewards.Commit(BoardIds.Critical, score, reward, MissionMetric.CriticalSeconds, Mathf.FloorToInt(survived));
        }

        void TickOver()
        {
            overTime += Time.unscaledDeltaTime;
            if (resolved || overTime < 0.42f)
                return;
            resolved = true;
            HitStop.Cancel();
            string body = Loc.Outcome(record, reward,
                Loc.Format("over.points", score),
                Loc.Format("over.seconds", Mathf.FloorToInt(survived)));
            shell.ShowGameOver(Loc.Get("over.centrifuge"), body);
            shell.ShowCountdown("");
        }
    }
}
