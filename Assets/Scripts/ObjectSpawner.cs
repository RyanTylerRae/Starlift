#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Cylinder Volume")]
    public float radius = 5f;
    public float height = 10f;

    [Header("Spawning")]
    public GameObject[] prefabs = new GameObject[0];
    public int maxObjects = 100;
    public bool spawnOnStart = true;

    [Header("Placement")]
    public float minSpacing = 1.5f;
    public int poissonAttempts = 30;

    [Header("Scale")]
    public float minScale = 0.5f;
    public float maxScale = 2.0f;

    [Header("Rotation")]
    [Range(0f, 1f)] public float rotateChance = 0.5f;
    public float minRotationSpeed = 0.1f;
    public float maxRotationSpeed = 1.0f;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnObjects();
        }
    }

    public void SpawnObjects()
    {
        if (prefabs.Length == 0)
        {
            return;
        }

        foreach (Vector3 localPos in PoissonDiskSample())
        {
            SpawnAt(localPos);
        }
    }

    private List<Vector3> PoissonDiskSample()
    {
        Bounds bounds = new Bounds(new Vector3(0f, height * 0.5f, 0f), new Vector3(radius * 2f, height, radius * 2f));
        float cellSize = minSpacing / Mathf.Sqrt(3f);

        int gx = Mathf.CeilToInt(bounds.size.x / cellSize) + 1;
        int gy = Mathf.CeilToInt(bounds.size.y / cellSize) + 1;
        int gz = Mathf.CeilToInt(bounds.size.z / cellSize) + 1;

        int[,,] grid = new int[gx, gy, gz];
        for (int i = 0; i < gx; i++)
        {
            for (int j = 0; j < gy; j++)
            {
                for (int k = 0; k < gz; k++)
                {
                    grid[i, j, k] = -1;
                }
            }
        }

        List<Vector3> result = new List<Vector3>();
        List<Vector3> active = new List<Vector3>();

        Vector3 seed = RandomInBounds(bounds);
        int seedAttempts = 0;
        while (!IsInsideCylinder(seed) && seedAttempts++ < 1000)
        {
            seed = RandomInBounds(bounds);
        }

        if (!IsInsideCylinder(seed))
        {
            return result;
        }

        result.Add(seed);
        active.Add(seed);
        SetGrid(seed, 0, bounds, cellSize, grid);

        while (active.Count > 0 && result.Count < maxObjects)
        {
            int activeIdx = Random.Range(0, active.Count);
            Vector3 origin = active[activeIdx];
            bool found = false;

            for (int attempt = 0; attempt < poissonAttempts; attempt++)
            {
                float dist = Random.Range(minSpacing, 2f * minSpacing);
                Vector3 candidate = origin + Random.onUnitSphere * dist;

                if (!IsInsideCylinder(candidate))
                {
                    continue;
                }
                if (!IsFarEnough(candidate, result, grid, bounds, cellSize))
                {
                    continue;
                }

                result.Add(candidate);
                active.Add(candidate);
                SetGrid(candidate, result.Count - 1, bounds, cellSize, grid);
                found = true;

                if (result.Count >= maxObjects)
                {
                    break;
                }
            }

            if (!found)
            {
                active.RemoveAt(activeIdx);
            }
        }

        return result;
    }

    private bool IsInsideCylinder(Vector3 localPoint)
    {
        float radialDist = new Vector2(localPoint.x, localPoint.z).magnitude;
        return localPoint.y >= 0f && localPoint.y <= height && radialDist <= radius;
    }

    private bool IsFarEnough(Vector3 candidate, List<Vector3> points, int[,,] grid, Bounds bounds, float cellSize)
    {
        Vector3Int g = WorldToGrid(candidate, bounds, cellSize);
        int gxLen = grid.GetLength(0);
        int gyLen = grid.GetLength(1);
        int gzLen = grid.GetLength(2);

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int nx = g.x + dx;
                    int ny = g.y + dy;
                    int nz = g.z + dz;

                    if (nx < 0 || nx >= gxLen || ny < 0 || ny >= gyLen || nz < 0 || nz >= gzLen)
                    {
                        continue;
                    }

                    int idx = grid[nx, ny, nz];
                    if (idx == -1)
                    {
                        continue;
                    }

                    if (Vector3.Distance(candidate, points[idx]) < minSpacing)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void SetGrid(Vector3 point, int index, Bounds bounds, float cellSize, int[,,] grid)
    {
        Vector3Int g = WorldToGrid(point, bounds, cellSize);
        grid[g.x, g.y, g.z] = index;
    }

    private Vector3Int WorldToGrid(Vector3 point, Bounds bounds, float cellSize)
    {
        Vector3 local = point - bounds.min;
        return new Vector3Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.y / cellSize),
            Mathf.FloorToInt(local.z / cellSize)
        );
    }

    private Vector3 RandomInBounds(Bounds bounds)
    {
        return bounds.min + new Vector3(
            Random.Range(0f, bounds.size.x),
            Random.Range(0f, bounds.size.y),
            Random.Range(0f, bounds.size.z)
        );
    }

    private Vector3 LocalToWorld(Vector3 localPos)
    {
        return transform.position + transform.rotation * localPos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        const int segments = 32;
        const float step = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;

            Vector3 baseA = LocalToWorld(new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius));
            Vector3 baseB = LocalToWorld(new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius));
            Vector3 topA  = LocalToWorld(new Vector3(Mathf.Cos(a0) * radius, height, Mathf.Sin(a0) * radius));
            Vector3 topB  = LocalToWorld(new Vector3(Mathf.Cos(a1) * radius, height, Mathf.Sin(a1) * radius));

            Gizmos.DrawLine(baseA, baseB);
            Gizmos.DrawLine(topA, topB);

            if (i % (segments / 8) == 0)
            {
                Gizmos.DrawLine(baseA, topA);
            }
        }
    }

    private void SpawnAt(Vector3 localPos)
    {
        if (prefabs.Length == 0)
        {
            return;
        }

        Vector3 worldPos = LocalToWorld(localPos);
        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        GameObject obj = Instantiate(prefab, worldPos, Random.rotation);

        obj.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);

        if (Random.value < rotateChance)
        {
            SlowlyRotate rotate = obj.AddComponent<SlowlyRotate>();
            rotate.rotationAxis = Random.onUnitSphere;
            rotate.rotationsPerSecond = Random.Range(minRotationSpeed, maxRotationSpeed);
        }
    }
}
