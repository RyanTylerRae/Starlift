#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class FoliagePainterWindow : EditorWindow
{
    private enum BrushMode { Hard, Soft }

    // Paint mode
    private bool paintingEnabled = false;

    // Brush
    private Color paintColor = Color.green;
    private float brushRadius = 1f;
    private float sampleSpacing = 0.2f;
    private BrushMode brushMode = BrushMode.Soft;
    private bool eraseMode = false;
    private LayerMask paintableLayers = ~0;

    // Layers
    private List<FoliageLayer?> layers = new();
    private Vector2 layerScroll;

    // Preview
    private List<FoliageDistributor.FoliageCandidate> previewPoints = new();
    private bool showPreview = false;

    [MenuItem("Tools/Starlift/Foliage Painter")]
    private static void Open() => GetWindow<FoliagePainterWindow>("Foliage Painter");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        showPreview = false;
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
        paintColor = EditorGUILayout.ColorField("Paint Color", paintColor);
        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.1f, 20f);
        sampleSpacing = EditorGUILayout.Slider("Sample Spacing", sampleSpacing, 0.05f, 2f);
        brushMode = (BrushMode)EditorGUILayout.EnumPopup("Brush Mode", brushMode);
        eraseMode = EditorGUILayout.Toggle("Erase Mode", eraseMode);
        paintableLayers = (LayerMask)EditorGUILayout.MaskField(
            "Paintable Layers",
            InternalEditorUtility.LayerMaskToConcatenatedLayersMask(paintableLayers),
            InternalEditorUtility.layers);
        paintableLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(paintableLayers);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Foliage Layers", EditorStyles.boldLabel);

        layerScroll = EditorGUILayout.BeginScrollView(layerScroll, GUILayout.Height(200));
        int removeIndex = -1;
        for (int i = 0; i < layers.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            layers[i] = (FoliageLayer?)EditorGUILayout.ObjectField(
                layers[i], typeof(FoliageLayer), false, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("−", GUILayout.Width(22)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();

            if (layers[i] != null)
                DrawLayerRow(layers[i]!);

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        if (removeIndex >= 0)
            layers.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Add Layer"))
            layers.Add(null);

        EditorGUILayout.Space();

        if (GUILayout.Button("Preview Distribution"))
            RefreshPreview();

        showPreview = EditorGUILayout.Toggle("Show Preview Gizmos", showPreview);

        EditorGUILayout.Space();

        FoliagePaintData? data = FindPaintData();
        using (new EditorGUI.DisabledScope(data == null))
        {
            if (GUILayout.Button("Clear All Paint"))
                ClearAllPaint(data!);
        }
    }

    private void DrawLayerRow(FoliageLayer layer)
    {
        EditorGUILayout.BeginHorizontal();

        Color32 c = layer.targetColor;
        Rect swatchRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(16));
        EditorGUI.DrawRect(swatchRect, new Color(c.r / 255f, c.g / 255f, c.b / 255f));

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
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying) return;

        // Always draw existing samples regardless of mode
        FoliagePaintData? existingData = FindPaintData();
        if (existingData != null && Event.current.type == EventType.Repaint)
            DrawSamples(existingData, sceneView.camera);

        if (!paintingEnabled) return;

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        bool didHit = Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, paintableLayers);
        bool isValidTarget = didHit && hitInfo.collider.gameObject.isStatic;

        // Brush disc
        if (didHit && e.type == EventType.Repaint)
        {
            Handles.color = !isValidTarget
                ? new Color(1f, 0.5f, 0f, 0.8f)
                : eraseMode
                    ? new Color(1f, 0.2f, 0.2f, 0.8f)
                    : new Color(paintColor.r, paintColor.g, paintColor.b, 0.8f);
            Handles.DrawWireDisc(hitInfo.point, hitInfo.normal, brushRadius);
        }

        bool isPaintEvent = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                            && e.button == 0 && !e.alt;

        if (isPaintEvent && isValidTarget)
        {
            FoliagePaintData data = GetOrCreatePaintData();

            if (eraseMode)
            {
                data.Erase(hitInfo.point, brushRadius, paintColor.a, brushMode == BrushMode.Soft);
            }
            else
            {
                ApplyPaintBrush(data, hitInfo.point, hitInfo.normal);
            }

            EditorUtility.SetDirty(data);
            showPreview = false;
            e.Use();
        }

        // Preview distribution gizmos
        if (showPreview && e.type == EventType.Repaint)
        {
            Handles.color = Color.yellow;
            foreach (var p in previewPoints)
            {
                if (IsInFrustum(p.position, sceneView.camera))
                    Handles.DotHandleCap(0, p.position, Quaternion.identity, 0.06f, EventType.Repaint);
            }
        }

        sceneView.Repaint();
    }

    private void ApplyPaintBrush(FoliagePaintData data, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Build a coordinate frame in the plane perpendicular to the hit normal
        Vector3 right = Vector3.Cross(hitNormal, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(hitNormal, Vector3.forward);
        right.Normalize();
        Vector3 fwd = Vector3.Cross(right, hitNormal).normalized;

        Color32 col32 = (Color32)paintColor;
        int steps = Mathf.CeilToInt(brushRadius * 2f / sampleSpacing);
        float rayOffset = 0.5f;

        for (int gx = 0; gx <= steps; gx++)
        {
            for (int gy = 0; gy <= steps; gy++)
            {
                float u = (gx / (float)steps - 0.5f) * brushRadius * 2f;
                float v = (gy / (float)steps - 0.5f) * brushRadius * 2f;
                float dist = Mathf.Sqrt(u * u + v * v);
                if (dist > brushRadius) continue;

                Vector3 candidate = hitPoint + right * u + fwd * v;
                Vector3 rayOrigin = candidate + hitNormal * rayOffset;

                if (!Physics.Raycast(rayOrigin, -hitNormal, out RaycastHit hit, rayOffset * 3f, paintableLayers))
                    continue;
                if (!hit.collider.gameObject.isStatic) continue;

                float normalizedDist = dist / brushRadius;
                byte alpha;
                if (brushMode == BrushMode.Hard)
                {
                    alpha = col32.a;
                }
                else
                {
                    float t = normalizedDist;
                    float falloff = 1f - (3f * t * t - 2f * t * t * t);
                    alpha = (byte)Mathf.RoundToInt(col32.a * falloff);
                }

                if (alpha == 0) continue;

                data.Paint(new FoliageSample
                {
                    position = hit.point,
                    normal = hit.normal,
                    color = new Color32(col32.r, col32.g, col32.b, alpha)
                }, sampleSpacing);
            }
        }
    }

    private void DrawSamples(FoliagePaintData data, Camera camera)
    {
        float discRadius = sampleSpacing * 0.45f;
        foreach (var s in data.GetAll())
        {
            if (!IsInFrustum(s.position, camera)) continue;
            Color32 c = s.color;
            Handles.color = new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f * 0.85f + 0.15f);
            Handles.DrawSolidDisc(s.position, s.normal, discRadius);
        }
    }

    private void RefreshPreview()
    {
        previewPoints.Clear();
        FoliagePaintData? data = FindPaintData();
        if (data == null) return;

        var candidates = FoliageDistributor.SampleCandidates(data);
        foreach (var layer in layers)
        {
            if (layer == null || layer.prefab == null) continue;
            previewPoints.AddRange(FoliageDistributor.PoissonThin(candidates, layer));
        }

        showPreview = true;
        SceneView.RepaintAll();
    }

    private void ClearAllPaint(FoliagePaintData data)
    {
        data.Clear();
        EditorUtility.SetDirty(data);
        previewPoints.Clear();
        SceneView.RepaintAll();
    }

    private static FoliagePaintData GetOrCreatePaintData()
    {
        FoliagePaintData? existing = FindPaintData();
        if (existing != null) return existing;
        return new GameObject("FoliagePaintData").AddComponent<FoliagePaintData>();
    }

    private static FoliagePaintData? FindPaintData() =>
        FindAnyObjectByType<FoliagePaintData>();

    private static bool IsInFrustum(Vector3 worldPos, Camera camera)
    {
        if (camera == null) return false;
        Vector3 vp = camera.WorldToViewportPoint(worldPos);
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f;
    }
}
