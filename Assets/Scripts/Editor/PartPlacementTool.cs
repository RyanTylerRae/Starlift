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

    private static bool hasValidPreview;
    private static Matrix4x4 previewTransform;
    private static int selectedConnectionPointIndex = 0;

    static PartPlacementTool()
    {
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
        DrawAllConnectionPoints(sceneView);
        HandleInput();
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
        Vector3 position = previewTransform.GetColumn(3);
        Quaternion rotation = previewTransform.rotation;

        GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPartPrefab);
        newInstance.transform.position = position;
        newInstance.transform.rotation = rotation;

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
        selectedConnectionPointIndex = 0;

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
        hasValidPreview = false;

        Part[] allParts = Object.FindObjectsByType<Part>(sortMode: FindObjectsSortMode.None);
        if (allParts.Count() == 0)
        {
            return;
        }

        bool foundClosest = false;
        Vector3 closestWorldPos = Vector3.zero;
        Quaternion closestWorldRotation = Quaternion.identity;
        ConnectionPoint closestConnectionPoint = new ConnectionPoint();
        float closestDistance = float.MaxValue;

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
                    Quaternion localRotation = Quaternion.Euler(connectionPoint.localEulerRot);
                    Quaternion worldRotation = part.transform.rotation * localRotation;

                    Part.DrawConnectionPointGizmo(worldPos, worldRotation, connectionPoint.orientation, 0.2f,
                        (color) => Handles.color = color,
                        (start, end) => Handles.DrawLine(start, end)
                    );

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
                        closestConnectionPoint = connectionPoint;
                        foundClosest = true;
                    }
                }
            }
        }

        if (foundClosest && closestDistance < MOUSE_THRESHOLD)
        {
            Handles.color = Color.green;
            Handles.SphereHandleCap(0, closestWorldPos, Quaternion.identity, 0.15f, EventType.Repaint);

            if (selectedPartMesh != null && selectedPartData != null && selectedPartData.connectionPoints != null && selectedPartData.connectionPoints.Count > 0)
            {
                if (selectedConnectionPointIndex >= selectedPartData.connectionPoints.Count)
                {
                    selectedConnectionPointIndex = 0;
                }

                ConnectionPoint selectedCP = selectedPartData.connectionPoints[selectedConnectionPointIndex];

                Matrix4x4 matrix = CalculateAlignedTransform(
                    closestWorldPos,
                    closestWorldRotation,
                    closestConnectionPoint,
                    selectedCP,
                    selectedPartLocalRotation
                );

                DrawWireframeMesh(selectedPartMesh, matrix, Color.green);

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

                hasValidPreview = true;
                previewTransform = matrix;
            }
        }
    }

    private static Matrix4x4 CalculateAlignedTransform(
        Vector3 targetWorldPos,
        Quaternion targetWorldRotation,
        ConnectionPoint targetConnectionPoint,
        ConnectionPoint sourceConnectionPoint,
        Quaternion partLocalRotation)
    {
        // Start with target connection point transform, apply its orientation,
        // flip the forward vector, then apply inverse transformations (orientation, rotation, position)
        // to find the new actor's world transform.

        Vector3 currentPos = targetWorldPos;
        Quaternion currentRot = targetWorldRotation;

        // Apply target's orientation (rotation around forward axis)
        Quaternion targetOrientation = Quaternion.Euler(0, 0, targetConnectionPoint.orientation);
        currentRot = currentRot * targetOrientation;

        // Flip to opposite forward direction
        Vector3 targetForward = currentRot * Vector3.forward;
        Vector3 targetUp = currentRot * Vector3.up;
        currentRot = Quaternion.LookRotation(-targetForward, targetUp);

        // Apply inverse of source orientation
        Quaternion sourceOrientation = Quaternion.Euler(0, 0, sourceConnectionPoint.orientation);
        currentRot = currentRot * Quaternion.Inverse(sourceOrientation);

        // Apply inverse of source connection point's local rotation
        Quaternion sourceLocalRotation = Quaternion.Euler(sourceConnectionPoint.localEulerRot);
        currentRot = currentRot * Quaternion.Inverse(sourceLocalRotation);

        // Apply inverse of part's local rotation
        currentRot = currentRot * Quaternion.Inverse(partLocalRotation);

        // Apply inverse position offset
        Vector3 offsetInWorld = currentRot * sourceConnectionPoint.localPos;
        currentPos = currentPos - offsetInWorld;

        return Matrix4x4.TRS(currentPos, currentRot, Vector3.one);
    }

    private static void DrawWireframeMesh(Mesh mesh, Matrix4x4 matrix, Color color)
    {
        Handles.color = color;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = matrix.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 v1 = matrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 v2 = matrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);

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

        Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);

        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }

    private static string GetConnectionPointLabel(int index)
    {
        Debug.Assert(index < 26);
        return ((char)('A' + index)).ToString();
    }
}
