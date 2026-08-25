#nullable enable

using UnityEngine;

// Marker component - attach to a camera to enable distance fog that fades to black near the far plane
[RequireComponent(typeof(Camera))]
public class FogCamera : MonoBehaviour
{
    public Color fogColor = Color.black;
    public float fogStartDistance = 1000f;
    [Range(0.1f, 8f)]
    public float fogPower = 1f;
}
