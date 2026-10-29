using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Data;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.Physics;
using SlimeHub.UI;

namespace SlimeHub.Minigames
{
    public sealed class TubeEscapeGame : MonoBehaviour
    {
        GameStateMachine fsm;
        IGameState countdown;
        IGameState playing;
        IGameState paused;
        IGameState gameOver;
        MinigameShell shell;
        MinigameConfigSO config;
        LevelGenerator level;
        SlimeRunnerController runner;
        Camera cam;
        Transform walls;
        float shake;
        bool squeezing;
        float squeezeTime;
        bool squeezeSound;
        bool resolved;
        float overTime;
        int score;
        int reward;
        bool record;
        readonly RunnerBody[] bodies = new RunnerBody[3];
        readonly System.Collections.Generic.List<SlimeActor> globs = new System.Collections.Generic.List<SlimeActor>(8);
        readonly Transform[] bubbles = new Transform[16];

        void Awake()
        {
            GameServices.Ensure();
            config = MinigameConfigSO.Load();
            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 7.15f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.1f, 0.22f);
            cam.transform.rotation = Quaternion.identity;

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(backdrop.GetComponent<Collider>());
            backdrop.name = "Backdrop";
            backdrop.transform.localScale = new Vector3(16f, 32f, 1f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(
                new Color(0.08f, 0.12f, 0.28f, 1f), true, 0f, 0.55f, new Color(0.25f, 0.85f, 1f, 1f));
            walls = backdrop.transform;

            var levelRoot = new GameObject("Level").transform;
            level = new LevelGenerator(levelRoot, config);

            int subdiv = AppQuality.LowMemory ? 1 : 2;
            SlimeActor main = SlimeActor.Create(Vector3.zero, 0.5f, subdiv, false, true);
            SlimeActor left = SlimeActor.Create(Vector3.zero, 0.5f, 1, false, false);
            SlimeActor right = SlimeActor.Create(Vector3.zero, 0.5f, 1, false, false);
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(main);
            var membrane = new GameObject("Membrane");
            var membraneFilter = membrane.AddComponent<MeshFilter>();
            membraneFilter.sharedMesh = SlimeMeshFactory.SharedBlob(1, 0.22f);
            var membraneRenderer = membrane.AddComponent<MeshRenderer>();
            Material bridge = SlimeMaterials.CreateJelly();
            SlimeMaterials.ApplyJelly(bridge, new Color(0.45f, 0.95f, 0.62f, 0.72f), new Color(0.85f, 1f, 0.9f, 1f), 0.15f, 0.45f, 0f, 0f, 0.02f, 0.2f);
            membraneRenderer.sharedMaterial = bridge;
            membraneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            runner = gameObject.AddComponent<SlimeRunnerController>();
            runner.Configure(config.tubeStartMass, config.tubeHalfWidth, config.tubeMinMass, config.tubeSplitCost,
                config.tubeBaseSpeed, config.tubeSpeedGain, config.tubeDamageBoost, main, left, right, membrane.transform);

            GlassWall(-config.tubeHalfWidth - 0.2f);
            GlassWall(config.tubeHalfWidth + 0.2f);

            shell = gameObject.AddComponent<MinigameShell>();
            shell.Build(Loc.Get("game.tube"));
            shell.PausePressed += () => { if (fsm.Current == playing) fsm.Change(paused); };
            shell.ResumePressed += () => fsm.Change(playing);
            shell.Home += () => SceneFlow.Load(SceneNames.Hub);
            shell.Retry += () => SceneFlow.Load(SceneNames.Tube);
            shell.SetHint(Loc.Get("hud.steer"));
            DressBubbles();

            countdown = new LambdaState(TickCountdown, () => { });
            playing = new LambdaState(TickPlaying);
            paused = new LambdaState(null, () => { Time.timeScale = 0f; shell.ShowPause(true); }, () => { Time.timeScale = 1f; shell.ShowPause(false); });
            gameOver = new LambdaState(TickGameOver, EnterGameOver);
            fsm = new GameStateMachine();
            fsm.Change(countdown);
        }

        void DressBubbles()
        {
            Material bubble = SlimeMaterials.CreateUnlit(new Color(0.75f, 0.95f, 1f, 0.28f), false, 0.8f, 0f, Color.white);
            for (int i = 0; i < bubbles.Length; i++)
            {
                var view = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(view.GetComponent<Collider>());
                view.name = "Bubble";
                float size = 0.08f + (i % 5) * 0.035f;
                view.transform.localScale = Vector3.one * size;
                view.transform.SetParent(walls, false);
                view.transform.localPosition = new Vector3((i % 2 == 0 ? -1f : 1f) * (1.7f + (i % 4) * 0.15f), (i - 8f) * 1.15f, -0.4f);
                view.GetComponent<MeshRenderer>().sharedMaterial = bubble;
                bubbles[i] = view.transform;
            }
        }

        void DriftBubbles()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (bubbles[i] == null)
                    continue;
                Vector3 local = bubbles[i].localPosition;
                local.y += dt * (0.35f + (i % 3) * 0.18f);
                local.x += Mathf.Sin(Time.time * 1.4f + i) * dt * 0.08f;
                if (local.y > 9f)
                    local.y = -9f;
                bubbles[i].localPosition = local;
            }
        }

        void GlassWall(float x)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(wall.GetComponent<Collider>());
            wall.name = "TubeWall";
            wall.transform.position = new Vector3(x, 0f, 0.2f);
            wall.transform.localScale = new Vector3(0.18f, 30f, 0.2f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(new Color(0.55f, 0.95f, 1f, 0.38f), false, 0.9f, 0f, Color.white);
            wall.transform.SetParent(walls, true);
        }

        void Update()
        {
            fsm.Update();
        }

        void TickCountdown()
        {
            float t = countdownTime();
            FollowCamera();
        }

        float countT;
        int countStep = -1;

        float countdownTime()
        {
            countT += Time.unscaledDeltaTime;
            int step = countT < 0.7f ? 3 : countT < 1.4f ? 2 : countT < 2.1f ? 1 : 0;
            if (step != countStep)
            {
                countStep = step;
                shell.ShowCountdown(step > 0 ? step.ToString() : Loc.Get("shell.go"));
                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                    audio.Play(Sfx.Ui, step > 0 ? 1f : 1.35f);
            }
            if (countT > 2.55f)
            {
                shell.ShowCountdown("");
                fsm.Change(playing);
            }
            shell.SetHud(Mathf.RoundToInt(runner.Mass) + " kg", Loc.Get("hud.ready"));
            shell.SetMeter(0f);
            return countT;
        }

        void TickPlaying()
        {
            PointerState pointer = default;
            if (ServiceLocator.TryGet<TouchInputService>(out var input))
                pointer = input.State;

            float unitsPerPixel = cam.orthographicSize * 2f * cam.aspect / Mathf.Max(1f, Screen.width);
            bool allow = !pointer.OverUi && !squeezing;
            float steer = allow && pointer.Held ? pointer.Delta.x * unitsPerPixel : 0f;
            bool split = allow && pointer.Pinch;
            runner.Tick(Time.deltaTime, steer, split, pointer.Pinch01);
            level.Tick(runner.Y);
            ResolveContacts();
            AnimateSqueezer();
            DriftBubbles();
            FollowCamera();

            shell.SetHud(Mathf.RoundToInt(runner.Mass) + " kg", Loc.Format("hud.peak", Mathf.RoundToInt(runner.PeakMass)));
            float course = Mathf.Max(1f, config.tubeSections * config.tubeSectionLength);
            shell.SetMeter(runner.Y / course);
            if (!runner.Alive)
                fsm.Change(gameOver);
            else if (squeezing && squeezeTime > 1.05f)
                fsm.Change(gameOver);
        }

        void ResolveContacts()
        {
            int count = runner.CollectBodies(bodies);
            var segments = level.Alive;
            for (int s = 0; s < segments.Count; s++)
            {
                TubeSegment segment = segments[s];
                for (int h = 0; h < segment.Hazards.Count; h++)
                {
                    HazardPiece hazard = segment.Hazards[h];
                    if (hazard.Spent)
                        continue;
                    bool hit = false;
                    Vector2 hitPoint = Vector2.zero;
                    for (int b = 0; b < count; b++)
                    {
                        if (!Overlap(bodies[b], hazard.Bounds))
                            continue;
                        hit = true;
                        hitPoint = bodies[b].Point;
                        break;
                    }

                    if (!hit && !runner.IsSplit)
                    {
                        float leadX = Mathf.Lerp(runner.X, runner.AimX, 0.75f);
                        if (Mathf.Abs(leadX - runner.X) > 0.12f)
                        {
                            var lead = new RunnerBody(new Vector2(leadX, runner.Y), runner.RadiusOf(runner.Mass) * 0.45f, 0);
                            if (Overlap(lead, hazard.Bounds))
                            {
                                hit = true;
                                hitPoint = lead.Point;
                            }
                        }
                    }

                    if (!hit)
                        continue;
                    Capture(hazard, hitPoint);
                }

                for (int p = 0; p < segment.Pickups.Count; p++)
                {
                    PickupPiece pickup = segment.Pickups[p];
                    if (pickup.Spent)
                        continue;
                    for (int b = 0; b < count; b++)
                    {
                        Vector2 delta = bodies[b].Point - pickup.Point;
                        float reach = bodies[b].Radius + pickup.Radius;
                        if (delta.sqrMagnitude > reach * reach)
                            continue;
                        pickup.Spent = true;
                        if (pickup.View != null)
                            pickup.View.SetActive(false);
                        runner.Collect(pickup.Mass, new Vector3(pickup.Point.x, pickup.Point.y, 0f));
                        shake = 0.08f;
                        if (ServiceLocator.TryGet<IAudioService>(out var audio))
                            audio.Play(Sfx.Bloop, Mathf.Lerp(1.2f, 0.68f, runner.Mass / 60f));
                        if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                            haptics.Light();
                        shell.Popup("+" + pickup.Mass.ToString("0.0") + " kg", new Color(0.7f, 1f, 0.8f));
                        break;
                    }
                }
            }
        }

        void Capture(HazardPiece hazard, Vector2 hitPoint)
        {
            hazard.Spent = true;
            Vector3 from = new Vector3(hitPoint.x, hitPoint.y, 0f);
            runner.Tear(hazard.Mouth, config.tubeHitLoss);
            SlimeActor glob = BorrowGlob();
            float scale = Mathf.Clamp(runner.RadiusOf(config.tubeHitLoss) / 0.34f, 0.7f, 1.35f);
            TrappedGlob trapped = glob.GetComponent<TrappedGlob>();
            trapped.Launch(hazard.View.transform, from, hazard.InteriorLocal, scale);
            glob.Deformer.Pulse(1.6f);
            glob.Deformer.Nudge((hazard.Mouth - from).normalized * 6f);
            shell.Flash(new Color(1f, 0.12f, 0.16f, 0.5f));
            shake = 0.22f;
            HitStop.Play(0.12f, 0.05f);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Schlupp, Mathf.Lerp(1.15f, 0.7f, runner.Mass / 60f));
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Heavy();
            shell.Popup("-" + Mathf.RoundToInt(config.tubeHitLoss) + " kg", new Color(1f, 0.55f, 0.5f));
        }

        SlimeActor BorrowGlob()
        {
            for (int i = 0; i < globs.Count; i++)
            {
                if (!globs[i].gameObject.activeSelf)
                    return globs[i];
            }

            SlimeActor created = SlimeActor.Create(Vector3.zero, 0.34f, 1, false, false);
            created.gameObject.AddComponent<TrappedGlob>();
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(created);
            created.gameObject.SetActive(false);
            globs.Add(created);
            return created;
        }

        static bool Overlap(RunnerBody body, Bounds box)
        {
            float x = Mathf.Clamp(body.Point.x, box.min.x, box.max.x);
            float y = Mathf.Clamp(body.Point.y, box.min.y, box.max.y);
            float dx = body.Point.x - x;
            float dy = body.Point.y - y;
            return dx * dx + dy * dy <= body.Radius * body.Radius;
        }

        void AnimateSqueezer()
        {
            var segments = level.Alive;
            for (int i = 0; i < segments.Count; i++)
            {
                TubeSegment segment = segments[i];
                if (!segment.Squeezer || segment.LeftPlate == null)
                    continue;
                if (runner.Y > level.SqueezerY - 1.2f)
                    squeezing = true;
                if (!squeezing)
                    continue;
                squeezeTime += Time.deltaTime;
                segment.Squeeze = Mathf.Clamp01(squeezeTime / 0.7f);
                float inset = Mathf.Lerp(config.tubeHalfWidth + 0.4f, 0.35f, segment.Squeeze);
                segment.LeftPlate.position = new Vector3(-inset, level.SqueezerY, 0f);
                segment.RightPlate.position = new Vector3(inset, level.SqueezerY, 0f);
                runner.PlaySqueeze(segment.Squeeze);
                if (!squeezeSound && segment.Squeeze > 0.15f)
                {
                    squeezeSound = true;
                    if (ServiceLocator.TryGet<IAudioService>(out var audio))
                        audio.Play(Sfx.Squeezer, 0.85f);
                    if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                        haptics.Medium();
                }
            }
        }

        void FollowCamera()
        {
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.4f);
            float ox = Mathf.Sin(Time.unscaledTime * 70f) * shake;
            float focus = runner.Y + 2.5f;
            cam.transform.position = new Vector3(ox, focus, -12f);
            walls.position = new Vector3(0f, focus, 4f);
        }

        void EnterGameOver()
        {
            overTime = 0f;
            resolved = false;
            HitStop.Play(0.1f, 0.42f);
            bool success = runner.Alive && squeezing;
            score = Mathf.RoundToInt(runner.PeakMass * 14f + runner.Y * 0.45f + runner.Blobs * 12f);
            reward = Mathf.Max(2, Mathf.RoundToInt(runner.PeakMass * 0.7f + runner.Blobs));
            record = RunRewards.Commit(BoardIds.Tube, score, reward, MissionMetric.TubeMass, Mathf.RoundToInt(runner.PeakMass));
        }

        void TickGameOver()
        {
            overTime += Time.unscaledDeltaTime;
            FollowCamera();
            if (resolved || overTime < 0.42f)
                return;
            resolved = true;
            HitStop.Cancel();
            string title = runner.Alive ? Loc.Get("over.squeezed") : Loc.Get("over.dissolved");
            string body = Loc.Outcome(record, reward,
                Loc.Format("over.points", score),
                Loc.Format("over.peak", Mathf.RoundToInt(runner.PeakMass)));
            shell.ShowGameOver(title, body);
            shell.ShowCountdown("");
        }
    }

    public sealed class TrappedGlob : MonoBehaviour
    {
        Vector3 goal;
        Vector3 targetScale = Vector3.one;

        public void Launch(Transform tube, Vector3 worldFrom, Vector3 localGoal, float scale)
        {
            gameObject.SetActive(true);
            transform.SetParent(tube, true);
            transform.position = worldFrom;
            transform.rotation = Quaternion.identity;
            goal = localGoal;
            targetScale = Vector3.one * scale;
            transform.localScale = targetScale * 0.35f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, goal, dt * 3.4f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-5.5f * dt));
        }
    }
}
