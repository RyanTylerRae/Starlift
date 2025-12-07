#nullable enable

using UnityEngine;

/// <summary>
/// Marker component - attach to a camera to enable crt effect
/// </summary>
[RequireComponent(typeof(Camera))]
public class CRTCamera : MonoBehaviour
{
    [Range(0f, 1f)]
    public float scanlineIntensity = 0.5f;
    [Range(0f, 500f)]
    public float scanlineCount = 500.0f;
    [Range(0f, 1f)]
    public float vignette = 0.3f;
    [Range(0f, 0.01f)]
    public float chromaticAbberation = 0.002f;
    [Range(0.5f, 1.5f)]
    public float brightness = 1.0f;
}
