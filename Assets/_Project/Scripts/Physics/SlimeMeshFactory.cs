using System.Collections.Generic;
using UnityEngine;

namespace SlimeHub.Physics
{
    public static class SlimeMeshFactory
    {
        static readonly Dictionary<int, Mesh> Shared = new Dictionary<int, Mesh>();

        public static Mesh CreateUnique(int subdivisions, float radius)
        {
            return Build(subdivisions, radius, "SlimeMesh");
        }

        public static Mesh SharedBlob(int subdivisions, float radius)
        {
            int key = subdivisions * 1000 + Mathf.RoundToInt(radius * 100f);
            if (Shared.TryGetValue(key, out Mesh cached) && cached != null)
                return cached;
            Mesh mesh = Build(subdivisions, radius, "SlimeShared");
            Shared[key] = mesh;
            return mesh;
        }

        public static Mesh Quad()
        {
            var mesh = new Mesh { name = "SlimeQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            if (mesh.normals[0].z > 0f)
                mesh.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh Build(int subdivisions, float radius, string name)
        {
            const float X = 0.5257311121f;
            const float Z = 0.8506508084f;
            var verts = new List<Vector3>(162)
            {
                new Vector3(-X, 0f, Z), new Vector3(X, 0f, Z), new Vector3(-X, 0f, -Z), new Vector3(X, 0f, -Z),
                new Vector3(0f, Z, X), new Vector3(0f, Z, -X), new Vector3(0f, -Z, X), new Vector3(0f, -Z, -X),
                new Vector3(Z, X, 0f), new Vector3(-Z, X, 0f), new Vector3(Z, -X, 0f), new Vector3(-Z, -X, 0f)
            };
            var faces = new List<int>(20 * 3)
            {
                0, 4, 1, 0, 9, 4, 9, 5, 4, 4, 5, 8, 4, 8, 1,
                8, 10, 1, 8, 3, 10, 5, 3, 8, 5, 2, 3, 2, 7, 3,
                7, 10, 3, 7, 6, 10, 7, 11, 6, 11, 0, 6, 0, 1, 6,
                6, 1, 10, 9, 0, 11, 9, 11, 2, 9, 2, 5, 7, 2, 11
            };

            subdivisions = Mathf.Clamp(subdivisions, 0, 3);
            var midpoints = new Dictionary<long, int>(320);
            for (int s = 0; s < subdivisions; s++)
            {
                midpoints.Clear();
                var next = new List<int>(faces.Count * 4);
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = faces[i];
                    int b = faces[i + 1];
                    int c = faces[i + 2];
                    int ab = Mid(verts, midpoints, a, b);
                    int bc = Mid(verts, midpoints, b, c);
                    int ca = Mid(verts, midpoints, c, a);
                    next.Add(a); next.Add(ab); next.Add(ca);
                    next.Add(b); next.Add(bc); next.Add(ab);
                    next.Add(c); next.Add(ca); next.Add(bc);
                    next.Add(ab); next.Add(bc); next.Add(ca);
                }
                faces = next;
            }

            var positions = new Vector3[verts.Count];
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 n = verts[i].normalized;
                normals[i] = n;
                positions[i] = n * radius;
            }

            var triangles = faces.ToArray();
            Vector3 edgeA = positions[triangles[1]] - positions[triangles[0]];
            Vector3 edgeB = positions[triangles[2]] - positions[triangles[0]];
            Vector3 face = Vector3.Cross(edgeA, edgeB);
            Vector3 centroid = (positions[triangles[0]] + positions[triangles[1]] + positions[triangles[2]]) / 3f;
            if (Vector3.Dot(face, centroid) < 0f)
            {
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int swap = triangles[i + 1];
                    triangles[i + 1] = triangles[i + 2];
                    triangles[i + 2] = swap;
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.4f);
            mesh.MarkDynamic();
            return mesh;
        }

        static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            int min = a < b ? a : b;
            int max = a < b ? b : a;
            long key = ((long)min << 32) | (uint)max;
            if (cache.TryGetValue(key, out int index))
                return index;
            Vector3 mid = (verts[a] + verts[b]).normalized;
            index = verts.Count;
            verts.Add(mid);
            cache[key] = index;
            return index;
        }
    }
}
