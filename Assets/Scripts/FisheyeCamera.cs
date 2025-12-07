#nullable enable

using UnityEngine;

/// <summary>
/// Marker component - attach to a camera to enable fisheye distortion effect
/// </summary>
[RequireComponent(typeof(Camera))]
public class FisheyeCamera : MonoBehaviour
{
    [Range(-1f, 1f)]
    public float horizontalDistortionStrength = 0.3f;
    [Range(-1f, 1f)]
    public float verticalDistortionStrength = 0.3f;
}
