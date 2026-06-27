#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class FoliagePaintData : MonoBehaviour
{
    public List<FoliageSample> samples = new();

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
            s.color.a = (byte)Mathf.Min(255, s.color.a + newSample.color.a);
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
            if (dSq > radiusSq)
            {
                continue;
            }

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

    public void EraseInShape(Vector3 origin, Vector3 right, Vector3 fwd, float radius, bool isSquare)
    {
        float radiusSq = radius * radius;
        for (int i = samples.Count - 1; i >= 0; i--)
        {
            Vector3 delta = samples[i].position - origin;
            if (delta.sqrMagnitude > radiusSq)
            {
                continue;
            }
            float u = Vector3.Dot(delta, right);
            float v = Vector3.Dot(delta, fwd);
            bool inside = isSquare
                ? Mathf.Abs(u) <= radius && Mathf.Abs(v) <= radius
                : u * u + v * v <= radius * radius;
            if (inside)
            {
                samples.RemoveAt(i);
            }
        }
    }

    public byte GetAlphaAt(Vector3 position, float radius)
    {
        float radiusSq = radius * radius;
        float closestDist = float.MaxValue;
        byte alpha = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            float dSq = (samples[i].position - position).sqrMagnitude;
            if (dSq < radiusSq && dSq < closestDist)
            {
                closestDist = dSq;
                alpha = samples[i].color.a;
            }
        }
        return alpha;
    }

    public void Clear()
    {
        samples.Clear();
    }

    public IReadOnlyList<FoliageSample> GetAll()
    {
        return samples;
    }
}
