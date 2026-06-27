#nullable enable

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoliageOctree))]
public class FoliageOctreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var octree = (FoliageOctree)target;

        if (octree.layer != null)
        {
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(true);
            float effective = octree.cellSize > 0f ? octree.cellSize : octree.layer.SuggestedCellSize;
            EditorGUILayout.FloatField("Effective Cell Size", effective);
            EditorGUI.EndDisabledGroup();
        }
    }
}
