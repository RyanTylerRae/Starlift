#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FoliageManagerGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmos(FoliageManager manager, GizmoType gizmoType)
    {
        if (manager.debugOctree)
        {
            foreach (var entry in manager.Entries)
            {
                var octree = entry.octree;
                if (octree == null || octree.nodes.Length == 0)
                {
                    continue;
                }

                foreach (var node in octree.nodes)
                {
                    if (node.childStart != -1)
                    {
                        continue;
                    }

                    Gizmos.color = new Color(0.8f, 0.6f, 1f, 0.5f);
                    Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
                }
            }
        }

    }
}

[CustomEditor(typeof(FoliageManager))]
public class FoliageManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var manager = (FoliageManager)target;

        if (GUILayout.Button("Bake All"))
        {
            BakeAll(manager);
        }

        EditorGUILayout.Space();

        foreach (var entry in manager.Entries)
        {
            if (entry.octree == null)
            {
                continue;
            }

            if (GUILayout.Button($"Bake: {entry.octree.name}"))
            {
                BakeEntry(entry);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void BakeAll(FoliageManager manager)
    {
        foreach (var entry in manager.Entries)
        {
            if (entry.octree == null || entry.paintData == null || entry.octree.layer == null)
            {
                continue;
            }

            BakeEntry(entry);
        }

        AssetDatabase.SaveAssets();
    }

    private static void BakeEntry(FoliageEntry entry)
    {
        if (entry.octree == null || entry.paintData == null || entry.octree.layer == null)
        {
            return;
        }

        var candidates = FoliageDistributor.SampleCandidates(entry.paintData);
        var thinned = FoliageDistributor.PoissonThin(candidates, entry.octree.layer, 0);

        var rng = new System.Random(0);
        var layer = entry.octree.layer;
        var matrices = new List<Matrix4x4>(thinned.Count);

        foreach (var c in thinned)
        {
            Quaternion alignToNormal = Quaternion.FromToRotation(Vector3.up, c.normal);
            float yawAngle = (float)(rng.NextDouble() * 360.0);
            Quaternion yaw = Quaternion.AngleAxis(yawAngle, c.normal);
            float scale = layer.minScale + (float)(rng.NextDouble() * (layer.maxScale - layer.minScale));
            matrices.Add(Matrix4x4.TRS(c.position, yaw * alignToNormal, Vector3.one * scale));
        }

        float cellSize = entry.octree.cellSize > 0f ? entry.octree.cellSize : layer.SuggestedCellSize;
        OctreeBaker.Bake(entry.octree, matrices, cellSize);
        EditorUtility.SetDirty(entry.octree);
    }

    private static class OctreeBaker
    {
        private const int MaxDepth = 8;

        private class TempNode
        {
            public Bounds bounds;
            public List<int> indices = new();
            public TempNode[]? children;
        }

        public static void Bake(FoliageOctree target, List<Matrix4x4> matrices, float cellSize)
        {
            if (matrices.Count == 0)
            {
                target.SetBakedData(Array.Empty<OctreeNode>(), Array.Empty<Matrix4x4>());
                return;
            }

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var positions = new Vector3[matrices.Count];

            for (int i = 0; i < matrices.Count; i++)
            {
                positions[i] = matrices[i].GetColumn(3);
                min = Vector3.Min(min, positions[i]);
                max = Vector3.Max(max, positions[i]);
            }

            var worldBounds = new Bounds();
            worldBounds.SetMinMax(min, max);
            worldBounds.Expand(0.01f);

            var allIndices = new List<int>(matrices.Count);
            for (int i = 0; i < matrices.Count; i++)
            {
                allIndices.Add(i);
            }

            var root = Build(positions, allIndices, worldBounds, cellSize, 0);

            // BFS flatten to ensure children of a node always form a contiguous block
            var bfsOrder = new List<TempNode>();
            var indexMap = new Dictionary<TempNode, int>();
            var queue = new Queue<TempNode>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                indexMap[n] = bfsOrder.Count;
                bfsOrder.Add(n);

                if (n.children != null)
                {
                    foreach (var c in n.children)
                    {
                        queue.Enqueue(c);
                    }
                }
            }

            var nodeList = new OctreeNode[bfsOrder.Count];
            var matrixList = new List<Matrix4x4>();

            for (int i = 0; i < bfsOrder.Count; i++)
            {
                var temp = bfsOrder[i];

                if (temp.children == null)
                {
                    int offset = matrixList.Count;
                    foreach (int idx in temp.indices)
                    {
                        matrixList.Add(matrices[idx]);
                    }

                    nodeList[i] = new OctreeNode
                    {
                        bounds = temp.bounds,
                        childStart = -1,
                        matrixOffset = offset,
                        matrixCount = temp.indices.Count
                    };
                }
                else
                {
                    nodeList[i] = new OctreeNode
                    {
                        bounds = temp.bounds,
                        childStart = indexMap[temp.children[0]],
                        matrixOffset = 0,
                        matrixCount = 0
                    };
                }
            }

            target.SetBakedData(nodeList, matrixList.ToArray());
        }

        private static TempNode Build(Vector3[] positions, List<int> indices, Bounds bounds, float cellSize, int depth)
        {
            var node = new TempNode { bounds = bounds, indices = indices };

            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (indices.Count == 0 || maxDim <= cellSize || depth >= MaxDepth)
            {
                return node;
            }

            Vector3 center = bounds.center;
            var childIndices = new List<int>[8];
            for (int c = 0; c < 8; c++)
            {
                childIndices[c] = new List<int>();
            }

            foreach (int i in indices)
            {
                Vector3 p = positions[i];
                int oct = (p.x >= center.x ? 1 : 0) |
                          (p.y >= center.y ? 2 : 0) |
                          (p.z >= center.z ? 4 : 0);
                childIndices[oct].Add(i);
            }

            node.children = new TempNode[8];
            Vector3 childExtents = bounds.extents * 0.5f;

            for (int c = 0; c < 8; c++)
            {
                Vector3 childCenter = center + new Vector3(
                    (c & 1) != 0 ? childExtents.x : -childExtents.x,
                    (c & 2) != 0 ? childExtents.y : -childExtents.y,
                    (c & 4) != 0 ? childExtents.z : -childExtents.z
                );

                node.children[c] = Build(positions, childIndices[c], new Bounds(childCenter, bounds.extents), cellSize, depth + 1);
            }

            return node;
        }
    }
}
