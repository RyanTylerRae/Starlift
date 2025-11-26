#nullable enable

using UnityEngine;

/// <summary>
/// Marker component - attach to a camera to enable fisheye distortion effect
/// </summary>
[RequireComponent(typeof(Camera))]
public class FisheyeCamera : MonoBehaviour
{
    [Range(0f, 1f)]
    public float distortionStrength = 0.3f;
}
