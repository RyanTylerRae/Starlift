#nullable enable

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoliageLayer))]
public class FoliageLayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var layer = (FoliageLayer)target;

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.FloatField("Suggested Cell Size", layer.SuggestedCellSize);
        EditorGUI.EndDisabledGroup();
    }
}
