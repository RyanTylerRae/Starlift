#nullable enable

using TMPro;
using UnityEngine;

/// <summary>
/// Shows "collected/total" in the top-left corner when a collectible is picked up, then fades it out.
/// Goes on the player's screen-space HUD canvas and builds its text in code. The total counts every
/// CollectibleComponent in the loaded scenes, including ones already hidden as collected.
/// </summary>
public class CollectibleCounter : MonoBehaviour
{
    [Header("Layout")]
    // pixels in from the top-left corner of the screen
    public Vector2 margin = new Vector2(32.0f, 32.0f);
    public float fontSize = 36.0f;
    public Color color = Color.white;

    // leave empty to use TMP's default font
    public TMP_FontAsset? font;

    [Header("Timing")]
    public float lingerDuration = 3.0f;
    public float fadeOutDuration = 1.0f;

    private TextMeshProUGUI? countText;
    private float elapsed = 0.0f;
    private bool isShowing = false;

    public void Start()
    {
        GameObject textObject = new GameObject("CollectibleCount");
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(transform, false);

        countText = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            countText.font = font;
        }

        countText.fontSize = fontSize;
        countText.color = color;
        countText.alignment = TextAlignmentOptions.TopLeft;
        countText.textWrappingMode = TextWrappingModes.NoWrap;
        countText.overflowMode = TextOverflowModes.Overflow;
        countText.raycastTarget = false;
        countText.text = string.Empty;
        countText.alpha = 0.0f;

        RectTransform textRect = countText.rectTransform;
        textRect.anchorMin = new Vector2(0.0f, 1.0f);
        textRect.anchorMax = new Vector2(0.0f, 1.0f);
        textRect.pivot = new Vector2(0.0f, 1.0f);
        textRect.anchoredPosition = new Vector2(margin.x, -margin.y);
    }

    public void Show()
    {
        if (countText == null)
        {
            return;
        }

        CollectibleComponent[] collectibles = FindObjectsByType<CollectibleComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int collectedCount = 0;
        foreach (CollectibleComponent collectible in collectibles)
        {
            if (collectible.IsCollected)
            {
                ++collectedCount;
            }
        }

        countText.text = $"{collectedCount}/{collectibles.Length}";
        countText.alpha = 1.0f;
        elapsed = 0.0f;
        isShowing = true;
    }

    public void Update()
    {
        if (!isShowing || countText == null)
        {
            return;
        }

        elapsed += Time.deltaTime;

        float fadeOutElapsed = elapsed - lingerDuration;
        if (fadeOutElapsed < 0.0f)
        {
            return;
        }

        if (fadeOutDuration <= 0.0f || fadeOutElapsed >= fadeOutDuration)
        {
            countText.alpha = 0.0f;
            isShowing = false;
            return;
        }

        float t = fadeOutElapsed / fadeOutDuration;
        countText.alpha = 1.0f - t * t;
    }
}
