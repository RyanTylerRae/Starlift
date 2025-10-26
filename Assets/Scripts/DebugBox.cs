using UnityEngine;

public static class DebugBox
{
    public static GameObject DrawBox(Vector3 center, float width, float lineThickness, Color color, float duration = 0f)
    {
        // Create a GameObject to hold the line renderer
        GameObject boxGO = new GameObject("DebugBox");
        LineRenderer lr = boxGO.AddComponent<LineRenderer>();
        boxGO.transform.position = center;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = lineThickness;
        lr.positionCount = 16; // 12 edges but connected for closed shape
        lr.useWorldSpace = true;
        lr.loop = false;

        // Define 8 corners of the box
        float h = width * 0.5f;
        Vector3[] p = new Vector3[8]
        {
            center + new Vector3(-h, -h, -h),
            center + new Vector3(h, -h, -h),
            center + new Vector3(h, -h, h),
            center + new Vector3(-h, -h, h),

            center + new Vector3(-h, h, -h),
            center + new Vector3(h, h, -h),
            center + new Vector3(h, h, h),
            center + new Vector3(-h, h, h)
        };

        // Connect corners for edges
        Vector3[] edges = new Vector3[]
        {
            p[0], p[1], p[2], p[3], p[0], // bottom loop
            p[4], p[5], p[6], p[7], p[4], // top loop
            p[5], p[1], p[2], p[6], p[7], p[3] // vertical connections
        };

        lr.SetPositions(edges);

        if (duration > 0f)
        {
            Object.Destroy(boxGO, duration);
        }

        return boxGO;
    }
}