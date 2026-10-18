#nullable enable

using UnityEngine;
using UnityEngine.UI;

// one-shot screen-jelly edge flash: quick fade in, short hold, eased fade out
public class ScreenJellyFlash : MonoBehaviour
{
    public Image? overlayImage;
    // overlay opacity at the top of the flash
    public float peakOpacity = 1f;
    public float fadeInDuration = 0.1f;
    public float holdDuration = 0.15f;
    public float fadeOutDuration = 1.2f;

    private float elapsed = 0f;
    private bool isPlaying = false;

    void Awake()
    {
        SetOpacity(0f);
    }

    // starts the flash, restarting it if one is already playing
    public void Play()
    {
        elapsed = 0f;
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        elapsed += Time.deltaTime;

        if (elapsed < fadeInDuration)
        {
            SetOpacity(peakOpacity * elapsed / fadeInDuration);
            return;
        }

        float fadeOutElapsed = elapsed - fadeInDuration - holdDuration;
        if (fadeOutElapsed < 0f)
        {
            SetOpacity(peakOpacity);
            return;
        }

        if (fadeOutDuration <= 0f || fadeOutElapsed >= fadeOutDuration)
        {
            SetOpacity(0f);
            isPlaying = false;
            return;
        }

        float t = fadeOutElapsed / fadeOutDuration;
        SetOpacity(peakOpacity * (1f - t * t));  // easing to feel better
    }

    private void SetOpacity(float opacity)
    {
        if (overlayImage == null)
        {
            return;
        }

        Color newColor = overlayImage.color;
        newColor.a = opacity;
        overlayImage.color = newColor;
    }
}
