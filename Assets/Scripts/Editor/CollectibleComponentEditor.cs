#nullable enable

using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollectibleComponent))]
[CanEditMultipleObjects]
public class CollectibleComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, nameof(CollectibleComponent.collectibleId));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CollectibleComponent.collectibleId)));
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Generate ID"))
        {
            foreach (UnityEngine.Object obj in targets)
            {
                var collectible = (CollectibleComponent)obj;
                Undo.RecordObject(collectible, "Generate Collectible ID");
                collectible.collectibleId = GenerateId();
                PrefabUtility.RecordPrefabInstancePropertyModifications(collectible);
                EditorUtility.SetDirty(collectible);
            }
        }

        DrawIdProblems();
    }

    private void DrawIdProblems()
    {
        CollectibleComponent[] sceneCollectibles = FindObjectsByType<CollectibleComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (UnityEngine.Object obj in targets)
        {
            var collectible = (CollectibleComponent)obj;
            string prefix = targets.Length > 1 ? $"{collectible.name}: " : "";

            if (EditorUtility.IsPersistent(collectible))
            {
                EditorGUILayout.HelpBox($"{prefix}This is a prefab asset. Generate IDs on the scene instances instead, or every instance will share one ID.", MessageType.Warning);
                continue;
            }

            if (string.IsNullOrEmpty(collectible.collectibleId))
            {
                EditorGUILayout.HelpBox($"{prefix}No ID, so this pickup won't be saved. Press Generate ID.", MessageType.Error);
                continue;
            }

            foreach (CollectibleComponent other in sceneCollectibles)
            {
                if (other != collectible && other.collectibleId == collectible.collectibleId)
                {
                    EditorGUILayout.HelpBox($"{prefix}Same ID as '{other.name}'. Press Generate ID on one of them.", MessageType.Error);
                    break;
                }
            }
        }
    }

    private static string GenerateId()
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Guid.NewGuid().ToByteArray());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
