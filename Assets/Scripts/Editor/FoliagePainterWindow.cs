#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class FoliagePainterWindow : EditorWindow
{
    private enum BrushShape { Circle, Square }

    // Paint mode
    private bool paintingEnabled = false;

    // Brush
    private Color paintColor = Color.green;
    private float brushRadius = 1f;
    private BrushShape brushShape = BrushShape.Circle;
    private float brushRotation = 0f;
    private float normalOffset = 0f;
    private float raycastDistance = 1f;
    private float brushStrength = 1f;
    private float falloffPower = 1f;
    private float sampleSpacing = 0.2f;
    private bool eraseMode = false;
    private float maxNormalDeviation = 15f;
    private LayerMask paintableLayers = ~(1 << 2); // exclude Ignore Raycast by default

    // Stroke state
    private Vector3 strokeStartNormal;

    // Palette
    private List<FoliageLayer?> layers = new();
    private Vector2 layerScroll;
    private int selectedLayerIndex = -1;

    [MenuItem("Tools/Starlift/Foliage Painter")]
    private static void Open()
    {
        GetWindow<FoliagePainterWindow>("Foliage Painter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        // Paint mode toggle
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = paintingEnabled ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button(paintingEnabled ? "Painting  (click to exit)" : "Paint Mode  (click to enter)", GUILayout.Height(30)))
        {
            paintingEnabled = !paintingEnabled;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
        paintColor = EditorGUILayout.ColorField(new GUIContent("Paint Color"), paintColor, true, false, false);
        brushRadius = EditorGUILayout.FloatField("Brush Radius", brushRadius);
        brushShape = (BrushShape)EditorGUILayout.EnumPopup("Brush Shape", brushShape);
        brushRotation = EditorGUILayout.Slider("Rotation", brushRotation, 0f, 360f);
        normalOffset = EditorGUILayout.FloatField("Normal Offset", normalOffset);
        raycastDistance = EditorGUILayout.FloatField("Raycast Distance", raycastDistance);
        brushStrength = EditorGUILayout.Slider("Brush Strength", brushStrength, 0f, 1f);
        falloffPower = EditorGUILayout.Slider("Falloff Power", falloffPower, 0.1f, 5f);
        sampleSpacing = EditorGUILayout.FloatField("Sample Spacing", sampleSpacing);
        eraseMode = EditorGUILayout.Toggle("Erase Mode", eraseMode);
        maxNormalDeviation = EditorGUILayout.Slider("Max Normal Deviation", maxNormalDeviation, 0f, 180f);
        paintableLayers = (LayerMask)EditorGUILayout.MaskField(
            "Paintable Layers",
            InternalEditorUtility.LayerMaskToConcatenatedLayersMask(paintableLayers),
            InternalEditorUtility.layers);
        paintableLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(paintableLayers);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

        layerScroll = EditorGUILayout.BeginScrollView(layerScroll, GUILayout.Height(200));
        int removeIndex = -1;
        for (int i = 0; i < layers.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            layers[i] = (FoliageLayer?)EditorGUILayout.ObjectField(
                layers[i], typeof(FoliageLayer), false, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            if (layers[i] != null && DrawLayerRow(layers[i]!, i == selectedLayerIndex))
            {
                selectedLayerIndex = i;
                Color32 tc = layers[i]!.targetColor;
                paintColor = new Color(tc.r / 255f, tc.g / 255f, tc.b / 255f);
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        if (removeIndex >= 0)
        {
            if (selectedLayerIndex >= removeIndex)
                selectedLayerIndex--;
            layers.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("+ Add Layer"))
            layers.Add(null);

        EditorGUILayout.Space();

        FoliagePaintData? data = FindPaintData();
        using (new EditorGUI.DisabledScope(data == null))
        {
            if (GUILayout.Button("Clear All Paint"))
                ClearAllPaint(data!);
        }
    }

    private bool DrawLayerRow(FoliageLayer layer, bool isSelected)
    {
        EditorGUILayout.BeginHorizontal();

        Color32 c = layer.targetColor;
        Rect swatchRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(16));

        if (isSelected)
            EditorGUI.DrawRect(new Rect(swatchRect.x - 1, swatchRect.y - 1, swatchRect.width + 2, swatchRect.height + 2), Color.white);
        EditorGUI.DrawRect(swatchRect, new Color(c.r / 255f, c.g / 255f, c.b / 255f));

        bool clicked = Event.current.type == EventType.MouseDown && swatchRect.Contains(Event.current.mousePosition);
        if (clicked)
        {
            Event.current.Use();
        }

        EditorGUILayout.LabelField(
            $"spacing: {layer.averageSpacing:F2}m  prefab: {(layer.prefab != null ? layer.prefab.name : "none")}",
            GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();

        // Alpha-density bar: left = alpha 0 (sparse), right = alpha 255 (dense)
        Rect barRect = EditorGUILayout.GetControlRect(GUILayout.Height(6));
        int steps = Mathf.Max(2, (int)barRect.width);
        float stepW = barRect.width / steps;
        for (int s = 0; s < steps; s++)
        {
            byte alpha = (byte)(s * 255 / (steps - 1));
            float spacing = layer.GetEffectiveSpacing(alpha);
            float density = float.IsPositiveInfinity(spacing)
                ? 0f
                : Mathf.Clamp01(layer.averageSpacing / spacing);
            Color col = new Color(c.r / 255f, c.g / 255f, c.b / 255f, density);
            EditorGUI.DrawRect(new Rect(barRect.x + s * stepW, barRect.y, stepW + 1, barRect.height), col);
        }

        return clicked;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!paintingEnabled)
        {
            return;
        }

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit[] allHits = Physics.RaycastAll(ray, float.MaxValue, paintableLayers);
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

        bool didHit = allHits.Length > 0;
        bool isValidTarget = false;
        RaycastHit hitInfo = default;

        if (didHit)
        {
            hitInfo = allHits[0];
            foreach (var h in allHits)
            {
                if (h.collider.gameObject.isStatic)
                {
                    hitInfo = h;
                    isValidTarget = true;
                    break;
                }
            }
        }

        // Brush outline + normal offset preview
        if (didHit && e.type == EventType.Repaint)
        {
            Handles.color = !isValidTarget
                ? new Color(1f, 0.2f, 0.2f, 0.8f)
                : eraseMode
                    ? new Color(1f, 0.2f, 0.2f, 0.8f)
                    : new Color(1f, 1f, 1f, 0.8f);

            var (right, fwd) = BuildBasis(hitInfo.normal, brushRotation);
            Vector3 brushCenter = hitInfo.point + hitInfo.normal * normalOffset;

            if (normalOffset != 0f)
                Handles.DrawAAPolyLine(20f, hitInfo.point, brushCenter);

            DrawBrushOutline(brushCenter, right, fwd, brushRadius, brushShape);
        }

        bool isPaintEvent = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                            && e.button == 0 && !e.alt;

        if (isPaintEvent && isValidTarget)
        {
            if (e.type == EventType.MouseDown)
                strokeStartNormal = hitInfo.normal;

            bool normalOk = Vector3.Angle(hitInfo.normal, strokeStartNormal) <= maxNormalDeviation;

            if (normalOk)
            {
                FoliagePaintData data = GetOrCreatePaintData();

                if (eraseMode)
                    data.Erase(hitInfo.point, brushRadius, brushStrength, true);
                else
                    ApplyPaintBrush(data, hitInfo.point, hitInfo.normal);

                EditorUtility.SetDirty(data);
            }

            e.Use();
        }

        sceneView.Repaint();
    }

    private void ApplyPaintBrush(FoliagePaintData data, Vector3 hitPoint, Vector3 hitNormal)
    {
        var (right, fwd) = BuildBasis(hitNormal, brushRotation);
        Vector3 brushCenter = hitPoint + hitNormal * normalOffset;

        Color32 col32 = (Color32)paintColor;

        float jitter = sampleSpacing * 0.4f;

        float uc = Vector3.Dot(brushCenter, right);
        float vc = Vector3.Dot(brushCenter, fwd);
        float hc = Vector3.Dot(brushCenter, hitNormal);

        int gxMin = Mathf.FloorToInt((uc - brushRadius) / sampleSpacing);
        int gxMax = Mathf.FloorToInt((uc + brushRadius) / sampleSpacing);
        int gyMin = Mathf.FloorToInt((vc - brushRadius) / sampleSpacing);
        int gyMax = Mathf.FloorToInt((vc + brushRadius) / sampleSpacing);

        float eraseRadius = sampleSpacing * 0.6f;

        // Phase 1: collect hits and snapshot existing alpha before any erasure
        var cellHits = new List<(Vector3 position, Vector3 normal, byte addAlpha, byte existingAlpha)>();
        for (int gx = gxMin; gx <= gxMax; gx++)
        {
            for (int gy = gyMin; gy <= gyMax; gy++)
            {
                var cellRng = new System.Random(HashCell(gx, gy));
                float u = (gx + 0.5f) * sampleSpacing + (float)(cellRng.NextDouble() * 2 - 1) * jitter;
                float v = (gy + 0.5f) * sampleSpacing + (float)(cellRng.NextDouble() * 2 - 1) * jitter;

                float du = u - uc;
                float dv = v - vc;
                if (brushShape == BrushShape.Circle && du * du + dv * dv > brushRadius * brushRadius)
                {
                    continue;
                }
                if (brushShape == BrushShape.Square && (Mathf.Abs(du) > brushRadius || Mathf.Abs(dv) > brushRadius))
                {
                    continue;
                }

                float t = brushShape == BrushShape.Circle
                    ? Mathf.Sqrt(du * du + dv * dv) / brushRadius
                    : (Mathf.Abs(du) + Mathf.Abs(dv)) / (brushRadius * 2f);
                byte addAlpha = (byte)Mathf.RoundToInt(brushStrength * Mathf.Pow(1f - t, falloffPower) * 255f);
                if (addAlpha == 0)
                {
                    continue;
                }

                Vector3 candidate = right * u + fwd * v + hitNormal * hc;
                RaycastHit[] hits = Physics.RaycastAll(candidate, -hitNormal, raycastDistance, paintableLayers);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (!h.collider.gameObject.isStatic)
                    {
                        continue;
                    }
                    byte existing = data.GetAlphaAt(h.point, eraseRadius);
                    cellHits.Add((h.point, h.normal, addAlpha, existing));
                    break;
                }
            }
        }

        // Phase 2: erase near all hit positions
        foreach (var (pos, _, _, _) in cellHits)
        {
            data.EraseInShape(pos, right, fwd, eraseRadius, false);
        }

        // Phase 3: place with accumulated alpha (existing + new delta)
        foreach (var (pos, norm, addAlpha, existingAlpha) in cellHits)
        {
            byte finalAlpha = (byte)Mathf.Min(255, existingAlpha + addAlpha);
            data.Paint(new FoliageSample
            {
                position = pos,
                normal = norm,
                color = new Color32(col32.r, col32.g, col32.b, finalAlpha)
            }, sampleSpacing * 0.5f);
        }
    }

    private static int HashCell(int gx, int gy)
    {
        unchecked { return gx * 73856093 ^ gy * 19349663; }
    }


    private void ClearAllPaint(FoliagePaintData data)
    {
        data.Clear();
        EditorUtility.SetDirty(data);
        SceneView.RepaintAll();
    }

    private static void DrawBrushOutline(Vector3 center, Vector3 right, Vector3 fwd, float radius, BrushShape shape)
    {
        if (shape == BrushShape.Circle)
        {
            const int N = 64;
            var pts = new Vector3[N + 1];
            for (int i = 0; i <= N; i++)
            {
                float a = i * Mathf.PI * 2f / N;
                pts[i] = center + (right * Mathf.Cos(a) + fwd * Mathf.Sin(a)) * radius;
            }
            Handles.DrawAAPolyLine(20f, pts);
        }
        else
        {
            var pts = new Vector3[5];
            pts[0] = center + (right + fwd) * radius;
            pts[1] = center + (-right + fwd) * radius;
            pts[2] = center + (-right - fwd) * radius;
            pts[3] = center + (right - fwd) * radius;
            pts[4] = pts[0];
            Handles.DrawAAPolyLine(20f, pts);
        }
    }

    private static (Vector3 right, Vector3 fwd) BuildBasis(Vector3 normal, float rotationDeg)
    {
        Vector3 right = Vector3.Cross(normal, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(normal, Vector3.forward);
        right = Quaternion.AngleAxis(rotationDeg, normal) * right.normalized;
        Vector3 fwd = Vector3.Cross(right, normal).normalized;
        return (right, fwd);
    }

    private static FoliagePaintData GetOrCreatePaintData()
    {
        FoliagePaintData? existing = FindPaintData();
        if (existing != null)
        {
            return existing;
        }
        return new GameObject("FoliagePaintData").AddComponent<FoliagePaintData>();
    }

    private static FoliagePaintData? FindPaintData()
    {
        return FindAnyObjectByType<FoliagePaintData>();
    }

}
