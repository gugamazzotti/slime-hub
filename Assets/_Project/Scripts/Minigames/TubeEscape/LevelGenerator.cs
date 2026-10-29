using System.Collections.Generic;
using UnityEngine;
using SlimeHub.Core;
using SlimeHub.Data;
using SlimeHub.Gameplay;
using SlimeHub.Physics;

namespace SlimeHub.Minigames
{
    public static class RunRewards
    {
        public static bool Commit(string board, int score, int biomass, MissionMetric metric, int metricValue)
        {
            bool record = ServiceLocator.Get<ILeaderboardService>().Submit(board, score);
            ServiceLocator.Get<IDataService>().AddBiomass(Mathf.Max(1, biomass));
            ServiceLocator.Get<MissionService>().Submit(metric, metricValue);
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(record ? Sfx.Merge : Sfx.Bloop, record ? 0.8f : 1f);
            return record;
        }
    }

    public sealed class HazardPiece
    {
        public Bounds Bounds;
        public bool Spent;
        public GameObject View;
        public Vector3 Mouth;
        public Vector3 InteriorLocal;
    }

    public sealed class PickupPiece
    {
        public Vector2 Point;
        public float Radius = 0.26f;
        public float Mass = 3.5f;
        public bool Spent;
        public GameObject View;
    }

    public sealed class TubeSegment
    {
        public float Origin;
        public float Length;
        public bool Squeezer;
        public float Squeeze;
        public readonly List<HazardPiece> Hazards = new List<HazardPiece>(4);
        public readonly List<PickupPiece> Pickups = new List<PickupPiece>(3);
        public Transform LeftPlate;
        public Transform RightPlate;
    }

    public sealed class LevelGenerator
    {
        readonly Transform root;
        readonly float halfWidth;
        readonly float sectionLength;
        readonly int sectionsBeforeSqueeze;
        readonly float pickupMass;
        readonly Stack<GameObject> cubes = new Stack<GameObject>();
        readonly Stack<GameObject> tubes = new Stack<GameObject>();
        readonly Stack<GameObject> blobs = new Stack<GameObject>();
        readonly List<TubeSegment> alive = new List<TubeSegment>();
        readonly Material glassMat;
        readonly Material rimMat;
        readonly Material plateMat;
        readonly Material blobMat;
        readonly Mesh blobMesh;
        readonly System.Random random = new System.Random();
        float nextY;
        int index;

        public float SqueezerY { get; private set; }
        public IReadOnlyList<TubeSegment> Alive => alive;

        public LevelGenerator(Transform root, MinigameConfigSO config)
        {
            this.root = root;
            halfWidth = config.tubeHalfWidth;
            sectionLength = config.tubeSectionLength;
            sectionsBeforeSqueeze = config.tubeSections;
            pickupMass = config.tubePickup;
            glassMat = SlimeMaterials.CreateUnlit(new Color(0.62f, 0.9f, 1f, 0.4f), false, 0.95f, 0f, Color.white);
            rimMat = SlimeMaterials.CreateUnlit(new Color(1f, 0.55f, 0.28f, 0.95f), false, 0.25f, 0f, Color.white);
            plateMat = SlimeMaterials.CreateUnlit(new Color(0.95f, 0.62f, 0.22f, 0.92f), false, 0.35f, 0f, new Color(1f, 0.9f, 0.5f, 1f));
            blobMat = SlimeMaterials.CreateJelly();
            SlimeMaterials.ApplyJelly(blobMat, new Color(0.35f, 0.9f, 0.7f, 0.9f), new Color(0.8f, 1f, 0.85f, 1f), 0.2f, 0.3f, 0f, 0f, 0.03f, 0.22f);
            blobMesh = SlimeMeshFactory.SharedBlob(1, 0.22f);
            nextY = 8f;
        }

        public void Tick(float playerY)
        {
            while (nextY < playerY + 26f && index <= sectionsBeforeSqueeze)
                Spawn();

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                TubeSegment segment = alive[i];
                if (segment.Origin + segment.Length >= playerY - 14f)
                    continue;
                Recycle(segment);
                alive.RemoveAt(i);
            }
        }

        void Spawn()
        {
            var segment = new TubeSegment
            {
                Origin = nextY,
                Length = sectionLength,
                Squeezer = index == sectionsBeforeSqueeze
            };

            if (segment.Squeezer)
            {
                SqueezerY = nextY + sectionLength * 0.5f;
                segment.LeftPlate = Plate(-halfWidth - 0.4f, SqueezerY);
                segment.RightPlate = Plate(halfWidth + 0.4f, SqueezerY);
            }
            else
            {
                int pattern = random.Next(5);
                if (pattern == 0)
                {
                    Pickup(segment, -0.7f);
                    Pickup(segment, 0.8f);
                }
                else if (pattern == 1)
                {
                    Block(segment, -halfWidth * 0.48f, halfWidth * 1.15f);
                    Pickup(segment, halfWidth * 0.55f);
                }
                else if (pattern == 2)
                {
                    Block(segment, halfWidth * 0.48f, halfWidth * 1.15f);
                    Pickup(segment, -halfWidth * 0.55f);
                }
                else if (pattern == 3)
                {
                    Block(segment, 0f, 1.9f);
                    Pickup(segment, -halfWidth * 0.62f);
                    Pickup(segment, halfWidth * 0.62f);
                }
                else
                {
                    Block(segment, -halfWidth * 0.62f, 1.35f);
                    Block(segment, halfWidth * 0.62f, 1.35f);
                    Pickup(segment, 0f);
                }
            }

            alive.Add(segment);
            nextY += sectionLength;
            index++;
        }

        void Block(TubeSegment segment, float x, float width)
        {
            int count = width > 1.7f ? 2 : 1;
            float y = segment.Origin + sectionLength * 0.58f;
            for (int i = 0; i < count; i++)
            {
                float ox = x + (i - (count - 1) * 0.5f) * 0.82f;
                AddTube(segment, ox, y);
            }
        }

        void AddTube(TubeSegment segment, float x, float y)
        {
            GameObject view = Tube();
            view.transform.SetParent(root, false);
            view.transform.position = new Vector3(x, y, 0.12f);
            view.transform.rotation = Quaternion.identity;
            view.transform.localScale = Vector3.one;
            Vector3 mouth = view.transform.position;
            segment.Hazards.Add(new HazardPiece
            {
                View = view,
                Mouth = mouth,
                InteriorLocal = new Vector3(0f, 1.22f, -0.04f),
                Bounds = new Bounds(mouth + new Vector3(0f, 0.62f, 0f), new Vector3(0.78f, 1.55f, 0.7f))
            });
        }

        GameObject Tube()
        {
            if (tubes.Count > 0)
            {
                GameObject pooled = tubes.Pop();
                pooled.SetActive(true);
                return pooled;
            }

            var rootTube = new GameObject("TestTube");
            Glass(rootTube.transform, PrimitiveType.Cylinder, "Wall", new Vector3(0f, 1.12f, 0f), new Vector3(0.62f, 1.12f, 0.62f), glassMat);
            Glass(rootTube.transform, PrimitiveType.Sphere, "Cap", new Vector3(0f, 2.18f, 0f), Vector3.one * 0.62f, glassMat);
            Glass(rootTube.transform, PrimitiveType.Cylinder, "Lip", new Vector3(0f, 0.05f, 0f), new Vector3(0.78f, 0.045f, 0.78f), rimMat);
            return rootTube;
        }

        static void Glass(Transform parent, PrimitiveType primitive, string name, Vector3 local, Vector3 scale, Material material)
        {
            var view = GameObject.CreatePrimitive(primitive);
            Collider collider = view.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
            view.name = name;
            view.transform.SetParent(parent, false);
            view.transform.localPosition = local;
            view.transform.localScale = scale;
            var renderer = view.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void Pickup(TubeSegment segment, float x)
        {
            GameObject view = Blob();
            float y = segment.Origin + sectionLength * (0.35f + (float)random.NextDouble() * 0.35f);
            view.transform.SetParent(root, false);
            view.transform.position = new Vector3(x, y, 0f);
            view.transform.localScale = Vector3.one;
            segment.Pickups.Add(new PickupPiece
            {
                View = view,
                Point = new Vector2(x, y),
                Mass = pickupMass
            });
        }

        Transform Plate(float x, float y)
        {
            GameObject view = Cube();
            view.name = "Plate";
            view.transform.SetParent(root, false);
            view.transform.position = new Vector3(x, y, 0f);
            view.transform.localScale = new Vector3(0.35f, 3.2f, 0.4f);
            view.GetComponent<MeshRenderer>().sharedMaterial = plateMat;
            return view.transform;
        }

        GameObject Cube()
        {
            GameObject view = cubes.Count > 0 ? cubes.Pop() : GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider collider = view.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            view.SetActive(true);
            var renderer = view.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return view;
        }

        GameObject Blob()
        {
            if (blobs.Count > 0)
            {
                GameObject pooled = blobs.Pop();
                pooled.SetActive(true);
                return pooled;
            }

            var view = new GameObject("Blob");
            var filter = view.AddComponent<MeshFilter>();
            filter.sharedMesh = blobMesh;
            var renderer = view.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = blobMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return view;
        }

        void Recycle(TubeSegment segment)
        {
            for (int i = 0; i < segment.Hazards.Count; i++)
            {
                ReleaseCaptures(segment.Hazards[i].View);
                Store(tubes, segment.Hazards[i].View);
            }
            for (int i = 0; i < segment.Pickups.Count; i++)
                Store(blobs, segment.Pickups[i].View);
            Store(cubes, segment.LeftPlate != null ? segment.LeftPlate.gameObject : null);
            Store(cubes, segment.RightPlate != null ? segment.RightPlate.gameObject : null);
        }

        static void ReleaseCaptures(GameObject view)
        {
            if (view == null)
                return;
            TrappedGlob[] trapped = view.GetComponentsInChildren<TrappedGlob>(true);
            for (int i = 0; i < trapped.Length; i++)
            {
                trapped[i].transform.SetParent(null, false);
                trapped[i].gameObject.SetActive(false);
            }
        }

        static void Store(Stack<GameObject> stack, GameObject view)
        {
            if (view == null)
                return;
            view.SetActive(false);
            stack.Push(view);
        }
    }
}
