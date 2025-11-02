using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public static class PartPlacementTool
{
    private const float MOUSE_THRESHOLD = 100f; // Pixels from mouse to show green sphere
    private static bool isEnabled;
    private static GameObject selectedPartPrefab;
    private static Part selectedPart;
    private static PartData selectedPartData;
    private static Mesh selectedPartMesh;
    private static Vector3 selectedPartLocalScale;
    private static Quaternion selectedPartLocalRotation;

    // Preview state for placement
    private static bool hasValidPreview;
    private static Matrix4x4 previewTransform;
    private static int selectedConnectionPointIndex = 0;

    static PartPlacementTool()
    {
        // Always start disabled when Editor opens
        isEnabled = false;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/Starlift/Part Placement Editor")]
    private static void TogglePartPlacementTool()
    {
        isEnabled = !isEnabled;
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Starlift/Part Placement Editor", true)]
    private static bool TogglePartPlacementToolValidate()
    {
        Menu.SetChecked("Tools/Starlift/Part Placement Editor", isEnabled);
        return true;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isEnabled || Application.isPlaying)
        {
            return;
        }

        UpdateSelectedPartPrefab();

        // Draw all connection points in view
        DrawAllConnectionPoints(sceneView);

        // Handle input for placing parts
        HandleInput();

        // Force scene view to repaint
        sceneView.Repaint();
    }

    private static void HandleInput()
    {
        Event e = Event.current;

        // Left-click to place part when preview is showing
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (hasValidPreview && selectedPartPrefab != null)
            {
                PlacePartInstance();
                e.Use(); // Consume event to prevent selection change
            }
        }

        // Scroll wheel to cycle through connection points
        if (e.type == EventType.ScrollWheel && selectedPartData != null && selectedPartData.connectionPoints != null)
        {
            int connectionPointCount = selectedPartData.connectionPoints.Count;
            if (connectionPointCount > 0)
            {
                // Scroll up = negative delta, scroll down = positive delta
                if (e.delta.y > 0)
                {
                    selectedConnectionPointIndex++;
                }
                else if (e.delta.y < 0)
                {
                    selectedConnectionPointIndex--;
                }

                // Wrap around
                if (selectedConnectionPointIndex < 0)
                {
                    selectedConnectionPointIndex = connectionPointCount - 1;
                }
                else if (selectedConnectionPointIndex >= connectionPointCount)
                {
                    selectedConnectionPointIndex = 0;
                }

                e.Use(); // Consume event to prevent camera zoom
            }
        }
    }

    private static void PlacePartInstance()
    {
        // Extract position and rotation from the preview transform
        Vector3 position = previewTransform.GetColumn(3);
        Quaternion rotation = previewTransform.rotation;

        // Instantiate the prefab
        GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPartPrefab);
        newInstance.transform.position = position;
        newInstance.transform.rotation = rotation;

        // Register with undo system
        Undo.RegisterCreatedObjectUndo(newInstance, "Place Part");

        // Don't select the new object - keep the prefab selected so user can continue placing
        Debug.Log($"Placed {selectedPartPrefab.name} at {position}");
    }

    private static void UpdateSelectedPartPrefab()
    {
        GameObject currentSelection = Selection.activeObject as GameObject;
        if (currentSelection == selectedPartPrefab)
        {
            return;
        }

        // Clear cache
        selectedPartPrefab = null;
        selectedPart = null;
        selectedPartData = null;
        selectedPartMesh = null;
        selectedPartLocalScale = Vector3.one;
        selectedPartLocalRotation = Quaternion.identity;

        if (currentSelection == null)
        {
            return;
        }

        if (!currentSelection.TryGetComponent<Part>(out Part part))
        {
            return;
        }

        selectedPartPrefab = currentSelection;
        selectedPart = part;
        selectedPart.LoadPartData();
        selectedPartData = selectedPart.GetPartData();

        // Reset connection point index when changing parts
        selectedConnectionPointIndex = 0;

        // cache transform data
        selectedPartLocalScale = currentSelection.transform.localScale;
        selectedPartLocalRotation = currentSelection.transform.localRotation;

        MeshFilter meshFilter = currentSelection.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            selectedPartMesh = meshFilter.sharedMesh;
        }
    }

    private static void DrawAllConnectionPoints(SceneView sceneView)
    {
        Vector2 mousePos = Event.current.mousePosition;

        // Reset preview state
        hasValidPreview = false;

        Part[] allParts = Object.FindObjectsByType<Part>(sortMode: FindObjectsSortMode.None);
        if (allParts.Count() == 0)
        {
            return;
        }

        Vector3? closestWorldPos = null;
        Quaternion closestWorldRotation = Quaternion.identity;
        float closestDistance = float.MaxValue;

        // draw gizmos for all parts and find the closest connection point to mouse cursor
        foreach (Part part in allParts)
        {
            // Force load part data if not already loaded
            PartData partData = part.GetPartData();
            if (partData == null)
            {
                part.LoadPartData();
                partData = part.GetPartData();
            }

            if (partData == null || partData.connectionPoints == null)
            {
                continue;
            }

            for (int i = 0; i < partData.connectionPoints.Count; i++)
            {
                var connectionPoint = partData.connectionPoints[i];
                Vector3 worldPos = part.transform.TransformPoint(connectionPoint.localPos);

                if (IsInViewFrustum(worldPos, sceneView.camera))
                {
                    // draw the connection point gizmo
                    Quaternion localRotation = Quaternion.Euler(connectionPoint.localEulerRot);
                    Quaternion worldRotation = part.transform.rotation * localRotation;

                    Part.DrawConnectionPointGizmo(worldPos, worldRotation, connectionPoint.orientation, 0.2f,
                        (color) => Handles.color = color,
                        (start, end) => Handles.DrawLine(start, end)
                    );

                    // Draw connection point label
                    string label = GetConnectionPointLabel(i);
                    GUIStyle labelStyle = new GUIStyle();
                    labelStyle.normal.textColor = Color.white;
                    labelStyle.fontSize = 12;
                    labelStyle.fontStyle = FontStyle.Bold;
                    Handles.Label(worldPos, label, labelStyle);

                    Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
                    float distanceToMouse = Vector2.Distance(mousePos, screenPos);

                    if (distanceToMouse < closestDistance)
                    {
                        closestDistance = distanceToMouse;
                        closestWorldPos = worldPos;
                        closestWorldRotation = worldRotation;
                    }
                }
            }
        }

        if (closestWorldPos.HasValue && closestDistance < MOUSE_THRESHOLD)
        {
            Handles.color = Color.green;
            Handles.SphereHandleCap(0, closestWorldPos.Value, Quaternion.identity, 0.15f, EventType.Repaint);

            // render the selected Part prefab wireframe at the connection point
            if (selectedPartMesh != null && selectedPartData != null && selectedPartData.connectionPoints != null && selectedPartData.connectionPoints.Count > 0)
            {
                // Clamp the selected index in case the connection point count changed
                if (selectedConnectionPointIndex >= selectedPartData.connectionPoints.Count)
                {
                    selectedConnectionPointIndex = 0;
                }

                ConnectionPoint selectedCP = selectedPartData.connectionPoints[selectedConnectionPointIndex];

                // calculate the aligned transform
                Matrix4x4 matrix = CalculateAlignedTransform(
                    closestWorldPos.Value,
                    closestWorldRotation,
                    selectedCP,
                    selectedPartLocalRotation,
                    selectedPartLocalScale
                );

                DrawWireframeMesh(selectedPartMesh, matrix, Color.green);

                // Draw labels for all connection points on the preview
                for (int i = 0; i < selectedPartData.connectionPoints.Count; i++)
                {
                    var cp = selectedPartData.connectionPoints[i];
                    Vector3 localPos = cp.localPos;
                    Vector3 worldPos = matrix.MultiplyPoint3x4(localPos);

                    string label = GetConnectionPointLabel(i);
                    GUIStyle labelStyle = new GUIStyle();
                    labelStyle.normal.textColor = Color.green;
                    labelStyle.fontSize = 12;
                    labelStyle.fontStyle = FontStyle.Bold;
                    Handles.Label(worldPos, label, labelStyle);
                }

                // Store the preview state for placement
                hasValidPreview = true;
                previewTransform = matrix;
            }
        }
    }

    private static Matrix4x4 CalculateAlignedTransform(
        Vector3 targetWorldPos,
        Quaternion targetWorldRotation,
        ConnectionPoint sourceConnectionPoint,
        Quaternion partLocalRotation,
        Vector3 partLocalScale)
    {
        // Goal: Align source connection point with target connection point
        // - Forwards should be opposite (facing each other)
        // - Orientations should be opposite (0° to 180°, 90° to 270°, etc.)

        // Step 1: Get source connection point's local rotation
        Quaternion sourceLocalRotation = Quaternion.Euler(sourceConnectionPoint.localEulerRot);

        // Step 2: Flip 180° so connection points face each other
        // Rotate around an axis perpendicular to forward
        Quaternion flip180 = Quaternion.AngleAxis(180f, Vector3.right);

        // Step 3: Account for opposite orientations
        // Since we want orientations to be opposite, rotate 180° around forward axis
        Quaternion orientationFlip = Quaternion.AngleAxis(180f, Vector3.forward);

        // Step 4: Calculate part's world rotation
        // Start with target rotation, apply flips, then remove source's local rotation, then apply part's local rotation
        Quaternion partWorldRotation = targetWorldRotation * flip180 * orientationFlip * Quaternion.Inverse(sourceLocalRotation) * partLocalRotation;

        // Step 5: Calculate where the source connection point would end up in world space
        Vector3 sourceOffsetWorld = partWorldRotation * Vector3.Scale(sourceConnectionPoint.localPos, partLocalScale);

        // Step 6: Position the part so the source connection point ends up at target position
        Vector3 partWorldPosition = targetWorldPos - sourceOffsetWorld;

        return Matrix4x4.TRS(partWorldPosition, partWorldRotation, partLocalScale);
    }

    private static void DrawWireframeMesh(Mesh mesh, Matrix4x4 matrix, Color color)
    {
        Handles.color = color;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Draw each triangle edge
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get the three vertices of the triangle
            Vector3 v0 = matrix.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 v1 = matrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 v2 = matrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);

            // Draw the three edges
            Handles.DrawLine(v0, v1);
            Handles.DrawLine(v1, v2);
            Handles.DrawLine(v2, v0);
        }
    }

    private static bool IsInViewFrustum(Vector3 worldPosition, Camera camera)
    {
        if (camera == null)
        {
            return false;
        }

        // Convert world position to viewport point
        Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);

        // Check if point is within viewport bounds and in front of camera
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }

    private static string GetConnectionPointLabel(int index)
    {
        // Generate labels: A, B, C, ... Z, AA, AB, etc.
        if (index < 26)
        {
            return ((char)('A' + index)).ToString();
        }
        else
        {
            int firstChar = (index / 26) - 1;
            int secondChar = index % 26;
            return ((char)('A' + firstChar)).ToString() + ((char)('A' + secondChar)).ToString();
        }
    }
}
