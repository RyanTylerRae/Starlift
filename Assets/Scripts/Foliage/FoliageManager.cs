using System.Collections.Generic;
using UnityEngine;

public class FoliageManager : MonoBehaviour
{
    [SerializeField] private FoliagePaintData paintData;
    [SerializeField] private List<FoliageLayer> layers = new();
    [SerializeField] private Transform instanceRoot;
    [SerializeField] private int distributionSeed = 0;

    private void Start()
    {
        if (paintData == null)
        {
            Debug.LogError("FoliageManager: no FoliagePaintData assigned.");
            return;
        }

        if (instanceRoot == null)
        {
            GameObject root = new GameObject("FoliageInstances");
            instanceRoot = root.transform;
        }

        var candidates = FoliageDistributor.SampleCandidates(paintData);

        foreach (FoliageLayer layer in layers)
        {
            if (layer == null || layer.prefab == null) continue;

            var thinned = FoliageDistributor.PoissonThin(candidates, layer, distributionSeed);
            foreach (var c in thinned)
            {
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, c.normal);
                Instantiate(layer.prefab, c.position, rot, instanceRoot);
            }
        }
    }
}
