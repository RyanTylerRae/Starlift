#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class FoliageManager : MonoBehaviour
{
    public List<FoliageEntry> entries = new();
    public bool debugOctree = false;

    private (Mesh mesh, Material material)[] renderData = Array.Empty<(Mesh, Material)>();
    private Matrix4x4[] drawBuffer = new Matrix4x4[1023];
    private Stack<int> traversalStack = new();
    private Camera? mainCamera;

    private int debugDrawCalls = 0;
    private int debugInstanceCount = 0;
    private float debugRenderMs = 0f;
    private readonly Stopwatch renderStopwatch = new();

    public IReadOnlyList<FoliageEntry> Entries { get { return entries; } }

    private void OnEnable()
    {
        mainCamera = null;
        renderData = new (Mesh, Material)[entries.Count];

        for (int i = 0; i < entries.Count; i++)
        {
            var prefab = entries[i].octree?.layer?.prefab;
            if (prefab == null)
            {
                continue;
            }

            var mf = prefab.GetComponentInChildren<MeshFilter>();
            var mr = prefab.GetComponentInChildren<MeshRenderer>();
            if (mf != null && mr != null)
            {
                renderData[i] = (mf.sharedMesh, mr.sharedMaterial);
            }
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<FirstPersonController>()?.playerCamera;
        }

        if (mainCamera == null)
        {
            return;
        }

        debugDrawCalls = 0;
        debugInstanceCount = 0;
        renderStopwatch.Restart();

        var frustum = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        for (int i = 0; i < entries.Count; i++)
        {
            var octree = entries[i].octree;
            if (octree == null || octree.nodes.Length == 0)
            {
                continue;
            }

            var (mesh, material) = renderData[i];
            if (mesh == null || material == null)
            {
                continue;
            }

            DrawOctree(octree, frustum, mesh, material);
        }

        renderStopwatch.Stop();
        debugRenderMs = (float)renderStopwatch.Elapsed.TotalMilliseconds;
    }

    private void OnGUI()
    {
        if (!debugOctree)
        {
            return;
        }

        GUI.Box(new Rect(10, 10, 220, 72), string.Empty);
        GUI.Label(new Rect(18, 16, 204, 20), $"Draw Calls:  {debugDrawCalls}");
        GUI.Label(new Rect(18, 36, 204, 20), $"Instances:   {debugInstanceCount}");
        GUI.Label(new Rect(18, 56, 204, 20), $"Render:      {debugRenderMs:F2} ms");
    }

    private void DrawOctree(FoliageOctree octree, Plane[] frustum, Mesh mesh, Material material)
    {
        traversalStack.Clear();
        traversalStack.Push(0);

        while (traversalStack.Count > 0)
        {
            int idx = traversalStack.Pop();
            OctreeNode node = octree.nodes[idx];

            if (!GeometryUtility.TestPlanesAABB(frustum, node.bounds))
            {
                continue;
            }

            if (node.childStart == -1)
            {
                int remaining = node.matrixCount;
                int offset = node.matrixOffset;
                while (remaining > 0)
                {
                    int batch = Math.Min(remaining, 1023);
                    Array.Copy(octree.matrices, offset, drawBuffer, 0, batch);
                    Graphics.DrawMeshInstanced(mesh, 0, material, drawBuffer, batch);
                    debugDrawCalls++;
                    debugInstanceCount += batch;
                    offset += batch;
                    remaining -= batch;
                }
            }
            else
            {
                for (int c = 0; c < 8; c++)
                {
                    traversalStack.Push(node.childStart + c);
                }
            }
        }
    }
}

[Serializable]
public struct FoliageEntry
{
    public FoliageOctree? octree;
    public FoliagePaintData? paintData;
}
