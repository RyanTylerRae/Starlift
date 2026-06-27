#nullable enable

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(FoliagePaintData))]
public class FoliagePaintDataEditor : Editor
{
    private static Material? lineMaterial;

    private static Material? GetLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            return null;
        }

        lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        return lineMaterial;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawSamplesGizmo(FoliagePaintData data, GizmoType gizmoType)
    {
        var mat = GetLineMaterial();
        if (mat == null)
        {
            return;
        }

        var camera = Camera.current;
        if (camera == null)
        {
            return;
        }

        const float armLength = 0.15f;
        const float lineWidth = 16f;

        mat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.QUADS);

        foreach (var s in data.GetAll())
        {
            if (!IsInFrustum(s.position, camera))
            {
                continue;
            }

            Color32 c = s.color;
            GL.Color(new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f * 0.85f + 0.15f));

            Vector3 right = Mathf.Abs(Vector3.Dot(s.normal, Vector3.up)) < 0.99f
                ? Vector3.Cross(s.normal, Vector3.up).normalized
                : Vector3.Cross(s.normal, Vector3.forward).normalized;
            Vector3 fwd = Vector3.Cross(s.normal, right);

            DrawThickLine(s.position - right * armLength, s.position + right * armLength, lineWidth, camera);
            DrawThickLine(s.position - fwd * armLength, s.position + fwd * armLength, lineWidth, camera);
        }

        GL.End();
        GL.PopMatrix();
    }

    private static void DrawThickLine(Vector3 p0, Vector3 p1, float widthPx, Camera camera)
    {
        Vector3 p0s = camera.WorldToScreenPoint(p0);
        Vector3 p1s = camera.WorldToScreenPoint(p1);
        if (p0s.z <= 0f || p1s.z <= 0f)
        {
            return;
        }

        Vector2 dir = new Vector2(p1s.x - p0s.x, p1s.y - p0s.y);
        if (dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector2 perp = new Vector2(-dir.y, dir.x).normalized * widthPx * 0.5f;
        Vector3 perp0 = ScreenOffsetToWorld(p0, perp, camera);
        Vector3 perp1 = ScreenOffsetToWorld(p1, perp, camera);

        GL.Vertex(p0 - perp0);
        GL.Vertex(p0 + perp0);
        GL.Vertex(p1 + perp1);
        GL.Vertex(p1 - perp1);
    }

    private static Vector3 ScreenOffsetToWorld(Vector3 worldPos, Vector2 screenOffset, Camera camera)
    {
        Vector3 s = camera.WorldToScreenPoint(worldPos);
        return camera.ScreenToWorldPoint(new Vector3(s.x + screenOffset.x, s.y + screenOffset.y, s.z))
             - camera.ScreenToWorldPoint(s);
    }

    private static bool IsInFrustum(Vector3 worldPos, Camera camera)
    {
        Vector3 vp = camera.WorldToViewportPoint(worldPos);
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f;
    }
}
