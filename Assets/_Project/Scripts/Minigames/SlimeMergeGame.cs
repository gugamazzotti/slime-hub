using System.Collections.Generic;
using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Gameplay;
using SlimeHub.Input;
using SlimeHub.UI;

namespace SlimeHub.Minigames
{
    public sealed class SlimeMergeGame : MonoBehaviour
    {
        const int GoalTier = 4;

        enum Phase { Count, Play, Pause, Over }

        static readonly float[] Radii = { 0.32f, 0.42f, 0.54f, 0.7f, 0.9f };
        static readonly Color[] Colors =
        {
            new Color(0.78f, 0.92f, 0.32f),
            new Color(0.36f, 0.86f, 0.55f),
            new Color(0.35f, 0.74f, 0.95f),
            new Color(0.98f, 0.78f, 0.28f),
            new Color(0.95f, 0.48f, 0.62f)
        };

        readonly List<MergeBlob> alive = new List<MergeBlob>(16);
        readonly List<MergeBlob> pending = new List<MergeBlob>(4);
        MinigameShell shell;
        Camera cam;
        MergeBlob ghost;
        int nextTier;
        int score;
        float aimX;
        float cooldown;
        Phase phase = Phase.Count;
        Vector3 savedGravity;
        float countT;
        int countStep = -1;
        bool won;
        bool resolved;
        float overTime;
        PhysicsMaterial physicsMat;

        void Awake()
        {
            GameServices.Ensure();
            savedGravity = UnityEngine.Physics.gravity;
            UnityEngine.Physics.gravity = new Vector3(0f, -16f, 0f);
            cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 4.6f;
            cam.transform.position = new Vector3(0f, 3.15f, -12f);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.98f, 0.93f, 0.86f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(sky.GetComponent<Collider>());
            sky.transform.position = new Vector3(0f, 3.1f, 2f);
            sky.transform.localScale = new Vector3(12f, 12f, 1f);
            sky.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(
                new Color(1f, 0.9f, 0.78f, 1f), true, 0f, 0.12f, new Color(0.55f, 0.85f, 0.55f, 1f));

            physicsMat = new PhysicsMaterial("merge") { dynamicFriction = 0.4f, staticFriction = 0.45f, bounciness = 0.08f };
            Box(new Vector3(0f, -0.2f, 0f), new Vector3(5.2f, 0.4f, 1f), new Color(0.55f, 0.42f, 0.32f));
            Box(new Vector3(-2.35f, 3.1f, 0f), new Vector3(0.28f, 6.6f, 1f), new Color(0.72f, 0.9f, 0.7f));
            Box(new Vector3(2.35f, 3.1f, 0f), new Vector3(0.28f, 6.6f, 1f), new Color(0.72f, 0.9f, 0.7f));
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(line.GetComponent<Collider>());
            line.name = "Line";
            line.transform.position = new Vector3(0f, 5.15f, -0.2f);
            line.transform.localScale = new Vector3(4.2f, 0.06f, 0.2f);
            line.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(new Color(1f, 0.45f, 0.42f, 0.85f), false);

            nextTier = Random.value < 0.62f ? 0 : 1;
            ghost = Spawn(0, new Vector3(0f, 6.15f, 0f), true);

            shell = gameObject.AddComponent<MinigameShell>();
            shell.Build(Loc.Get("game.merge"));
            shell.SetHint(Loc.Get("hud.merge"));
            shell.PausePressed += () => { if (phase == Phase.Play) SetPhase(Phase.Pause); };
            shell.ResumePressed += () => SetPhase(Phase.Play);
            shell.Home += () => SceneFlow.Load(SceneNames.Hub);
            shell.Retry += () => SceneFlow.Load(SceneNames.Merge);
        }

        void OnDestroy()
        {
            UnityEngine.Physics.gravity = savedGravity;
        }

        void Box(Vector3 pos, Vector3 scale, Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.position = pos;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = SlimeMaterials.CreateUnlit(color, true);
            box.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        MergeBlob Spawn(int tier, Vector3 pos, bool held)
        {
            var go = new GameObject("Blob");
            go.transform.position = pos;
            float diameter = Radii[tier] * 2f;
            go.transform.localScale = Vector3.one * diameter;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = SlimeHub.Physics.SlimeMeshFactory.SharedBlob(1, 0.5f);
            var renderer = go.AddComponent<MeshRenderer>();
            Material jelly = SlimeMaterials.CreateJelly();
            Color color = Colors[tier];
            SlimeMaterials.ApplyJelly(jelly, color, Color.Lerp(color, Color.white, 0.55f), 0.05f, 0.2f, 0f, 0.1f, 0.02f, Radii[tier]);
            renderer.sharedMaterial = jelly;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var body = go.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            sphere.material = physicsMat;
            var blob = go.AddComponent<MergeBlob>();
            blob.Tier = tier;
            blob.Game = this;
            blob.Body = body;
            if (held)
            {
                body.isKinematic = true;
                body.useGravity = false;
                sphere.enabled = false;
            }
            return blob;
        }

        void Update()
        {
            if (phase == Phase.Count)
                TickCount();
            else if (phase == Phase.Play)
                TickPlay();
            else if (phase == Phase.Over)
                TickOver();
            for (int i = 0; i < alive.Count; i++)
                alive[i].Dress();
            if (ghost != null)
                ghost.Dress();
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
                    audio.Play(Sfx.Ui, step > 0 ? 1f : 1.25f);
            }
            if (countT > 2.3f)
            {
                shell.ShowCountdown("");
                phase = Phase.Play;
            }
            shell.SetHud("0", Loc.Get("hud.merge"));
            shell.SetMeter(0f);
        }

        void TickPlay()
        {
            cooldown -= Time.deltaTime;
            PointerState pointer = default;
            if (ServiceLocator.TryGet<TouchInputService>(out var input))
                pointer = input.State;
            if (pointer.Has && !pointer.OverUi)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(pointer.Position.x, pointer.Position.y, 12f));
                aimX = Mathf.Clamp(world.x, -2.05f + Radii[ghost.Tier], 2.05f - Radii[ghost.Tier]);
            }
            if (ghost != null)
                ghost.transform.position = new Vector3(aimX, 6.15f, 0f);
            if (pointer.Ended && !pointer.OverUi && cooldown <= 0f)
                Drop();

            ResolveMerges();
            if (phase == Phase.Over)
                return;
            for (int i = 0; i < alive.Count; i++)
            {
                MergeBlob blob = alive[i];
                if (blob.Dead || blob.Age < 0.8f)
                    continue;
                if (blob.Body.linearVelocity.magnitude < 0.45f && blob.transform.position.y > 5.15f)
                    blob.Danger += Time.deltaTime;
                else
                    blob.Danger = 0f;
                if (blob.Danger > 1.15f)
                {
                    Finish(false);
                    return;
                }
            }
            shell.SetHud(score.ToString(), Loc.Get("hud.merge"));
            shell.SetMeter(Mathf.Clamp01(score / 80f));
        }

        void Drop()
        {
            if (ghost == null)
                return;
            Vector3 pos = ghost.transform.position;
            if (UnityEngine.Physics.OverlapSphere(pos, Radii[ghost.Tier] * 0.9f).Length > 0)
            {
                ghost.Squash = 0.2f;
                return;
            }
            ghost.Body.isKinematic = false;
            ghost.Body.useGravity = true;
            ghost.GetComponent<SphereCollider>().enabled = true;
            ghost.Body.linearVelocity = new Vector3(0f, -1f, 0f);
            alive.Add(ghost);
            ghost = null;
            cooldown = 0.35f;
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Bloop, 1.1f, 0.7f);
            int tier = nextTier;
            nextTier = Random.value < 0.62f ? 0 : 1;
            ghost = Spawn(tier, new Vector3(aimX, 6.15f, 0f), true);
        }

        public void QueueMerge(MergeBlob a, MergeBlob b)
        {
            if (a.Dead || b.Dead || a.Tier != b.Tier)
                return;
            a.Dead = true;
            b.Dead = true;
            a.GetComponent<Collider>().enabled = false;
            b.GetComponent<Collider>().enabled = false;
            pending.Add(a);
            pending.Add(b);
        }

        void ResolveMerges()
        {
            if (pending.Count == 0)
                return;
            var batch = pending.ToArray();
            pending.Clear();
            for (int i = 0; i < batch.Length; i += 2)
            {
                MergeBlob a = batch[i];
                MergeBlob b = batch[i + 1];
                Vector3 at = (a.transform.position + b.transform.position) * 0.5f;
                int tier = a.Tier;
                bool top = tier >= GoalTier;
                score += 8 * (tier + 1);
                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                    audio.Play(top ? Sfx.Merge : Sfx.Pop, Mathf.Lerp(1.2f, 0.75f, tier / 4f));
                shell.Popup("+" + (8 * (tier + 1)), Colors[Mathf.Min(tier + 1, Colors.Length - 1)]);
                if (!top)
                {
                    MergeBlob born = Spawn(tier + 1, at, false);
                    born.Body.linearVelocity = new Vector3(0f, 1.4f, 0f);
                    born.Squash = 0.35f;
                    alive.Add(born);
                    if (tier + 1 >= GoalTier)
                    {
                        Finish(true);
                    }
                }
                Destroy(a.gameObject);
                Destroy(b.gameObject);
                alive.Remove(a);
                alive.Remove(b);
            }
        }

        void Finish(bool success)
        {
            if (phase == Phase.Over)
                return;
            won = success;
            phase = Phase.Over;
            overTime = 0f;
            resolved = false;
        }

        void TickOver()
        {
            overTime += Time.unscaledDeltaTime;
            if (resolved || overTime < 0.35f)
                return;
            resolved = true;
            int reward = Mathf.Max(2, score / 4);
            bool record = RunRewards.Commit(BoardIds.Merge, score, reward, MissionMetric.MergeScore, score);
            string title = won ? Loc.Get("over.mergewin") : Loc.Get("over.mergelose");
            shell.ShowGameOver(title, Loc.Outcome(record, reward, Loc.Format("over.points", score)));
        }

        void SetPhase(Phase next)
        {
            phase = next;
            Time.timeScale = next == Phase.Pause ? 0f : 1f;
            shell.ShowPause(next == Phase.Pause);
        }
    }

    public sealed class MergeBlob : MonoBehaviour
    {
        public int Tier;
        public SlimeMergeGame Game;
        public Rigidbody Body;
        public bool Dead;
        public float Age;
        public float Danger;
        public float Squash;

        void OnCollisionEnter(Collision collision)
        {
            if (Dead)
                return;
            Squash = Mathf.Clamp(collision.relativeVelocity.magnitude / 8f, 0.08f, 0.4f);
            var other = collision.collider.GetComponent<MergeBlob>();
            if (other == null || other.Dead || other.Tier != Tier)
                return;
            if (GetInstanceID() > other.GetInstanceID())
                return;
            Game.QueueMerge(this, other);
        }

        public void Dress()
        {
            if (!Body.isKinematic)
                Age += Time.deltaTime;
            Squash = Mathf.MoveTowards(Squash, 0f, Time.deltaTime * 2.2f);
            float wobble = Mathf.Sin(Time.time * 3f + Tier) * 0.03f;
            float x = 1f + Squash + wobble;
            float y = 1f - Squash * 0.8f + wobble;
            Vector3 baseScale = Vector3.one * (SlimeMergeGameRadius() * 2f);
            transform.localScale = new Vector3(baseScale.x * x, baseScale.y * y, baseScale.z);
        }

        float SlimeMergeGameRadius()
        {
            float[] radii = { 0.32f, 0.42f, 0.54f, 0.7f, 0.9f };
            return radii[Mathf.Clamp(Tier, 0, radii.Length - 1)];
        }
    }
}
