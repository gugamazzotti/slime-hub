using System.Collections.Generic;
using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.UI;

namespace SlimeHub.Minigames
{
    public sealed class JellyHopGame : MonoBehaviour
    {
        const int Goal = 10;

        enum Phase { Count, Play, Pause, Over }

        MinigameShell shell;
        Camera cam;
        SlimeActor slime;
        Rigidbody body;
        readonly List<Transform> pads = new List<Transform>(12);
        Phase phase = Phase.Count;
        float countT;
        int countStep = -1;
        int lands;
        bool won;
        bool resolved;
        float overTime;
        float squash;
        float nextPadY = 0.4f;
        float camY = 1.2f;
        bool grounded;
        float nextLand;
        Vector3 savedGravity;
        Material padMat;
        Transform sky;

        void Awake()
        {
            GameServices.Ensure();
            savedGravity = UnityEngine.Physics.gravity;
            UnityEngine.Physics.gravity = new Vector3(0f, -20f, 0f);
            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.transform.rotation = Quaternion.identity;
            cam.transform.position = new Vector3(0f, camY, -12f);
            cam.backgroundColor = new Color(0.93f, 0.97f, 0.86f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var skyObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(skyObject.GetComponent<Collider>());
            skyObject.name = "Sky";
            sky = skyObject.transform;
            sky.localScale = new Vector3(16f, 28f, 1f);
            skyObject.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(
                new Color(0.86f, 0.96f, 0.78f, 1f), true, 0f, 0.22f, new Color(1f, 0.72f, 0.84f, 1f));

            padMat = SlimeMaterials.CreateUnlit(new Color(0.45f, 0.86f, 0.55f, 1f), true, 0.15f, 0f, Color.white);
            for (int i = 0; i < 8; i++)
                SpawnPad(i == 0);

            slime = SlimeActor.Create(new Vector3(0f, 1.05f, 0f), 0.42f, 1, true, true);
            ServiceLocator.Get<SlimeCustomizer>().ApplyTo(slime);
            body = slime.Body;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            body.linearDamping = 0.05f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            slime.gameObject.AddComponent<HopFeet>().Game = this;

            shell = gameObject.AddComponent<MinigameShell>();
            shell.Build(Loc.Get("game.hop"));
            shell.SetHint(Loc.Get("hud.hop"));
            shell.PausePressed += () => { if (phase == Phase.Play) SetPhase(Phase.Pause); };
            shell.ResumePressed += () => SetPhase(Phase.Play);
            shell.Home += () => SceneFlow.Load(SceneNames.Hub);
            shell.Retry += () => SceneFlow.Load(SceneNames.Hop);
        }

        void OnDestroy()
        {
            UnityEngine.Physics.gravity = savedGravity;
        }

        void SpawnPad(bool wide)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            bool easy = wide || pads.Count < 3;
            float width = easy ? 3.3f : Random.Range(1.8f, 2.6f);
            float x = easy ? 0f : Random.Range(-1.45f, 1.45f);
            pad.transform.position = new Vector3(x, nextPadY, 0f);
            pad.transform.localScale = new Vector3(width, 0.28f, 0.7f);
            pad.GetComponent<MeshRenderer>().sharedMaterial = padMat;
            pad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pads.Add(pad.transform);
            nextPadY += Random.Range(1.85f, 2.15f);
        }

        void Update()
        {
            if (phase == Phase.Count)
                TickCount();
            else if (phase == Phase.Play)
                TickPlay();
            else if (phase == Phase.Over)
                TickOver();
            DressSlime();
            if (sky != null)
                sky.position = new Vector3(0f, camY, 3f);
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
            shell.SetHud("0", Loc.Get("hud.hop"));
            shell.SetMeter(0f);
            if (countT > 2.3f)
            {
                shell.ShowCountdown("");
                SetPhase(Phase.Play);
            }
        }

        void TickPlay()
        {
            PointerState pointer = default;
            if (ServiceLocator.TryGet<TouchInputService>(out var input))
                pointer = input.State;
            if (!pointer.OverUi && pointer.Has)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(pointer.Position.x, pointer.Position.y, 12f));
                float target = Mathf.Clamp(world.x, -2.15f, 2.15f);
                Vector3 velocity = body.linearVelocity;
                velocity.x = Mathf.Lerp(velocity.x, (target - body.position.x) * 5.5f, 1f - Mathf.Exp(-8f * Time.deltaTime));
                velocity.z = 0f;
                body.linearVelocity = velocity;
            }

            if (body.position.y > camY - 1.2f)
                camY = Mathf.Lerp(camY, body.position.y + 1.4f, 1f - Mathf.Exp(-4f * Time.deltaTime));
            cam.transform.position = new Vector3(0f, camY, -12f);

            while (nextPadY < camY + 10f)
                SpawnPad(false);
            for (int i = pads.Count - 1; i >= 0; i--)
            {
                if (pads[i].position.y >= camY - 8f)
                    continue;
                Destroy(pads[i].gameObject);
                pads.RemoveAt(i);
            }

            shell.SetHud(lands + " / " + Goal, Loc.Get("hud.hop"));
            shell.SetMeter(lands / (float)Goal);
            if (body.position.y < camY - 7.2f)
                Finish(false);
            else if (lands >= Goal)
                Finish(true);
        }

        public void Land(Vector3 normal)
        {
            if (normal.y < 0.55f || grounded || phase == Phase.Pause || phase == Phase.Over)
                return;
            if (Time.time < nextLand || body.linearVelocity.y > 2f)
                return;
            grounded = true;
            nextLand = Time.time + 0.45f;
            squash = 0.42f;
            Vector3 velocity = body.linearVelocity;
            velocity.y = 9.6f;
            velocity.z = 0f;
            body.linearVelocity = velocity;
            if (phase != Phase.Play)
                return;
            lands++;
            slime.Deformer.Pulse(1.1f);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Pop, Mathf.Lerp(1.25f, 0.85f, lands / (float)Goal), 0.75f);
            shell.Popup("+" + lands, new Color(0.35f, 0.75f, 0.4f));
        }

        public void LeaveGround()
        {
            grounded = false;
        }

        void DressSlime()
        {
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 2.6f);
            float rise = body != null ? Mathf.Clamp(body.linearVelocity.y / 14f, -0.15f, 0.22f) : 0f;
            float x = 1f + squash * 0.7f - rise * 0.35f;
            float y = 1f - squash * 0.75f + rise;
            slime.transform.localScale = new Vector3(x, y, x);
        }

        void Finish(bool success)
        {
            if (phase == Phase.Over)
                return;
            won = success;
            phase = Phase.Over;
            overTime = 0f;
            resolved = false;
            body.linearVelocity = Vector3.zero;
            if (!success && ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Bloop, 0.8f);
        }

        void TickOver()
        {
            overTime += Time.unscaledDeltaTime;
            if (resolved || overTime < 0.35f)
                return;
            resolved = true;
            int reward = Mathf.Max(2, lands * 3);
            bool record = RunRewards.Commit(BoardIds.Hop, lands, reward, MissionMetric.HopLands, lands);
            string title = won ? Loc.Get("over.hopwin") : Loc.Get("over.hoplose");
            shell.ShowGameOver(title, Loc.Outcome(record, reward, Loc.Format("over.lands", lands)));
            shell.ShowCountdown("");
        }

        void SetPhase(Phase next)
        {
            phase = next;
            if (next == Phase.Pause)
            {
                Time.timeScale = 0f;
                shell.ShowPause(true);
            }
            else if (next == Phase.Play)
            {
                Time.timeScale = 1f;
                shell.ShowPause(false);
            }
        }
    }

    public sealed class HopFeet : MonoBehaviour
    {
        public JellyHopGame Game;

        void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount == 0)
                return;
            Game?.Land(collision.GetContact(0).normal);
        }

        void OnCollisionExit(Collision collision)
        {
            Game?.LeaveGround();
        }
    }
}
