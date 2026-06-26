using System.Collections.Generic;
using UnityEngine;

public static class FoliageDistributor
{
    public struct FoliageCandidate
    {
        public Vector3 position;
        public Vector3 normal;
        public Color32 color;
    }

    // Convert paint samples directly to candidates — no mesh lookup needed.
    public static List<FoliageCandidate> SampleCandidates(FoliagePaintData data)
    {
        var result = new List<FoliageCandidate>();
        foreach (var s in data.GetAll())
        {
            if (s.color.a > 0)
                result.Add(new FoliageCandidate { position = s.position, normal = s.normal, color = s.color });
        }
        return result;
    }

    // Greedy Poisson disk thinning — filters to one layer's color then thins by spacing.
    public static List<FoliageCandidate> PoissonThin(
        List<FoliageCandidate> candidates,
        FoliageLayer layer,
        int seed = 0)
    {
        var matching = new List<FoliageCandidate>();
        foreach (var c in candidates)
        {
            if (layer.ColorMatches(c.color) && c.color.a > 0)
                matching.Add(c);
        }

        // Fisher-Yates shuffle
        var rng = new System.Random(seed);
        for (int i = matching.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (matching[i], matching[j]) = (matching[j], matching[i]);
        }

        var accepted = new List<FoliageCandidate>();
        foreach (var candidate in matching)
        {
            if (candidate.color.a == 0) continue;

            bool tooClose = false;
            foreach (var a in accepted)
            {
                if (Vector3.Distance(candidate.position, a.position) < layer.averageSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Alpha drives spawn probability exponentially: p = (a/255)^2
            double p = candidate.color.a / 255.0;
            if (rng.NextDouble() > p * p) continue;

            accepted.Add(candidate);
        }

        return accepted;
    }
}
