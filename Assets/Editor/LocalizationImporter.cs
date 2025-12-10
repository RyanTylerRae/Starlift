using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(2, "localization.json")]
public class LocalizationImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        try
        {
            string jsonContent = File.ReadAllText(ctx.assetPath);
            var dataDict = ParseLocalizationJson(jsonContent);

            var localizationData = ScriptableObject.CreateInstance<LocalizationData>();
            localizationData.SetEntries(dataDict);

            ctx.AddObjectToAsset("main", localizationData);
            ctx.SetMainObject(localizationData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to import localization file {ctx.assetPath}: {e.Message}\n{e.StackTrace}");
        }
    }

    private Dictionary<string, LocalizedString> ParseLocalizationJson(string json)
    {
        var result = new Dictionary<string, LocalizedString>();

        json = json.Trim();
        if (json.StartsWith("{")) json = json.Substring(1);
        if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

        var keyPattern = @"""([^""]+)""\s*:\s*\{([^}]+)\}";
        var matches = Regex.Matches(json, keyPattern);

        foreach (Match match in matches)
        {
            string key = match.Groups[1].Value;
            string valuesJson = "{" + match.Groups[2].Value + "}";

            try
            {
                LocalizedString localizedString = JsonUtility.FromJson<LocalizedString>(valuesJson);
                result[key] = localizedString;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse localization key '{key}': {e.Message}");
            }
        }

        return result;
    }
}
