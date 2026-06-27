#nullable enable

using System;
using UnityEngine;

[Serializable]
public class LocalizedString
{
    public string? en;
    public string? es;
    public string? fr;
    public string? de;
    public string? ja;
    private string? zhCN = null;
    public string? ko;

    public string? GetTranslation(string languageCode)
    {
        return languageCode switch
        {
            "en" => en,
            "es" => es,
            "fr" => fr,
            "de" => de,
            "ja" => ja,
            "zh-CN" => zhCN,
            "ko" => ko,
            _ => en
        };
    }
}
