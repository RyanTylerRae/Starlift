#nullable enable

using UnityEngine;
using UnityEngine.UI;

// drives the screen-jelly edge overlay from the suffocation grace period
public class SuffocationOverlay : MonoBehaviour
{
    public Image? overlayImage;
    // overlay opacity once the grace period has fully elapsed
    public float maxOpacity = 0.5f;
    // seconds to fade from maxOpacity back to 0 once the player can breathe again
    public float fadeOutDuration = 1f;

    private OxygenSystem? oxygenSystem = null;
    private float currentOpacity = 0f;

    void Awake()
    {
        oxygenSystem = GetComponentInParent<OxygenSystem>();
        SetOpacity(0f);
    }

    void LateUpdate()
    {
        if (oxygenSystem == null)
        {
            return;
        }

        float targetOpacity = oxygenSystem.SuffocationProgress * maxOpacity;

        if (targetOpacity >= currentOpacity || fadeOutDuration <= 0f)
        {
            // fading in already ramps smoothly with the grace timer, so follow it directly
            currentOpacity = targetOpacity;
        }
        else
        {
            currentOpacity = Mathf.MoveTowards(currentOpacity, targetOpacity, Time.deltaTime * maxOpacity / fadeOutDuration);
        }

        SetOpacity(currentOpacity);
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
