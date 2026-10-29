using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlimeHub
{
    public sealed class SlimeGame : MonoBehaviour
    {
        public static SlimeGame Instance { get; private set; }

        private const float CameraY = 6.15f;
        private const float ViewHalf = 8f;
        private const float InnerHalf = 3.62f;
        private const float FloorY = 0.42f;
        private const float DropY = 12.15f;
        private const float DangerY = 10.55f;
        private const float DropCooldown = 0.32f;

        private enum Phase { Title, Play, Over }

        private readonly List<SlimeBlob> alive = new List<SlimeBlob>();
        private readonly List<Vector2Int> merges = new List<Vector2Int>();
        private readonly List<SlimeBlob> lookup = new List<SlimeBlob>();

        private Camera cam;
        private Transform camRig;
        private Vector3 camBase;
        private AudioSource audioSource;
        private Material blobMat;
        private Mesh quad;
        private PhysicsMaterial2D physicsMat;
        private Transform decorRoot;

        private SlimeBlob ghost;
        private Transform nextPreview;
        private MeshRenderer nextRenderer;
        private int nextTier;
        private float aimX;
        private float cooldown;
        private float trauma;
        private float chainTimer;
        private int chain;
        private int score;
        private int best;
        private bool pointerArmed;
        private bool ignoreMouse;
        private Phase phase = Phase.Title;

        private Font font;
        private Text titleText;
        private Text subText;
        private Text hintText;
        private Text scoreText;
        private Text bestText;
        private Text comboText;
        private Text nextLabel;
        private float hintLeft = 5f;
        private float scorePunch = 1f;

        private struct Mote
        {
            public Transform Transform;
            public Vector2 Velocity;
            public float Life;
            public MeshRenderer Renderer;
        }

        private readonly List<Mote> motes = new List<Mote>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindObjectOfType<SlimeGame>() != null) return;
            var go = new GameObject("SlimeHub");
            go.AddComponent<SlimeGame>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            best = PlayerPrefs.GetInt("SlimeHub.Best", 0);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.pixelLightCount = 0;
            Physics2D.gravity = new Vector2(0f, -18f);
            SlimeAudio.Warmup();
            LoadArt();
            BuildCamera();
            BuildJar();
            BuildHud();
            ShowTitle();
        }

        private void LoadArt()
        {
            blobMat = Resources.Load<Material>("Materials/Blob");
            if (blobMat == null)
            {
                var shader = Shader.Find("SlimeHub/Blob");
                blobMat = new Material(shader);
            }

            quad = new Mesh { name = "slime-quad" };
            quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            quad.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();

            physicsMat = new PhysicsMaterial2D("slime")
            {
                bounciness = 0.08f,
                friction = 0.45f
            };
        }

        private void BuildCamera()
        {
            var rig = new GameObject("CameraRig");
            camRig = rig.transform;
            camBase = new Vector3(0f, CameraY, -10f);
            camRig.position = camBase;

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(camRig, false);
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ViewHalf;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.10f, 0.08f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 40f;
            cam.allowMSAA = false;
            audioSource = camGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            camGo.AddComponent<AudioListener>();
        }

        private void BuildJar()
        {
            decorRoot = new GameObject("Decor").transform;
            Paint(decorRoot, "back", new Vector2(0f, 6.1f), new Vector2(7.15f, 11.5f), new Color(0.10f, 0.15f, 0.12f, 1f), 0f, 2f);
            Paint(decorRoot, "floor", new Vector2(0f, 0.12f), new Vector2(7.7f, 0.55f), new Color(0.18f, 0.28f, 0.20f, 1f), 0f, -0.2f);
            Paint(decorRoot, "wall-l", new Vector2(-3.86f, 6.2f), new Vector2(0.42f, 12.1f), new Color(0.20f, 0.32f, 0.24f, 1f), 0f, -0.2f);
            Paint(decorRoot, "wall-r", new Vector2(3.86f, 6.2f), new Vector2(0.42f, 12.1f), new Color(0.20f, 0.32f, 0.24f, 1f), 0f, -0.2f);
            Paint(decorRoot, "lip", new Vector2(0f, 11.85f), new Vector2(7.7f, 0.28f), new Color(0.24f, 0.38f, 0.28f, 1f), 0f, -0.25f);
            Paint(decorRoot, "danger", new Vector2(0f, DangerY), new Vector2(6.9f, 0.045f), new Color(1f, 0.42f, 0.52f, 0.55f), 0f, -0.3f);
            Paint(decorRoot, "glow-l", new Vector2(-2.3f, 4.2f), new Vector2(2.4f, 2.4f), new Color(0.35f, 0.8f, 0.45f, 0.08f), 1f, 3f);
            Paint(decorRoot, "glow-r", new Vector2(2.1f, 8.4f), new Vector2(2.8f, 2.8f), new Color(0.45f, 0.7f, 0.95f, 0.07f), 1f, 3.2f);

            AddBox(new Vector2(0f, -0.05f), new Vector2(8.4f, 0.7f));
            AddBox(new Vector2(-3.95f, 6.2f), new Vector2(0.5f, 13f));
            AddBox(new Vector2(3.95f, 6.2f), new Vector2(0.5f, 13f));

            var next = new GameObject("next");
            nextPreview = next.transform;
            nextPreview.position = new Vector3(2.55f, 13.25f, -0.1f);
            var filter = next.AddComponent<MeshFilter>();
            filter.sharedMesh = quad;
            nextRenderer = next.AddComponent<MeshRenderer>();
            nextRenderer.sharedMaterial = blobMat;
            nextRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            nextRenderer.receiveShadows = false;
            Tint(nextRenderer, SlimeTiers.ColorOf(0), 1f, 1f);
        }

        private void AddBox(Vector2 center, Vector2 size)
        {
            var go = new GameObject("collider");
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        private void Paint(Transform parent, string name, Vector2 pos, Vector2 size, Color color, float round, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, z);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = quad;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = blobMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Tint(renderer, color, color.a, round);
        }

        private static void Tint(Renderer renderer, Color color, float alpha, float round)
        {
            color.a = alpha;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            block.SetColor("_Highlight", new Color(1f, 1f, 1f, 0.35f));
            block.SetFloat("_Round", round);
            renderer.SetPropertyBlock(block);
        }

        private void BuildHud()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            titleText = MakeText(canvasGo.transform, "SLIME HUB", 92, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(900f, 140f));
            subText = MakeText(canvasGo.transform, "Junte dois slimes iguais.", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.55f), new Vector2(860f, 160f));
            hintText = MakeText(canvasGo.transform, "Arraste para mirar e solte para cair.", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.16f), new Vector2(900f, 180f));
            scoreText = MakeText(canvasGo.transform, "0", 72, TextAnchor.UpperLeft, new Vector2(0.06f, 0.95f), new Vector2(520f, 100f));
            bestText = MakeText(canvasGo.transform, "", 34, TextAnchor.UpperRight, new Vector2(0.94f, 0.955f), new Vector2(520f, 70f));
            comboText = MakeText(canvasGo.transform, "", 40, TextAnchor.UpperLeft, new Vector2(0.06f, 0.90f), new Vector2(520f, 70f));
            nextLabel = MakeText(canvasGo.transform, "próxima", 30, TextAnchor.MiddleCenter, new Vector2(0.78f, 0.915f), new Vector2(280f, 50f));
            scoreText.rectTransform.pivot = new Vector2(0f, 1f);
            bestText.rectTransform.pivot = new Vector2(1f, 1f);
            comboText.rectTransform.pivot = new Vector2(0f, 1f);
            var gold = new Color(0.96f, 0.86f, 0.45f);
            bestText.color = gold;
            comboText.color = new Color(0.72f, 1f, 0.62f);
        }

        private Text MakeText(Transform parent, string content, int size, TextAnchor anchor, Vector2 anchorPos, Vector2 sizeDelta)
        {
            var go = new GameObject("text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = new Color(0.93f, 0.97f, 0.90f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = anchorPos;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            return text;
        }

        private void ShowTitle()
        {
            phase = Phase.Title;
            titleText.gameObject.SetActive(true);
            subText.gameObject.SetActive(true);
            hintText.gameObject.SetActive(true);
            titleText.text = "SLIME HUB";
            subText.text = "Junte dois slimes iguais.\nEles crescem até entrar no hub.";
            SetHint("Arraste para mirar e solte para cair.\nToque para começar.");
            scoreText.gameObject.SetActive(false);
            bestText.gameObject.SetActive(false);
            comboText.gameObject.SetActive(false);
            nextLabel.gameObject.SetActive(false);
            if (nextPreview != null) nextPreview.gameObject.SetActive(false);
        }

        private void StartRound()
        {
            ClearBoard();
            score = 0;
            chain = 0;
            chainTimer = 0f;
            cooldown = 0.15f;
            aimX = 0f;
            phase = Phase.Play;
            hintLeft = 4.5f;
            titleText.gameObject.SetActive(false);
            subText.gameObject.SetActive(false);
            scoreText.gameObject.SetActive(true);
            bestText.gameObject.SetActive(true);
            comboText.gameObject.SetActive(true);
            nextLabel.gameObject.SetActive(true);
            nextPreview.gameObject.SetActive(true);
            SetHint("Arraste e solte. Teclado: ← → e espaço.");
            nextTier = SlimeTiers.RollDrop();
            SpawnGhost(SlimeTiers.RollDrop());
            RefreshNext();
            RefreshScore();
        }

        private void ClearBoard()
        {
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] != null) Destroy(alive[i].gameObject);
            }
            alive.Clear();
            merges.Clear();
            lookup.Clear();
            for (int i = 0; i < motes.Count; i++)
            {
                if (motes[i].Transform != null) Destroy(motes[i].Transform.gameObject);
            }
            motes.Clear();
        }

        private void GameOver()
        {
            if (phase != Phase.Play) return;
            phase = Phase.Over;
            if (score > best)
            {
                best = score;
                PlayerPrefs.SetInt("SlimeHub.Best", best);
                PlayerPrefs.Save();
            }
            if (ghost != null) ghost.gameObject.SetActive(false);
            nextPreview.gameObject.SetActive(false);
            nextLabel.gameObject.SetActive(false);
            titleText.gameObject.SetActive(true);
            subText.gameObject.SetActive(true);
            hintText.gameObject.SetActive(true);
            titleText.text = "Transbordou";
            subText.text = score + " pontos\nrecorde " + best;
            SetHint("Toque para tentar de novo.");
            scoreText.gameObject.SetActive(false);
            bestText.gameObject.SetActive(false);
            comboText.gameObject.SetActive(false);
            SlimeAudio.PlayOver(audioSource);
            trauma = Mathf.Max(trauma, 0.8f);
            foreach (var blob in alive)
            {
                if (blob != null && blob.Body != null) blob.Body.simulated = false;
            }
        }

        public void QueueMerge(SlimeBlob a, SlimeBlob b)
        {
            if (a == null || b == null || a.Dead || b.Dead) return;
            a.MarkDead();
            b.MarkDead();
            merges.Add(new Vector2Int(a.GetInstanceID(), b.GetInstanceID()));
            lookup.Add(a);
            lookup.Add(b);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            ReadInput(dt);
            cooldown -= dt;
            chainTimer -= dt;
            if (chainTimer <= 0f) chain = 0;

            ResolveMerges();
            TickBlobs(dt);
            TickMotes(dt);
            TickDanger(dt);
            TickCamera(dt);
            TickDecor();
            FitCamera();
            TickHud(dt);
        }

        private void ReadInput(float dt)
        {
            bool pressed = false;
            bool released = false;
            bool havePointer = false;
            Vector2 world = Vector2.zero;

            if (Input.touchCount > 0)
            {
                ignoreMouse = true;
                var t = Input.GetTouch(0);
                havePointer = true;
                world = ScreenToWorld(t.position);
                pressed = t.phase == TouchPhase.Began;
                released = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            }
            else if (ignoreMouse)
            {
                if (!Input.GetMouseButton(0)) ignoreMouse = false;
            }
            else
            {
                havePointer = true;
                world = ScreenToWorld(Input.mousePosition);
                pressed = Input.GetMouseButtonDown(0);
                released = Input.GetMouseButtonUp(0);
            }

            if (phase != Phase.Play)
            {
                bool keyStart = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
                if (released || keyStart) StartRound();
                return;
            }

            float kb = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) kb -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) kb += 1f;
            if (kb != 0f) aimX += kb * 7.2f * dt;
            else if (havePointer) aimX = world.x;

            if (pressed) pointerArmed = true;
            bool keyDrop = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            if ((released && pointerArmed) || keyDrop)
            {
                pointerArmed = false;
                TryDrop();
            }

            if (ghost != null)
            {
                float limit = Mathf.Max(0.2f, InnerHalf - ghost.Radius - 0.04f);
                aimX = Mathf.Clamp(aimX, -limit, limit);
                ghost.Place(new Vector2(aimX, DropY));
            }
        }

        private Vector2 ScreenToWorld(Vector3 screen)
        {
            screen.z = 10f;
            var w = cam.ScreenToWorldPoint(screen);
            return new Vector2(w.x, w.y);
        }

        private void TryDrop()
        {
            if (phase != Phase.Play || ghost == null || cooldown > 0f) return;
            var pos = new Vector2(aimX, DropY);
            if (PhysicsBlocked(pos, ghost.Radius))
            {
                trauma = Mathf.Max(trauma, 0.15f);
                return;
            }

            ghost.Release();
            alive.Add(ghost);
            ghost = null;
            cooldown = DropCooldown;
            SlimeAudio.PlayDrop(audioSource);
            int dropped = nextTier;
            nextTier = SlimeTiers.RollDrop();
            SpawnGhost(dropped);
            RefreshNext();
        }

        private bool PhysicsBlocked(Vector2 pos, float radius)
        {
            var hits = Physics2D.OverlapCircleAll(pos, radius * 0.9f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].GetComponent<SlimeBlob>() != null) return true;
            }
            return false;
        }

        private void SpawnGhost(int tier)
        {
            ghost = CreateBlob(tier, true, new Vector2(aimX, DropY));
        }

        private SlimeBlob CreateBlob(int tier, bool held, Vector2 position)
        {
            var go = new GameObject("slime-" + tier);
            float z = -0.02f * (alive.Count + 1);
            go.transform.position = new Vector3(position.x, position.y, z);
            var blob = go.AddComponent<SlimeBlob>();
            blob.Init(tier, held, blobMat, quad, physicsMat);
            blob.Place(position);
            return blob;
        }

        private void RefreshNext()
        {
            float d = SlimeTiers.Radius(nextTier) * 2f * 0.72f;
            nextPreview.localScale = new Vector3(d, d, 1f);
            Tint(nextRenderer, SlimeTiers.ColorOf(nextTier), 1f, 1f);
        }

        private void ResolveMerges()
        {
            if (merges.Count == 0) return;
            var pending = merges.ToArray();
            merges.Clear();
            for (int i = 0; i < pending.Length; i++)
            {
                var a = FindBlob(pending[i].x);
                var b = FindBlob(pending[i].y);
                if (a == null || b == null) continue;
                Vector2 at = (Vector2)a.transform.position + (Vector2)b.transform.position;
                at *= 0.5f;
                int tier = a.Tier;
                bool hub = tier >= SlimeTiers.Count - 1;
                int resultTier = Mathf.Min(tier + 1, SlimeTiers.Count - 1);
                if (chainTimer > 0f) chain++;
                else chain = 1;
                chainTimer = 0.9f;
                int gained = (hub ? 150 : 12 * resultTier) * chain;
                score += gained;
                scorePunch = 1.28f;
                trauma = Mathf.Max(trauma, hub ? 0.7f : 0.22f + tier * 0.04f);
                Burst(at, SlimeTiers.ColorOf(tier), hub ? 14 : 7);
                if (hub) SlimeAudio.PlayHub(audioSource);
                else SlimeAudio.PlayMerge(audioSource, resultTier);
                if (!hub)
                {
                    var born = CreateBlob(resultTier, false, at);
                    born.Body.velocity = new Vector2(0f, 1.6f);
                    alive.Add(born);
                }
                RefreshScore();
            }
            lookup.Clear();
        }

        private SlimeBlob FindBlob(int id)
        {
            for (int i = 0; i < lookup.Count; i++)
            {
                if (lookup[i] != null && lookup[i].GetInstanceID() == id) return lookup[i];
            }
            return null;
        }

        private void TickBlobs(float dt)
        {
            if (ghost != null) ghost.Tick(dt);
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                var blob = alive[i];
                if (blob == null)
                {
                    alive.RemoveAt(i);
                    continue;
                }
                blob.Tick(dt);
                if (blob.Dead && blob.transform.localScale.x >= 0f)
                {
                    var visual = blob.transform.childCount > 0 ? blob.transform.GetChild(0) : null;
                    if (visual != null && visual.localScale.x <= 0.05f)
                    {
                        Destroy(blob.gameObject);
                        alive.RemoveAt(i);
                    }
                }
            }
        }

        private void TickDanger(float dt)
        {
            if (phase != Phase.Play) return;
            var line = decorRoot.Find("danger");
            bool hot = false;
            for (int i = 0; i < alive.Count; i++)
            {
                var blob = alive[i];
                if (blob == null || blob.Dead || blob.Held || blob.Age < 0.85f)
                {
                    if (blob != null) blob.AboveTime = 0f;
                    continue;
                }
                bool calm = blob.Body.velocity.magnitude < 0.7f;
                bool above = blob.transform.position.y - blob.Radius * 0.15f > DangerY;
                if (calm && above) blob.AboveTime += dt;
                else blob.AboveTime = 0f;
                if (blob.AboveTime > 0.2f) hot = true;
                if (blob.AboveTime > 1.25f)
                {
                    GameOver();
                    return;
                }
            }
            if (line != null)
            {
                var renderer = line.GetComponent<MeshRenderer>();
                float pulse = hot ? 0.45f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.5f : 0.4f;
                Tint(renderer, new Color(1f, 0.38f, 0.5f, pulse), pulse, 0f);
            }
        }

        private void SetHint(string text)
        {
            hintText.text = text;
            hintText.gameObject.SetActive(true);
            var c = hintText.color;
            c.a = 1f;
            hintText.color = c;
            hintLeft = phase == Phase.Play ? 4.5f : 99f;
        }

        private void FitCamera()
        {
            float aspect = cam.aspect;
            if (aspect < 0.05f) aspect = 9f / 16f;
            cam.orthographicSize = Mathf.Max(ViewHalf, 4.5f / aspect);
        }

        private void TickDecor()
        {
            var left = decorRoot.Find("glow-l");
            var right = decorRoot.Find("glow-r");
            if (left != null)
                left.position = new Vector3(-2.3f + Mathf.Sin(Time.time * 0.4f) * 0.18f, 4.2f, 3f);
            if (right != null)
                right.position = new Vector3(2.1f, 8.4f + Mathf.Sin(Time.time * 0.33f) * 0.22f, 3.2f);
        }

        private void TickCamera(float dt)
        {
            trauma = Mathf.Max(0f, trauma - dt * 1.5f);
            float mag = trauma * trauma * 0.22f;
            Vector2 j = mag > 0.001f ? Random.insideUnitCircle * mag : Vector2.zero;
            camRig.position = camBase + new Vector3(j.x, j.y, 0f);
        }

        private void TickHud(float dt)
        {
            scorePunch = Mathf.Lerp(scorePunch, 1f, 1f - Mathf.Exp(-8f * dt));
            scoreText.rectTransform.localScale = Vector3.one * scorePunch;
            if (phase == Phase.Play)
            {
                hintLeft -= dt;
                var c = hintText.color;
                c.a = Mathf.Clamp01(hintLeft);
                hintText.color = c;
                comboText.text = chain > 1 && chainTimer > 0f ? "combo x" + chain : "";
            }
        }

        private void RefreshScore()
        {
            scoreText.text = score.ToString();
            bestText.text = "recorde " + Mathf.Max(best, score);
            if (score > 0 && score >= best)
                scoreText.color = new Color(0.96f, 0.86f, 0.45f);
            else
                scoreText.color = new Color(0.93f, 0.97f, 0.90f);
        }

        private void Burst(Vector2 at, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("mote");
                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float speed = Random.Range(1.5f, 3.4f);
                go.transform.position = new Vector3(at.x, at.y, -0.4f);
                float s = Random.Range(0.12f, 0.28f);
                go.transform.localScale = new Vector3(s, s, 1f);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = quad;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = blobMat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Tint(renderer, color, 1f, 1f);
                motes.Add(new Mote
                {
                    Transform = go.transform,
                    Velocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed,
                    Life = Random.Range(0.25f, 0.45f),
                    Renderer = renderer
                });
            }
        }

        private void TickMotes(float dt)
        {
            for (int i = motes.Count - 1; i >= 0; i--)
            {
                var mote = motes[i];
                mote.Life -= dt;
                if (mote.Transform == null || mote.Life <= 0f)
                {
                    if (mote.Transform != null) Destroy(mote.Transform.gameObject);
                    motes.RemoveAt(i);
                    continue;
                }
                mote.Velocity += Vector2.down * (6f * dt);
                mote.Transform.position += (Vector3)(mote.Velocity * dt);
                float s = Mathf.Max(0.02f, mote.Life * 0.6f);
                mote.Transform.localScale = new Vector3(s, s, 1f);
                motes[i] = mote;
            }
        }
    }
}
