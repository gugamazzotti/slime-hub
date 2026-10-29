using System.Collections.Generic;
using UnityEngine;

namespace SlimeHub.Physics
{
    /// <summary>
    /// Superfície de líquido: um vértice por célula que cruza o campo
    /// e um quad em cada aresta que muda de sinal. O gradiente aponta para fora.
    /// </summary>
    public static class SlimeMetaball
    {
        public static void Build(float[] field, int res, Vector3 origin, float cell, float iso, Mesh mesh, List<Vector3> verts, List<Vector3> normals, List<int> tris, int[] cellVertex)
        {
            if (field == null || mesh == null || res < 3)
                return;

            int cells = res - 1;
            int cellCount = cells * cells * cells;
            if (cellVertex == null || cellVertex.Length < cellCount)
                return;

            for (int i = 0; i < cellCount; i++)
                cellVertex[i] = -1;

            verts.Clear();
            normals.Clear();
            tris.Clear();

            for (int z = 0; z < cells; z++)
            {
                for (int y = 0; y < cells; y++)
                {
                    for (int x = 0; x < cells; x++)
                    {
                        if (!Crosses(field, res, x, y, z, iso))
                            continue;
                        Vector3 average = EdgeAverage(field, res, origin, cell, iso, x, y, z);
                        int id = verts.Count;
                        cellVertex[(z * cells + y) * cells + x] = id;
                        verts.Add(average);
                        normals.Add(Vector3.up);
                    }
                }
            }

            if (verts.Count < 4)
                return;

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 projected = Project(field, res, origin, cell, iso, verts[i]);
                verts[i] = projected;
                Vector3 gradient = Gradient(field, res, origin, cell, projected);
                normals[i] = gradient.sqrMagnitude > 1e-8f ? -gradient.normalized : Vector3.up;
            }

            for (int z = 0; z < res; z++)
            {
                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res - 1; x++)
                        Emit(field, res, cells, origin, cell, cellVertex, verts, tris, iso, x, y, z, 0);
                }
            }

            for (int z = 0; z < res; z++)
            {
                for (int y = 0; y < res - 1; y++)
                {
                    for (int x = 0; x < res; x++)
                        Emit(field, res, cells, origin, cell, cellVertex, verts, tris, iso, x, y, z, 1);
                }
            }

            for (int z = 0; z < res - 1; z++)
            {
                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                        Emit(field, res, cells, origin, cell, cellVertex, verts, tris, iso, x, y, z, 2);
                }
            }

            if (tris.Count < 3)
                return;

            Relax(verts, normals, tris);

            float facing = 0f;
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                Vector3 a = verts[tris[t]];
                Vector3 b = verts[tris[t + 1]];
                Vector3 c = verts[tris[t + 2]];
                facing += Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c);
            }

            if (facing < 0f)
            {
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    int swap = tris[t + 1];
                    tris[t + 1] = tris[t + 2];
                    tris[t + 2] = swap;
                }

                for (int i = 0; i < normals.Count; i++)
                    normals[i] = -normals[i];
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * cell * res * 1.15f);
            mesh.MarkDynamic();
        }

        static Vector3[] relaxSum;
        static int[] relaxCount;

        static void Relax(List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            int n = verts.Count;
            if (n == 0)
                return;
            if (relaxSum == null || relaxSum.Length < n)
            {
                relaxSum = new Vector3[Mathf.NextPowerOfTwo(n)];
                relaxCount = new int[relaxSum.Length];
            }

            for (int pass = 0; pass < 2; pass++)
            {
                System.Array.Clear(relaxSum, 0, n);
                System.Array.Clear(relaxCount, 0, n);
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    int a = tris[t];
                    int b = tris[t + 1];
                    int c = tris[t + 2];
                    if ((uint)a >= (uint)n || (uint)b >= (uint)n || (uint)c >= (uint)n)
                        continue;
                    relaxSum[a] += verts[b] + verts[c];
                    relaxSum[b] += verts[a] + verts[c];
                    relaxSum[c] += verts[a] + verts[b];
                    relaxCount[a] += 2;
                    relaxCount[b] += 2;
                    relaxCount[c] += 2;
                }

                for (int i = 0; i < n; i++)
                {
                    if (relaxCount[i] == 0)
                        continue;
                    Vector3 average = relaxSum[i] / relaxCount[i];
                    verts[i] = Vector3.Lerp(verts[i], average, 0.34f);
                }
            }

            System.Array.Clear(relaxSum, 0, n);
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                int a = tris[t];
                int b = tris[t + 1];
                int c = tris[t + 2];
                if ((uint)a >= (uint)n || (uint)b >= (uint)n || (uint)c >= (uint)n)
                    continue;
                Vector3 face = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                relaxSum[a] += face;
                relaxSum[b] += face;
                relaxSum[c] += face;
            }

            for (int i = 0; i < n; i++)
            {
                Vector3 blended = relaxSum[i].sqrMagnitude > 1e-8f ? relaxSum[i].normalized : normals[i];
                normals[i] = blended;
            }
        }

        static bool Crosses(float[] field, int res, int x, int y, int z, float iso)
        {
            bool inside = At(field, res, x, y, z) > iso;
            for (int i = 0; i < 8; i++)
            {
                int cx = x + ((i & 1) == 0 ? 0 : 1);
                int cy = y + ((i & 2) == 0 ? 0 : 1);
                int cz = z + ((i & 4) == 0 ? 0 : 1);
                if ((At(field, res, cx, cy, cz) > iso) != inside)
                    return true;
            }

            return false;
        }

        static Vector3 EdgeAverage(float[] field, int res, Vector3 origin, float cell, float iso, int x, int y, int z)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            Accumulate(field, res, origin, cell, iso, x, y, z, x + 1, y, z, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y, z, x, y + 1, z, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y, z, x, y, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x + 1, y, z, x + 1, y + 1, z, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x + 1, y, z, x + 1, y, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y + 1, z, x + 1, y + 1, z, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y + 1, z, x, y + 1, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y, z + 1, x + 1, y, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y, z + 1, x, y + 1, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x + 1, y, z + 1, x + 1, y + 1, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x, y + 1, z + 1, x + 1, y + 1, z + 1, ref sum, ref count);
            Accumulate(field, res, origin, cell, iso, x + 1, y + 1, z, x + 1, y + 1, z + 1, ref sum, ref count);
            if (count == 0)
                return origin + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * cell;
            return sum / count;
        }

        static void Accumulate(float[] field, int res, Vector3 origin, float cell, float iso, int x0, int y0, int z0, int x1, int y1, int z1, ref Vector3 sum, ref int count)
        {
            float a = At(field, res, x0, y0, z0);
            float b = At(field, res, x1, y1, z1);
            if ((a > iso) == (b > iso))
                return;
            float denom = a - b;
            float t = Mathf.Abs(denom) < 1e-6f ? 0.5f : Mathf.Clamp01((a - iso) / denom);
            Vector3 p = origin + new Vector3(
                Mathf.Lerp(x0, x1, t),
                Mathf.Lerp(y0, y1, t),
                Mathf.Lerp(z0, z1, t)) * cell;
            sum += p;
            count++;
        }

        static void Emit(float[] field, int res, int cells, Vector3 origin, float cell, int[] cellVertex, List<Vector3> verts, List<int> tris, float iso, int x, int y, int z, int axis)
        {
            int x1 = x;
            int y1 = y;
            int z1 = z;
            if (axis == 0)
                x1++;
            else if (axis == 1)
                y1++;
            else
                z1++;

            float a = At(field, res, x, y, z);
            float b = At(field, res, x1, y1, z1);
            if ((a > iso) == (b > iso))
                return;

            int c0;
            int c1;
            int c2;
            int c3;
            if (axis == 0)
            {
                if (!TryCell(cellVertex, cells, x, y, z, out c0)) return;
                if (!TryCell(cellVertex, cells, x, y - 1, z, out c1)) return;
                if (!TryCell(cellVertex, cells, x, y - 1, z - 1, out c2)) return;
                if (!TryCell(cellVertex, cells, x, y, z - 1, out c3)) return;
            }
            else if (axis == 1)
            {
                if (!TryCell(cellVertex, cells, x, y, z, out c0)) return;
                if (!TryCell(cellVertex, cells, x - 1, y, z, out c1)) return;
                if (!TryCell(cellVertex, cells, x - 1, y, z - 1, out c2)) return;
                if (!TryCell(cellVertex, cells, x, y, z - 1, out c3)) return;
            }
            else
            {
                if (!TryCell(cellVertex, cells, x, y, z, out c0)) return;
                if (!TryCell(cellVertex, cells, x - 1, y, z, out c1)) return;
                if (!TryCell(cellVertex, cells, x - 1, y - 1, z, out c2)) return;
                if (!TryCell(cellVertex, cells, x, y - 1, z, out c3)) return;
            }

            Vector3 v0 = verts[c0];
            Vector3 v1 = verts[c1];
            Vector3 v2 = verts[c2];
            Vector3 face = Vector3.Cross(v1 - v0, v2 - v0);
            if (face.sqrMagnitude < 1e-10f)
                return;

            Vector3 mid = (v0 + v1 + verts[c2] + verts[c3]) * 0.25f;
            Vector3 outward = -Gradient(field, res, origin, cell, mid);
            if (Vector3.Dot(face, outward) < 0f)
            {
                int swap = c1;
                c1 = c3;
                c3 = swap;
            }

            tris.Add(c0);
            tris.Add(c1);
            tris.Add(c2);
            tris.Add(c0);
            tris.Add(c2);
            tris.Add(c3);
        }

        static bool TryCell(int[] cellVertex, int cells, int x, int y, int z, out int id)
        {
            id = -1;
            if ((uint)x >= (uint)cells || (uint)y >= (uint)cells || (uint)z >= (uint)cells)
                return false;
            id = cellVertex[(z * cells + y) * cells + x];
            return id >= 0;
        }

        static Vector3 Project(float[] field, int res, Vector3 origin, float cell, float iso, Vector3 point)
        {
            Vector3 gradient = Gradient(field, res, origin, cell, point);
            float mag2 = gradient.sqrMagnitude;
            if (mag2 < 1e-8f)
                return point;
            float value = Sample(field, res, origin, cell, point);
            Vector3 shift = gradient * ((value - iso) / mag2);
            float limit = cell * cell;
            if (shift.sqrMagnitude > limit)
                shift = shift.normalized * cell;
            return point - shift;
        }

        static Vector3 Gradient(float[] field, int res, Vector3 origin, float cell, Vector3 point)
        {
            float step = cell * 0.45f;
            float dx = Sample(field, res, origin, cell, point + new Vector3(step, 0f, 0f)) - Sample(field, res, origin, cell, point - new Vector3(step, 0f, 0f));
            float dy = Sample(field, res, origin, cell, point + new Vector3(0f, step, 0f)) - Sample(field, res, origin, cell, point - new Vector3(0f, step, 0f));
            float dz = Sample(field, res, origin, cell, point + new Vector3(0f, 0f, step)) - Sample(field, res, origin, cell, point - new Vector3(0f, 0f, step));
            return new Vector3(dx, dy, dz) / (step * 2f);
        }

        static float Sample(float[] field, int res, Vector3 origin, float cell, Vector3 point)
        {
            Vector3 q = (point - origin) / cell;
            int x0 = Mathf.FloorToInt(q.x);
            int y0 = Mathf.FloorToInt(q.y);
            int z0 = Mathf.FloorToInt(q.z);
            float tx = q.x - x0;
            float ty = q.y - y0;
            float tz = q.z - z0;
            float c000 = At(field, res, x0, y0, z0);
            float c100 = At(field, res, x0 + 1, y0, z0);
            float c010 = At(field, res, x0, y0 + 1, z0);
            float c110 = At(field, res, x0 + 1, y0 + 1, z0);
            float c001 = At(field, res, x0, y0, z0 + 1);
            float c101 = At(field, res, x0 + 1, y0, z0 + 1);
            float c011 = At(field, res, x0, y0 + 1, z0 + 1);
            float c111 = At(field, res, x0 + 1, y0 + 1, z0 + 1);
            float c00 = Mathf.Lerp(c000, c100, tx);
            float c10 = Mathf.Lerp(c010, c110, tx);
            float c01 = Mathf.Lerp(c001, c101, tx);
            float c11 = Mathf.Lerp(c011, c111, tx);
            return Mathf.Lerp(Mathf.Lerp(c00, c10, ty), Mathf.Lerp(c01, c11, ty), tz);
        }

        static float At(float[] field, int res, int x, int y, int z)
        {
            if ((uint)x >= (uint)res || (uint)y >= (uint)res || (uint)z >= (uint)res)
                return 0f;
            return field[x + res * (y + res * z)];
        }
    }
}
