#nullable enable

using UnityEditor;
using UnityEngine;

public class SaveGameEditorWindow : EditorWindow
{
    private SaveGameData data = new();

    [MenuItem("Tools/Starlift/Save Game Editor")]
    private static void Open()
    {
        SaveGameEditorWindow window = GetWindow<SaveGameEditorWindow>("Save Game Editor");
        window.data = SaveGame.Load();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Save file", SaveGame.SaveFilePath, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        data.maxOxygenTankCount = EditorGUILayout.IntField("Max Oxygen Tank Count", data.maxOxygenTankCount);

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload From Disk"))
            {
                data = SaveGame.Load();
            }

            if (GUILayout.Button("Save"))
            {
                SaveGame.Save(data);
            }

            if (GUILayout.Button("Reset To Defaults"))
            {
                data = new SaveGameData();
                SaveGame.Save(data);
            }
        }
    }
}
