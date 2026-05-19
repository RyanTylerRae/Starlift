#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager? Instance;
    public Text? TextObject;
    public List<string> subtitleKeys = new List<string>();

    private int currentIndex = 0;

    public void Awake()
    {
        Instance = this;
    }

    public void AdvanceSubtitle()
    {
        if (currentIndex < subtitleKeys.Count)
        {
            DisplaySubtitle(subtitleKeys[currentIndex]);
            currentIndex++;
        }
        else
        {
            ClearSubtitle();
        }
    }

    public void ResetSubtitles()
    {
        currentIndex = 0;
        ClearSubtitle();
    }

    public void DisplaySubtitle(string key)
    {
        if (TextObject != null)
        {
            TextObject.gameObject.SetActive(true);
            TextObject.text = LocalizationManager.Instance?.GetTranslation(key) ?? string.Empty;
        }
    }

    public void ClearSubtitle()
    {
        if (TextObject != null)
        {
            TextObject.gameObject.SetActive(false);
            TextObject.text = "";
        }
    }
}
