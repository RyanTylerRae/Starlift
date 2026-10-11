#nullable enable

using TMPro;
using UnityEngine;

public class TextFader : MonoBehaviour
{
    public float fadeInDuration = 1.0f;
    public float fadeOutDuration = 0.5f;
    public bool startHidden = true;

    private TMP_Text? text = null;
    private float visibleAlpha = 1.0f;
    private float targetAlpha = 0.0f;

    public bool IsShown { get { return targetAlpha > 0.0f; } }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.LogError($"TextFader: '{name}' has no TextMeshPro component!");
            return;
        }

        visibleAlpha = text.alpha;
        targetAlpha = startHidden ? 0.0f : visibleAlpha;
        SetAlpha(targetAlpha);
    }

    private void Update()
    {
        if (text == null || text.alpha == targetAlpha)
        {
            return;
        }

        float duration = targetAlpha > text.alpha ? fadeInDuration : fadeOutDuration;
        float step = duration > 0.0f ? visibleAlpha / duration * Time.deltaTime : visibleAlpha;
        SetAlpha(Mathf.MoveTowards(text.alpha, targetAlpha, step));
    }

    public void Show()
    {
        targetAlpha = visibleAlpha;
    }

    public void Hide()
    {
        targetAlpha = 0.0f;
    }

    private void SetAlpha(float alpha)
    {
        if (text == null)
        {
            return;
        }

        text.alpha = alpha;
        text.enabled = alpha > 0.0f;
    }
}
