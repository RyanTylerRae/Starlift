#nullable enable

using UnityEngine;

[System.Serializable]
public struct FoliageSample
{
    public Vector3 position;
    public Vector3 normal;
    public Color32 color; // RGB = layer-match color, A = accumulated density (0-255)
}
