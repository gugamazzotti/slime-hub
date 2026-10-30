using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.UI;

namespace SlimeHub.Minigames
{
    public sealed class SlimeGulpGame : MonoBehaviour
    {
        const int Goal = 8;

        enum Phase { Count, Play, Pause, Over }

        MinigameShell shell;
        Camera cam;
        SlimeActor slime;
        Transform food;
        Transform spike;
        Phase phase = Phase.Count;
        float countT;
        int countStep = -1;
        int eaten;
        bool won;
        bool resolved;
        float overTime;
        float chew;
        Vector3 foodVel;
        Vector3 spikeVel;

        void Awake()
        {
            GameServices.Ensure();
            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.transform.position = new Vector3(0f, 0.2f, -12f);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.9f, 0.95f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(sky.GetComponent<Collider>());
            sky.transform.position = new Vector3(0f, 0.2f, 3f);
            sky.transform.localScale = new Vector3(14f, 16f, 1f);
            sky.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(
                new Color(0.78f, 0.9f, 1f, 1f), true, 0f, 0.16f, new Color(0.55f, 0.95f, 0.7f, 1f));

            slime = SlimeActor.Create(new Vector3(0f, -1.2f, 0f), 0.55f, 1, false, true);
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(slime);
            food = Bit("Food", new Color(0.45f, 0.9f, 0.42f), 0.34f);
            spike = Bit("Spike", new Color(0.95f, 0.38f, 0.4f), 0.38f);
            Place(food, true);
            Place(spike, false);

            shell = gameObject.AddComponent<MinigameShell>();
            shell.Build(Loc.Get("game.gulp"));
            shell.SetHint(Loc.Get("hud.gulp"));
            shell.PausePressed += () => { if (phase == Phase.Play) SetPhase(Phase.Pause); };
            shell.ResumePressed += () => SetPhase(Phase.Play);
            shell.Home += () => SceneFlow.Load(SceneNames.Hub);
            shell.Retry += () => SceneFlow.Load(SceneNames.Gulp);
        }

        Transform Bit(string name, Color color, float radius)
        {
            var go = new GameObject(name);
            go.transform.localScale = Vector3.one * radius * 2f;
            go.AddComponent<MeshFilter>().sharedMesh = SlimeHub.Physics.SlimeMeshFactory.SharedBlob(1, 0.5f);
            var renderer = go.AddComponent<MeshRenderer>();
            Material jelly = SlimeMaterials.CreateJelly();
            SlimeMaterials.ApplyJelly(jelly, color, Color.Lerp(color, Color.white, 0.5f), 0f, 0.25f, 0f, 0.2f, 0.03f, radius);
            renderer.sharedMaterial = jelly;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void Place(Transform bit, bool snack)
        {
            float x = Random.Range(-2f, 2f);
            float y = Random.Range(-2.4f, 2.6f);
            if (slime != null && (bit.position - slime.transform.position).sqrMagnitude < 1.2f)
                y = 2.2f;
            bit.position = new Vector3(x, y, 0f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.right;
            Vector3 vel = new Vector3(dir.x, dir.y, 0f) * (snack ? 1.15f : 0.85f);
            if (snack)
                foodVel = vel;
            else
                spikeVel = vel;
        }

        void Update()
        {
            if (phase == Phase.Count)
                TickCount();
            else if (phase == Phase.Play)
                TickPlay();
            else if (phase == Phase.Over)
                TickOver();
            chew = Mathf.MoveTowards(chew, 0f, Time.deltaTime * 2.4f);
            float x = 1f + chew * 0.45f;
            float y = 1f - chew * 0.35f;
            slime.transform.localScale = new Vector3(x, y, x);
            float wobble = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            food.localScale = Vector3.one * (0.68f * wobble);
            spike.localScale = Vector3.one * (0.76f * (1f + Mathf.Sin(Time.time * 7f) * 0.06f));
        }

        void TickCount()
        {
            countT += Time.unscaledDeltaTime;
            int step = countT < 0.65f ? 3 : countT < 1.3f ? 2 : countT < 1.95f ? 1 : 0;
            if (step != countStep)
            {
                countStep = step;
                shell.ShowCountdown(step > 0 ? step.ToString() : Loc.Get("shell.go"));
                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                    audio.Play(Sfx.Ui, step > 0 ? 1f : 1.3f);
            }
            shell.SetHud("0 / " + Goal, Loc.Get("hud.gulp"));
            shell.SetMeter(0f);
            if (countT > 2.3f)
            {
                shell.ShowCountdown("");
                phase = Phase.Play;
            }
        }

        void TickPlay()
        {
            PointerState pointer = default;
            if (ServiceLocator.TryGet<TouchInputService>(out var input))
                pointer = input.State;
            if (pointer.Held && !pointer.OverUi)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(pointer.Position.x, pointer.Position.y, 12f));
                world.x = Mathf.Clamp(world.x, -2.3f, 2.3f);
                world.y = Mathf.Clamp(world.y, -3.3f, 3.1f);
                world.z = 0f;
                slime.transform.position = Vector3.Lerp(slime.transform.position, world, 1f - Mathf.Exp(-14f * Time.deltaTime));
            }

            Move(food, ref foodVel, 1.15f);
            Move(spike, ref spikeVel, 0.9f);
            float reach = 0.7f;
            if ((food.position - slime.transform.position).sqrMagnitude < reach * reach)
                Eat();
            if ((spike.position - slime.transform.position).sqrMagnitude < 0.62f * 0.62f)
                Finish(false);

            shell.SetHud(eaten + " / " + Goal, Loc.Get("hud.gulp"));
            shell.SetMeter(eaten / (float)Goal);
            if (eaten >= Goal)
                Finish(true);
        }

        void Move(Transform bit, ref Vector3 velocity, float speed)
        {
            Vector3 pos = bit.position + velocity * Time.deltaTime;
            if (pos.x < -2.4f || pos.x > 2.4f)
                velocity.x = -velocity.x;
            if (pos.y < -3.4f || pos.y > 3.3f)
                velocity.y = -velocity.y;
            pos.x = Mathf.Clamp(pos.x, -2.4f, 2.4f);
            pos.y = Mathf.Clamp(pos.y, -3.4f, 3.3f);
            pos.z = 0f;
            bit.position = pos;
            velocity = velocity.normalized * speed;
        }

        void Eat()
        {
            eaten++;
            chew = 0.4f;
            slime.Deformer.Pulse(1.4f);
            shell.Popup("+1", new Color(0.3f, 0.75f, 0.4f));
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Bloop, Mathf.Lerp(1.3f, 0.8f, eaten / (float)Goal));
            Place(food, true);
            if (Random.value < 0.5f)
                Place(spike, false);
        }

        void Finish(bool success)
        {
            if (phase == Phase.Over)
                return;
            won = success;
            phase = Phase.Over;
            overTime = 0f;
            resolved = false;
            if (!success)
                shell.Flash(new Color(1f, 0.3f, 0.3f, 0.45f));
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(success ? Sfx.Merge : Sfx.Schlupp, success ? 0.9f : 1f);
        }

        void TickOver()
        {
            overTime += Time.unscaledDeltaTime;
            if (resolved || overTime < 0.35f)
                return;
            resolved = true;
            int reward = Mathf.Max(2, eaten * 4);
            bool record = RunRewards.Commit(BoardIds.Gulp, eaten, reward, MissionMetric.GulpEaten, eaten);
            string title = won ? Loc.Get("over.gulpwin") : Loc.Get("over.gulplose");
            shell.ShowGameOver(title, Loc.Outcome(record, reward, Loc.Format("over.eaten", eaten)));
            shell.ShowCountdown("");
        }

        void SetPhase(Phase next)
        {
            phase = next;
            Time.timeScale = next == Phase.Pause ? 0f : 1f;
            shell.ShowPause(next == Phase.Pause);
        }
    }
}
