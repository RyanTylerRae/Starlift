#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class ConstantForceComponent : MonoBehaviour
{
    [Header("Force Settings")]
    public Vector3 relativeForce = Vector3.zero;
    public ForceMode forceMode = ForceMode.Force;

    [Header("Visualization")]
    public float arrowLength = 1.0f;
    public Color arrowColor = Color.cyan;

    private List<Rigidbody> trackedRigidbodies = new List<Rigidbody>();

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !trackedRigidbodies.Contains(rb))
        {
            trackedRigidbodies.Add(rb);
            Debug.Log($"ConstantForceComponent: Added {other.gameObject.name} to tracked rigidbodies");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            trackedRigidbodies.Remove(rb);
            Debug.Log($"ConstantForceComponent: Removed {other.gameObject.name} from tracked rigidbodies");
        }
    }

    private void FixedUpdate()
    {
        // Clean up null references (destroyed objects)
        trackedRigidbodies.RemoveAll(rb => rb == null);

        // Transform the relative force to world space based on this component's orientation
        Vector3 worldForce = transform.TransformDirection(relativeForce);

        // Apply force to all tracked rigidbodies
        foreach (Rigidbody rb in trackedRigidbodies)
        {
            if (rb != null)
            {
                rb.AddForce(worldForce, forceMode);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw arrow showing force direction in local space
        Gizmos.color = arrowColor;

        Vector3 worldForceDirection = transform.TransformDirection(relativeForce.normalized);
        Vector3 start = transform.position;
        Vector3 end = start + worldForceDirection * arrowLength;

        // Draw main line
        Gizmos.DrawLine(start, end);

        // Draw arrowhead
        Vector3 right = Quaternion.LookRotation(worldForceDirection) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(worldForceDirection) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;

        Gizmos.DrawLine(end, end + right * 0.25f * arrowLength);
        Gizmos.DrawLine(end, end + left * 0.25f * arrowLength);
    }
}
