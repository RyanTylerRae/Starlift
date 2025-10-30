using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PartData
{
    public string partName;
    public float mass;
    public float durability;
}

public class Part : MonoBehaviour
{
    // Predefined path to the parts folder in StreamingAssets
    private const string PARTS_FOLDER_PATH = "Parts";

    [SerializeField]
    private string partName;

    private PartData partData;

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
            partData = JsonUtility.FromJson<PartData>(jsonContent);
            Debug.Log($"Loaded part data from: {fullPath}");
        }
        else
        {
            Debug.LogError($"Could not find JSON file at: {fullPath}");
        }
    }

    public PartData GetPartData()
    {
        return partData;
    }
}
