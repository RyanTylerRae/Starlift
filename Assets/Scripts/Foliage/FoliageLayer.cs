#nullable enable

using UnityEngine;

[CreateAssetMenu(menuName = "Starlift/Foliage Layer")]
public class FoliageLayer : ScriptableObject
{
    public Color32 targetColor = new Color32(0, 128, 0, 255);
    public Color32 colorTolerance = new Color32(10, 10, 10, 0);
    public GameObject? prefab;
    public float averageSpacing = 1f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    // Nearest perfect square cell size that targets ~350 instances per leaf (middle of 200-500).
    public float SuggestedCellSize
    {
        get
        {
            float ideal = averageSpacing * Mathf.Sqrt(350f);
            int n = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(ideal)));
            return n * n;
        }
    }

    public float GetEffectiveSpacing(byte alpha)
    {
        if (alpha == 0)
        {
            return float.PositiveInfinity;
        }
        return averageSpacing * (255f / alpha);
    }

    public bool ColorMatches(Color32 sample)
    {
        return Mathf.Abs(sample.r - targetColor.r) <= colorTolerance.r &&
               Mathf.Abs(sample.g - targetColor.g) <= colorTolerance.g &&
               Mathf.Abs(sample.b - targetColor.b) <= colorTolerance.b;
    }
}
