using UnityEditor;
using UnityEngine;

public class PrefabGridPlacer : EditorWindow
{
    GameObject _prefab;
    Transform _parent;
    int _columns = 5;
    int _rows = 5;
    float _xOffset = 5f;
    float _yOffset = 5f;
    float _minXRotation = -10f;
    float _maxXRotation = 10f;
    int _seed = 0;
    float _pivotZ = 100f;

    [MenuItem("Tools/Prefab Grid Placer")]
    static void ShowWindow() => GetWindow<PrefabGridPlacer>("Prefab Grid Placer");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
        _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
        _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        _columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _columns));
        _rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _rows));
        _xOffset = EditorGUILayout.FloatField("X Offset", _xOffset);
        _yOffset = EditorGUILayout.FloatField("Y Offset", _yOffset);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("X Rotation", EditorStyles.boldLabel);
        _minXRotation = EditorGUILayout.FloatField("Min", _minXRotation);
        _maxXRotation = EditorGUILayout.FloatField("Max", _maxXRotation);
        _seed = EditorGUILayout.IntField("Random Seed", _seed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Curvature", EditorStyles.boldLabel);
        _pivotZ = EditorGUILayout.FloatField("Pivot Z Distance", _pivotZ);
        EditorGUILayout.HelpBox("Distance along Z to the center of curvature. Larger = flatter, smaller = tighter curve.", MessageType.None);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(_prefab == null || Mathf.Approximately(_pivotZ, 0f)))
        {
            if (GUILayout.Button("Generate"))
                Generate();
        }
    }

    void Generate()
    {
        Undo.SetCurrentGroupName("Generate Prefab Grid");
        int undoGroup = Undo.GetCurrentGroup();

        Random.InitState(_seed);

        float halfWidth = (_columns - 1) * _xOffset * 0.5f;
        float halfHeight = (_rows - 1) * _yOffset * 0.5f;

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                float flatX = c * _xOffset - halfWidth;
                float flatY = r * _yOffset - halfHeight;

                // Wrap the X axis around a cylinder whose center is at (0, y, pivotZ).
                // theta is the arc angle from the grid center to this column.
                float theta = Mathf.Atan2(flatX, _pivotZ);
                float curvedX = Mathf.Sin(theta) * _pivotZ;
                float curvedZ = _pivotZ * (1f - Mathf.Cos(theta));

                Vector3 position = new Vector3(curvedX, flatY, curvedZ);

                // Tilt first (X), then rotate to follow the curve (Y).
                // Composing separately ensures X is applied in the tile's pre-curve local space.
                float xRot = Random.Range(_minXRotation, _maxXRotation);
                Quaternion tilt = Quaternion.AngleAxis(xRot, Vector3.right);
                Quaternion curve = Quaternion.AngleAxis(-theta * Mathf.Rad2Deg, Vector3.up);
                Quaternion rotation = curve * tilt;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, _parent);
                Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
                instance.transform.localPosition = position;
                instance.transform.localRotation = rotation;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
    }
}
