#nullable enable

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class PlayerSettings
{
    private const string GodModeOxygenDisabledKey = "Starlift.PlayerSettings.GodModeOxygenDisabled";

    private static bool godModeOxygenDisabled;

    public static bool GodModeOxygenDisabled
    {
        get => godModeOxygenDisabled;
        set
        {
            godModeOxygenDisabled = value;
#if UNITY_EDITOR
            EditorPrefs.SetBool(GodModeOxygenDisabledKey, value);
#endif
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void LoadFromEditorPrefs()
    {
        godModeOxygenDisabled = EditorPrefs.GetBool(GodModeOxygenDisabledKey, false);
    }
#endif
}
