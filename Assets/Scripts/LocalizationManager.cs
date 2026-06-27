#nullable enable

using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager? Instance { get; private set; }

    public WordBanks? wordBanks;
    public string currentLanguage = "en";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetTranslation(string key)
    {
        if (wordBanks == null)
        {
            Debug.LogError("WordBanks is not assigned in LocalizationManager!");
            return key;
        }

        foreach (var bank in wordBanks.localizationBanks)
        {
            if (bank == null)
            {
                continue;
            }

            var localizedString = bank.GetLocalizedString(key);
            if (localizedString != null)
            {
                return localizedString.GetTranslation(currentLanguage) ?? key;
            }
        }

        Debug.LogWarning($"Translation key '{key}' not found in any localization bank.");
        return key;
    }

    public LocalizedString? GetLocalizedString(string key)
    {
        if (wordBanks == null)
        {
            Debug.LogError("WordBanks is not assigned in LocalizationManager!");
            return null;
        }

        foreach (var bank in wordBanks.localizationBanks)
        {
            if (bank == null) { continue; }

            var localizedString = bank.GetLocalizedString(key);
            if (localizedString != null)
            {
                return localizedString;
            }
        }

        Debug.LogWarning($"Localized string key '{key}' not found in any localization bank.");
        return null;
    }

    public void SetLanguage(string languageCode)
    {
        currentLanguage = languageCode;
    }
}
