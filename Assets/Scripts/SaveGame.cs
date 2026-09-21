#nullable enable

using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class SaveGameData
{
    public int maxOxygenTankCount = 2;
}

public static class SaveGame
{
    private const string SAVE_FILE_NAME = "savegame.json";

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    public static SaveGameData Load()
    {
        if (File.Exists(SaveFilePath))
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveGameData? data = JsonConvert.DeserializeObject<SaveGameData>(json);
            if (data != null)
            {
                return data;
            }
        }

        return new SaveGameData();
    }

    public static void Save(SaveGameData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SaveFilePath, json);
    }
}
