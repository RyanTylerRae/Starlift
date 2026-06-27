#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationEntry
{
    public string? key;
    public LocalizedString? value;
}

public class LocalizationData : ScriptableObject
{
    public List<LocalizationEntry> entries = new List<LocalizationEntry>();

    private Dictionary<string, LocalizedString>? _dictionary;

    public Dictionary<string, LocalizedString> GetDictionary()
    {
        if (_dictionary == null || _dictionary.Count != entries.Count)
        {
            _dictionary = new Dictionary<string, LocalizedString>();
            foreach (var entry in entries)
            {
                if (entry.key != null && entry.value != null)
                {
                    _dictionary[entry.key] = entry.value;
                }
            }
        }
        return _dictionary;
    }

    public LocalizedString? GetLocalizedString(string key)
    {
        return GetDictionary().TryGetValue(key, out var value) ? value : null;
    }

    public string GetTranslation(string key, string languageCode)
    {
        var localizedString = GetLocalizedString(key);
        return localizedString?.GetTranslation(languageCode) ?? key;
    }

    public void SetEntries(Dictionary<string, LocalizedString> dictionary)
    {
        entries.Clear();
        foreach (var kvp in dictionary)
        {
            entries.Add(new LocalizationEntry { key = kvp.Key, value = kvp.Value });
        }
        _dictionary = null;
    }
}
