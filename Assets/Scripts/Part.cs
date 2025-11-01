#nullable enable

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class PartData
{
    public List<ConnectionPoint> connectionPoints = new();
}

public class ConnectionPoint
{
    public Vector3 localPos;
    public Vector3 localEulerRot;
    public float orientation;
}

public class Part : MonoBehaviour
{
    // Predefined path to the parts folder in StreamingAssets
    private const string PARTS_FOLDER_PATH = "Parts";

    [SerializeField]
    private string partName;

    private PartData? partData = null;

    private void Start()
    {
        LoadPartData();
    }

    public void LoadPartData()
    {
        if (string.IsNullOrEmpty(partName))
        {
            Debug.LogError("Part name is not set.");
            return;
        }

        // Construct the full file path: StreamingAssets/Parts/{partName}.json
        string fullPath = Path.Combine(Application.streamingAssetsPath, PARTS_FOLDER_PATH, partName + ".json");

        if (File.Exists(fullPath))
        {
            string jsonContent = File.ReadAllText(fullPath);
            partData = JsonConvert.DeserializeObject<PartData>(jsonContent);
            Debug.Log($"Loaded part data from: {fullPath}");
        }
        else
        {
            Debug.LogError($"Could not find JSON file at: {fullPath}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (partData == null)
        {
            LoadPartData();
        }

        if (partData == null || partData.connectionPoints.Count == 0)
        {
            return;
        }

        foreach (var connectionPoint in partData.connectionPoints)
        {
            // Convert local position to world position
            Vector3 worldPos = transform.TransformPoint(connectionPoint.localPos);

            // Convert local euler rotation to world rotation
            Quaternion localRotation = Quaternion.Euler(connectionPoint.localEulerRot);
            Quaternion worldRotation = transform.rotation * localRotation;

            // Draw a square facing away from the connection point
            DrawSquare(worldPos, worldRotation, connectionPoint.orientation, 0.2f);
        }
    }

    private void DrawSquare(Vector3 position, Quaternion rotation, float orientation, float size)
    {
        // Calculate the four corners of the square
        Vector3 right = rotation * Vector3.right * size;
        Vector3 up = rotation * Vector3.up * size;

        Vector3 topLeft = position - right + up;
        Vector3 topRight = position + right + up;
        Vector3 bottomRight = position + right - up;
        Vector3 bottomLeft = position - right - up;

        // Draw the square
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Draw forward direction indicator (plane normal)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + rotation * Vector3.forward * size);

        // Draw orientation indicator
        // Start with local up direction, add slight forward offset, then rotate by orientation around forward axis
        Vector3 localDirection = Vector3.up * size + Vector3.forward * (size * 0.5f);
        Quaternion orientationRotation = Quaternion.AngleAxis(orientation, Vector3.forward);
        Vector3 rotatedDirection = orientationRotation * localDirection;
        Vector3 worldDirection = rotation * rotatedDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, position + worldDirection);
    }
}
