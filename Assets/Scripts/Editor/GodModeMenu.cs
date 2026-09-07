#nullable enable

using UnityEditor;

public static class GodModeMenu
{
    private const string MenuPath = "Tools/Starlift/Toggle God Mode";

    [MenuItem(MenuPath)]
    private static void ToggleGodMode()
    {
        global::PlayerSettings.GodModeOxygenDisabled = !global::PlayerSettings.GodModeOxygenDisabled;
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleGodModeValidate()
    {
        Menu.SetChecked(MenuPath, global::PlayerSettings.GodModeOxygenDisabled);
        return true;
    }
}
