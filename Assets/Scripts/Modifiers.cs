#nullable enable

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public enum ModifierType
{
    Oxygen
}

[Serializable]
public struct ModifierData
{
    public string name;
    public float initialValue;
    public float minValue;
    public float maxValue;
    [NonSerialized]
    public float currentValue;
}

[Serializable]
public class ModifierConfig
{
    public float minValue;
    public float maxValue;
}

[Serializable]
public class ModifiersJsonData
{
    public Dictionary<string, ModifierConfig> modifiers = new();
}

public class Modifiers : MonoBehaviour
{
    private const string MODIFIERS_FOLDER_PATH = "Modifiers";

    [SerializeField]
    private string modifiersConfigName = "modifiers";

    public ModifierData[] modifiers = new ModifierData[Enum.GetValues(typeof(ModifierType)).Length];

    private static Dictionary<string, ModifierType> StringToModifierTypeMap = new()
    {
        { "oxygen", ModifierType.Oxygen }
    };

    private static Dictionary<ModifierType, string> ModifierTypeToStringMap = new()
    {
        { ModifierType.Oxygen, "oxygen" }
    };

    void Start()
    {
        LoadModifiersConfig();
        ResetAll();
    }

    void OnValidate()
    {
        LoadModifiersConfig();
    }

    public void LoadModifiersConfig()
    {
        if (string.IsNullOrEmpty(modifiersConfigName))
        {
            Debug.LogError("Modifiers config name is not set.");
            return;
        }

        string fullPath = Path.Combine(Application.streamingAssetsPath, MODIFIERS_FOLDER_PATH, modifiersConfigName + ".json");

        if (File.Exists(fullPath))
        {
            string jsonContent = File.ReadAllText(fullPath);
            ModifiersJsonData? jsonData = JsonConvert.DeserializeObject<ModifiersJsonData>(jsonContent);

            if (jsonData != null)
            {
                foreach (var kvp in jsonData.modifiers)
                {
                    if (StringToModifierTypeMap.TryGetValue(kvp.Key, out ModifierType type))
                    {
                        int index = (int)type;
                        modifiers[index].name = kvp.Key;
                        modifiers[index].minValue = kvp.Value.minValue;
                        modifiers[index].maxValue = kvp.Value.maxValue;
                        modifiers[index].initialValue = kvp.Value.minValue;
                    }
                    else
                    {
                        Debug.LogWarning($"Unknown modifier type in JSON: {kvp.Key}");
                    }
                }
                Debug.Log($"Loaded modifiers config from: {fullPath}");
            }
        }
        else
        {
            Debug.LogError($"Could not find modifiers config file at: {fullPath}");
        }
    }

    public void ResetModifier(ModifierType type)
    {
        int index = (int)type;
        modifiers[index].currentValue = Mathf.Clamp(modifiers[index].initialValue, modifiers[index].minValue, modifiers[index].maxValue);
    }

    public void ResetAll()
    {
        for (int i = 0; i < modifiers.Length; i++)
        {
            modifiers[i].currentValue = Mathf.Clamp(modifiers[i].initialValue, modifiers[i].minValue, modifiers[i].maxValue);
        }
    }

    public void Set(ModifierType type, float value)
    {
        int index = (int)type;
        modifiers[index].currentValue = Mathf.Clamp(value, modifiers[index].minValue, modifiers[index].maxValue);
    }

    public float Get(ModifierType type)
    {
        return modifiers[(int)type].currentValue;
    }
}
