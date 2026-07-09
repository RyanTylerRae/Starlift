#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DodecahedronBuilder
{
    private const string PENTAGON_MESH_GUID = "953e913ed3ca23c489459d04d6b2c2de";
    private const float FACE_SCALE = 5f;

    [MenuItem("Tools/Starlift/Generate Dodecahedron")]
    private static void Generate()
    {
        Mesh? pentagonMesh = LoadPentagonMesh();
        if (pentagonMesh == null)
        {
            Debug.LogError("DodecahedronBuilder: could not find Pentagon mesh for guid " + PENTAGON_MESH_GUID);
            return;
        }

        Material? pentagonMaterial = LoadPentagonMaterial();

        Vector3[] vertices = pentagonMesh.vertices;
        if (vertices.Length == 0)
        {
            Debug.LogError("DodecahedronBuilder: Pentagon mesh has no vertices");
            return;
        }

        Vector3 centroid = Vector3.zero;
        foreach (Vector3 v in vertices)
        {
            centroid += v;
        }
        centroid /= vertices.Length;

        // Pentagon.fbx's largest (flat) face lies in the local XZ plane, facing +Y.
        // Averaging all vertex normals is unreliable here since the mesh has side
        // geometry/thickness whose normals skew the average away from the true face normal.
        Vector3 localNormal = Vector3.up;

        Vector3 farthest = vertices[0];
        float farthestDist = 0f;
        foreach (Vector3 v in vertices)
        {
            Vector3 offset = v - centroid;
            Vector3 inPlane = offset - Vector3.Dot(offset, localNormal) * localNormal;
            float d = inPlane.magnitude;
            if (d > farthestDist)
            {
                farthestDist = d;
                farthest = v;
            }
        }

        Vector3 localOffset = farthest - centroid;
        Vector3 inPlaneOffset = localOffset - Vector3.Dot(localOffset, localNormal) * localNormal;
        Vector3 localRef = inPlaneOffset.normalized;
        float meshCircumradius = inPlaneOffset.magnitude;

        List<DodecFace> faces = BuildUnitDodecahedronFaces();

        GameObject root = new GameObject("Dodecahedron");
        Undo.RegisterCreatedObjectUndo(root, "Generate Dodecahedron");

        for (int i = 0; i < faces.Count; i++)
        {
            DodecFace face = faces[i];
            float worldScale = (meshCircumradius * FACE_SCALE) / face.circumradius;
            Vector3 targetCentroidPos = face.center * worldScale;

            Quaternion targetRot = Quaternion.LookRotation(face.normal, face.reference);
            Quaternion localRot = Quaternion.LookRotation(localNormal, localRef);
            Quaternion rotation = targetRot * Quaternion.Inverse(localRot);

            // Unity scales/rotates a mesh about its local origin, not its centroid, so the
            // object's position has to be offset back by the (scaled, rotated) pivot-to-centroid
            // vector to make the centroid itself land on the target face center.
            Vector3 position = targetCentroidPos - rotation * (FACE_SCALE * centroid);

            GameObject go = new GameObject("PentagonFace_" + i);
            go.transform.SetParent(root.transform);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * FACE_SCALE;

            MeshFilter meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = pentagonMesh;

            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
            if (pentagonMaterial != null)
            {
                meshRenderer.sharedMaterial = pentagonMaterial;
            }

            Undo.RegisterCreatedObjectUndo(go, "Generate Dodecahedron");
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static Mesh? LoadPentagonMesh()
    {
        string path = AssetDatabase.GUIDToAssetPath(PENTAGON_MESH_GUID);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
    }

    private static Material? LoadPentagonMaterial()
    {
        string path = AssetDatabase.GUIDToAssetPath(PENTAGON_MESH_GUID);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().FirstOrDefault();
    }

    private struct DodecFace
    {
        public Vector3 center;
        public Vector3 normal;
        public Vector3 reference;
        public float circumradius;
    }

    private static List<DodecFace> BuildUnitDodecahedronFaces()
    {
        float phi = (1f + Mathf.Sqrt(5f)) / 2f;
        float invPhi = 1f / phi;
        float[] signs = { -1f, 1f };

        List<Vector3> vertices = new List<Vector3>();
        foreach (float sx in signs)
        {
            foreach (float sy in signs)
            {
                foreach (float sz in signs)
                {
                    vertices.Add(new Vector3(sx, sy, sz));
                }
            }
        }

        foreach (float sy in signs)
        {
            foreach (float sz in signs)
            {
                vertices.Add(new Vector3(0f, sy * invPhi, sz * phi));
            }
        }

        foreach (float sx in signs)
        {
            foreach (float sy in signs)
            {
                vertices.Add(new Vector3(sx * invPhi, sy * phi, 0f));
            }
        }

        foreach (float sx in signs)
        {
            foreach (float sz in signs)
            {
                vertices.Add(new Vector3(sx * phi, 0f, sz * invPhi));
            }
        }

        // Face normals must use this specific cyclic ordering of the (0, 1, phi) permutation
        // to actually be supporting-hyperplane normals for the vertex set above - the other
        // chirality (0,1,phi)->(1,phi,0)->(phi,0,1) picks 5 non-coplanar vertices per "face".
        List<Vector3> faceDirs = new List<Vector3>();
        foreach (float sx in signs)
        {
            foreach (float sy in signs)
            {
                faceDirs.Add(new Vector3(sx * phi, sy, 0f));
            }
        }

        foreach (float sy in signs)
        {
            foreach (float sz in signs)
            {
                faceDirs.Add(new Vector3(0f, sy * phi, sz));
            }
        }

        foreach (float sx in signs)
        {
            foreach (float sz in signs)
            {
                faceDirs.Add(new Vector3(sx, 0f, sz * phi));
            }
        }

        List<DodecFace> faces = new List<DodecFace>();
        foreach (Vector3 dir in faceDirs)
        {
            Vector3 normal = dir.normalized;
            List<Vector3> faceVerts = vertices.OrderByDescending(v => Vector3.Dot(v, normal)).Take(5).ToList();

            Vector3 center = Vector3.zero;
            foreach (Vector3 v in faceVerts)
            {
                center += v;
            }
            center /= faceVerts.Count;

            Vector3 offset = faceVerts[0] - center;
            Vector3 reference = (offset - Vector3.Dot(offset, normal) * normal).normalized;

            faces.Add(new DodecFace
            {
                center = center,
                normal = normal,
                reference = reference,
                circumradius = offset.magnitude,
            });
        }

        return faces;
    }
}
