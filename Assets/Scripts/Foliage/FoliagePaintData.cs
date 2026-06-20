using System.Collections.Generic;
using UnityEngine;

public class FoliagePaintData : MonoBehaviour
{
    [SerializeField] private List<FoliageSample> samples = new();

    // Paint a sample, blending alpha with any existing nearby sample.
    public void Paint(FoliageSample newSample, float mergeRadius)
    {
        float mergeRadiusSq = mergeRadius * mergeRadius;
        int closestIdx = -1;
        float closestDist = float.MaxValue;

        for (int i = 0; i < samples.Count; i++)
        {
            float dSq = (samples[i].position - newSample.position).sqrMagnitude;
            if (dSq < mergeRadiusSq && dSq < closestDist)
            {
                closestIdx = i;
                closestDist = dSq;
            }
        }

        if (closestIdx >= 0)
        {
            var s = samples[closestIdx];
            s.color = new Color32(
                newSample.color.r,
                newSample.color.g,
                newSample.color.b,
                (byte)Mathf.Min(255, s.color.a + newSample.color.a));
            s.position = newSample.position;
            s.normal = newSample.normal;
            samples[closestIdx] = s;
        }
        else
        {
            samples.Add(newSample);
        }
    }

    // Erase within radius, subtracting alpha based on distance falloff.
    public void Erase(Vector3 brushCenter, float brushRadius, float brushAlpha, bool softFalloff)
    {
        float radiusSq = brushRadius * brushRadius;
        for (int i = samples.Count - 1; i >= 0; i--)
        {
            float dSq = (samples[i].position - brushCenter).sqrMagnitude;
            if (dSq > radiusSq) continue;

            float t = Mathf.Sqrt(dSq) / brushRadius;
            float amount = softFalloff ? brushAlpha * (1f - (3 * t * t - 2 * t * t * t)) : brushAlpha;

            int newAlpha = samples[i].color.a - Mathf.RoundToInt(amount * 255f);
            if (newAlpha <= 0)
            {
                samples.RemoveAt(i);
            }
            else
            {
                var s = samples[i];
                s.color.a = (byte)newAlpha;
                samples[i] = s;
            }
        }
    }

    public void Clear() => samples.Clear();
    public IReadOnlyList<FoliageSample> GetAll() => samples;
}
