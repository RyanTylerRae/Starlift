using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField]
    private Image fadeImage;

    public async Task FadeToOpacity(float targetOpacity, float duration)
    {
        if (fadeImage == null)
        {
            Debug.LogError("ScreenFader: Fade Image is not assigned!");
            return;
        }

        targetOpacity = Mathf.Clamp01(targetOpacity);

        if (duration > 0.0f)
        {
            Color startColor = fadeImage.color;
            float startOpacity = startColor.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Color newColor = startColor;
                newColor.a = Mathf.Lerp(startOpacity, targetOpacity, t);
                fadeImage.color = newColor;

                await Task.Yield();
            }
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetOpacity;
        fadeImage.color = finalColor;
    }
}
