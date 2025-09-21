using UnityEngine;
using UnityEngine.UI;

public class Reticle : MonoBehaviour
{
    [Header("Reticle Settings")]
    public float reticleSize = 4f;
    public Color reticleColor = Color.white;

    private Canvas reticleCanvas;
    private Image reticleImage;

    void Start()
    {
        CreateReticle();
    }

    void CreateReticle()
    {
        GameObject canvasObject = new GameObject("ReticleCanvas");
        reticleCanvas = canvasObject.AddComponent<Canvas>();
        reticleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        reticleCanvas.sortingOrder = 100;

        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        GraphicRaycaster graphicRaycaster = canvasObject.AddComponent<GraphicRaycaster>();

        GameObject reticleObject = new GameObject("Reticle");
        reticleObject.transform.SetParent(canvasObject.transform);

        reticleImage = reticleObject.AddComponent<Image>();
        reticleImage.sprite = CreateCircleSprite();
        reticleImage.color = reticleColor;

        RectTransform reticleRect = reticleObject.GetComponent<RectTransform>();
        reticleRect.anchorMin = new Vector2(0.5f, 0.5f);
        reticleRect.anchorMax = new Vector2(0.5f, 0.5f);
        reticleRect.pivot = new Vector2(0.5f, 0.5f);
        reticleRect.anchoredPosition = Vector2.zero;
        reticleRect.sizeDelta = new Vector2(reticleSize, reticleSize);
    }

    Sprite CreateCircleSprite()
    {
        int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] pixels = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float radius = textureSize / 2f - 2;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                if (distance <= radius && distance >= radius - 2)
                {
                    pixels[y * textureSize + x] = Color.white;
                }
                else
                {
                    pixels[y * textureSize + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }
}