using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer), typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class SplineTube : MonoBehaviour
{
    [SerializeField] float radius = 0.5f;
    [SerializeField] float interval = 0.5f;
    [SerializeField, Min(3)] int sides = 8;
    [SerializeField] bool caps = true;

    SplineContainer splineContainer;
    MeshFilter meshFilter;
    Mesh mesh;
    bool dirty;

    void OnEnable()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
        Spline.Changed += OnSplineChanged;
        dirty = true;
    }

    void OnDisable()
    {
        if (splineContainer != null)
        {
            Spline.Changed -= OnSplineChanged;
        }
        DestroyMesh();
    }

    void OnDestroy()
    {
        DestroyMesh();
    }

    void OnValidate()
    {
        dirty = true;
    }

    void Update()
    {
        if (!dirty)
        {
            return;
        }
        dirty = false;

        Rebuild();
    }

    void OnSplineChanged(Spline spline, int index, SplineModification mod)
    {
        if (splineContainer != null && spline == splineContainer.Spline)
        {
            dirty = true;
        }
    }

    void DestroyMesh()
    {
        if (mesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }

            mesh = null;
        }

        if (meshFilter != null)
        {
            meshFilter.sharedMesh = null;
        }
    }

    public void Rebuild()
    {
        if (splineContainer == null || meshFilter == null)
        {
            return;
        }

        var spline = splineContainer.Spline;
        float totalLength = SplineUtility.CalculateLength(spline, float4x4.identity);
        if (totalLength <= 0f)
        {
            return;
        }

        int ringCount = Mathf.Max(2, Mathf.FloorToInt(totalLength / Mathf.Max(0.001f, interval)) + 1);
        float step = totalLength / (ringCount - 1);

        var positions = new float3[ringCount];
        var rights = new float3[ringCount];
        var ups = new float3[ringCount];
        var tangents = new float3[ringCount];

        for (int i = 0; i < ringCount; i++)
        {
            float d = math.min(i * step, totalLength);
            float t = SplineUtility.ConvertIndexUnit(spline, d, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tan, out float3 up);

            tan = math.normalizesafe(tan);
            up = math.normalizesafe(up);
            float3 right = math.normalizesafe(math.cross(up, tan));
            up = math.normalizesafe(math.cross(tan, right));

            positions[i] = pos;
            rights[i] = right;
            ups[i] = up;
            tangents[i] = tan;
        }

        int vertsPerRing = sides + 1;
        var verts = new List<Vector3>(ringCount * vertsPerRing);
        var normals = new List<Vector3>(ringCount * vertsPerRing);
        var uvs = new List<Vector2>(ringCount * vertsPerRing);
        var tris = new List<int>((ringCount - 1) * sides * 6);

        for (int i = 0; i < ringCount; i++)
        {
            float v = (float)i / (ringCount - 1);
            for (int j = 0; j <= sides; j++)
            {
                float u = (float)j / sides;
                float angle = u * math.PI * 2f;
                float cosA = math.cos(angle);
                float sinA = math.sin(angle);

                float3 n = cosA * rights[i] + sinA * ups[i];
                verts.Add(positions[i] + radius * n);
                normals.Add(n);
                uvs.Add(new Vector2(u, v));
            }
        }

        for (int i = 0; i < ringCount - 1; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int a = i * vertsPerRing + j;
                int b = a + 1;
                int c = (i + 1) * vertsPerRing + j;
                int d = c + 1;

                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }
        }

        if (caps)
        {
            BuildCap(verts, normals, uvs, tris, positions[0], rights[0], ups[0], -tangents[0], false);
            BuildCap(verts, normals, uvs, tris, positions[ringCount - 1], rights[ringCount - 1], ups[ringCount - 1], tangents[ringCount - 1], true);
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "SplineTube" };
        }
        else
        {
            mesh.Clear();
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    void BuildCap(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris,
        float3 center, float3 right, float3 up, float3 normal, bool flipWinding)
    {
        int baseIdx = verts.Count;

        verts.Add(center);
        normals.Add(normal);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int j = 0; j < sides; j++)
        {
            float angle = (float)j / sides * math.PI * 2f;
            float cosA = math.cos(angle);
            float sinA = math.sin(angle);
            verts.Add(center + radius * (cosA * right + sinA * up));
            normals.Add(normal);
            uvs.Add(new Vector2(cosA * 0.5f + 0.5f, sinA * 0.5f + 0.5f));
        }

        for (int j = 0; j < sides; j++)
        {
            int a = baseIdx;
            int b = baseIdx + 1 + j;
            int c = baseIdx + 1 + (j + 1) % sides;

            if (flipWinding)
            {
                tris.Add(a); tris.Add(b); tris.Add(c);
            }
            else
            {
                tris.Add(a); tris.Add(c); tris.Add(b);
            }
        }
    }
}
