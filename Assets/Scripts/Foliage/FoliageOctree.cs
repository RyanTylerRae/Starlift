#nullable enable

using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Starlift/Foliage Octree")]
public class FoliageOctree : ScriptableObject
{
    public FoliageLayer? layer;
    public float cellSize = 0f;

    public OctreeNode[] nodes = Array.Empty<OctreeNode>();
    public Matrix4x4[] matrices = Array.Empty<Matrix4x4>();

    public void SetBakedData(OctreeNode[] bakedNodes, Matrix4x4[] bakedMatrices)
    {
        nodes = bakedNodes;
        matrices = bakedMatrices;
    }
}

[Serializable]
public struct OctreeNode
{
    public Bounds bounds;
    public int childStart;    // -1 if leaf
    public int matrixOffset;  // valid when leaf
    public int matrixCount;   // valid when leaf
}
