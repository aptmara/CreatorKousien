using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.DefenceLine
{
    internal static class DefenseLineFractureMeshBuilder
    {
        internal sealed class Cell
        {
            public readonly Vector2 Site;
            public readonly List<Vector2> Vertices;
            public readonly Vector2 Centroid;
            public readonly float Area;

            public Cell(Vector2 site, List<Vector2> vertices)
            {
                Site = site;
                Vertices = vertices;
                Area = CalculateArea(vertices);
                Centroid = CalculateCentroid(vertices);
            }
        }

        internal readonly struct FragmentMesh
        {
            public readonly Mesh Mesh;
            public readonly Vector3 WorldCenter;
            public readonly Vector3[] FrontLoop;
            public readonly float WorldArea;

            public FragmentMesh(Mesh mesh, Vector3 worldCenter, Vector3[] frontLoop, float worldArea)
            {
                Mesh = mesh;
                WorldCenter = worldCenter;
                FrontLoop = frontLoop;
                WorldArea = worldArea;
            }
        }

        internal static List<Cell> BuildVoronoi(Rect area, int fragmentCount, int randomSeed)
        {
            int validCount = Mathf.Max(1, fragmentCount);
            System.Random random = new System.Random(randomSeed);
            List<Vector2> sites = new List<Vector2>(validCount);
            for (int i = 0; i < validCount; i++)
            {
                sites.Add(new Vector2(
                    Mathf.Lerp(area.xMin, area.xMax, (float)random.NextDouble()),
                    Mathf.Lerp(area.yMin, area.yMax, (float)random.NextDouble())));
            }

            List<Cell> cells = new List<Cell>(validCount);
            for (int siteIndex = 0; siteIndex < sites.Count; siteIndex++)
            {
                List<Vector2> polygon = CreateRectangle(area);
                Vector2 site = sites[siteIndex];

                for (int otherIndex = 0; otherIndex < sites.Count && polygon.Count >= 3; otherIndex++)
                {
                    if (otherIndex == siteIndex) continue;

                    Vector2 other = sites[otherIndex];
                    Vector2 midpoint = (site + other) * 0.5f;
                    Vector2 normal = other - site;
                    polygon = ClipToHalfPlane(polygon, midpoint, normal);
                }

                if (polygon.Count >= 3 && CalculateArea(polygon) > 0.000001f)
                {
                    cells.Add(new Cell(site, polygon));
                }
            }

            return cells;
        }

        internal static FragmentMesh BuildFragment(
            Cell cell,
            Transform sourceTransform,
            float surfaceLocalZ,
            float worldThickness,
            Rect uvArea)
        {
            Vector3 localCenter = new Vector3(cell.Centroid.x, cell.Centroid.y, surfaceLocalZ);
            Vector3 worldCenter = sourceTransform.TransformPoint(localCenter);
            Vector3 worldNormal = sourceTransform.TransformDirection(Vector3.forward).normalized;
            float halfThickness = Mathf.Max(0.001f, worldThickness) * 0.5f;
            int count = cell.Vertices.Count;

            List<Vector3> vertices = new List<Vector3>(count * 6);
            List<Vector3> normals = new List<Vector3>(count * 6);
            List<Vector2> uvs = new List<Vector2>(count * 6);
            List<int> triangles = new List<int>((count - 2) * 6 + count * 6);
            Vector3[] frontLoop = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 point = cell.Vertices[i];
                Vector3 surfacePoint = sourceTransform.TransformPoint(new Vector3(point.x, point.y, surfaceLocalZ));
                Vector3 front = surfacePoint + worldNormal * halfThickness - worldCenter;
                frontLoop[i] = front + worldNormal * 0.001f;
                vertices.Add(front);
                normals.Add(worldNormal);
                uvs.Add(ToUv(point, uvArea));
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 point = cell.Vertices[i];
                Vector3 surfacePoint = sourceTransform.TransformPoint(new Vector3(point.x, point.y, surfaceLocalZ));
                vertices.Add(surfacePoint - worldNormal * halfThickness - worldCenter);
                normals.Add(-worldNormal);
                uvs.Add(ToUv(point, uvArea));
            }

            for (int i = 1; i < count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);

                triangles.Add(count);
                triangles.Add(count + i + 1);
                triangles.Add(count + i);
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Vector3 frontA = vertices[i];
                Vector3 frontB = vertices[next];
                Vector3 backA = vertices[count + i];
                Vector3 backB = vertices[count + next];
                Vector3 sideNormal = Vector3.Cross(backA - frontA, frontB - frontA).normalized;
                int start = vertices.Count;

                vertices.Add(frontA);
                vertices.Add(backA);
                vertices.Add(backB);
                vertices.Add(frontB);
                for (int vertexIndex = 0; vertexIndex < 4; vertexIndex++)
                {
                    normals.Add(sideNormal);
                }
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(1.0f, 1.0f));

                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            Mesh mesh = new Mesh
            {
                name = "DefenseLineFragment"
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();

            float scaleX = sourceTransform.TransformVector(Vector3.right).magnitude;
            float scaleY = sourceTransform.TransformVector(Vector3.up).magnitude;
            return new FragmentMesh(mesh, worldCenter, frontLoop, cell.Area * scaleX * scaleY);
        }

        private static List<Vector2> CreateRectangle(Rect area)
        {
            return new List<Vector2>
            {
                new Vector2(area.xMin, area.yMin),
                new Vector2(area.xMax, area.yMin),
                new Vector2(area.xMax, area.yMax),
                new Vector2(area.xMin, area.yMax)
            };
        }

        private static List<Vector2> ClipToHalfPlane(
            List<Vector2> polygon,
            Vector2 pointOnLine,
            Vector2 planeNormal)
        {
            List<Vector2> result = new List<Vector2>(polygon.Count + 1);
            Vector2 previous = polygon[polygon.Count - 1];
            float previousDistance = Vector2.Dot(previous - pointOnLine, planeNormal);
            bool previousInside = previousDistance <= 0.000001f;

            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i];
                float currentDistance = Vector2.Dot(current - pointOnLine, planeNormal);
                bool currentInside = currentDistance <= 0.000001f;

                if (currentInside != previousInside)
                {
                    float denominator = previousDistance - currentDistance;
                    float t = Mathf.Abs(denominator) > 0.000001f
                        ? previousDistance / denominator
                        : 0.0f;
                    result.Add(Vector2.Lerp(previous, current, Mathf.Clamp01(t)));
                }

                if (currentInside)
                {
                    result.Add(current);
                }

                previous = current;
                previousDistance = currentDistance;
                previousInside = currentInside;
            }

            return result;
        }

        private static float CalculateArea(IReadOnlyList<Vector2> vertices)
        {
            float doubleArea = 0.0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 current = vertices[i];
                Vector2 next = vertices[(i + 1) % vertices.Count];
                doubleArea += current.x * next.y - next.x * current.y;
            }

            return Mathf.Abs(doubleArea) * 0.5f;
        }

        private static Vector2 CalculateCentroid(IReadOnlyList<Vector2> vertices)
        {
            float doubleArea = 0.0f;
            Vector2 weightedCenter = Vector2.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 current = vertices[i];
                Vector2 next = vertices[(i + 1) % vertices.Count];
                float cross = current.x * next.y - next.x * current.y;
                doubleArea += cross;
                weightedCenter += (current + next) * cross;
            }

            if (Mathf.Abs(doubleArea) < 0.000001f)
            {
                Vector2 average = Vector2.zero;
                for (int i = 0; i < vertices.Count; i++) average += vertices[i];
                return average / Mathf.Max(1, vertices.Count);
            }

            return weightedCenter / (3.0f * doubleArea);
        }

        private static Vector2 ToUv(Vector2 point, Rect area)
        {
            return new Vector2(
                Mathf.InverseLerp(area.xMin, area.xMax, point.x),
                Mathf.InverseLerp(area.yMin, area.yMax, point.y));
        }
    }
}
