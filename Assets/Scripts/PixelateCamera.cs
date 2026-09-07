#nullable enable

using UnityEngine;

/// <summary>
/// Marker component - attach to a camera to enable the pixelate effect
/// </summary>
[RequireComponent(typeof(Camera))]
public class PixelateCamera : MonoBehaviour
{
    [Range(16f, 1024f)]
    public float pixelsPerScreenHeight;
}
