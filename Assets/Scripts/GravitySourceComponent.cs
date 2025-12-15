#nullable enable

using UnityEngine;

public class GravitySourceComponent : MonoBehaviour
{
    public Collider triggerCollider;

    public Collider meshCollider;

    public enum GravityMode
    {
        Plane,
        Point,
        Mesh
    }

    [Header("Gravity Settings")]
    public GravityMode gravityMode;
    public Vector3 direction = new();
    public float G_multiplier = 1.0f;

    public float maxDistanceToSurface = 1.0f;

    // @todo trae - move global gravity somewhere else
    public float defaultGravity = -10.0f;

    public bool isGravityEnabled = true;

    void Start()
    {
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"GravityComponent on {gameObject.name}: Assigned collider is not set as a trigger!");
        }

        direction = direction.normalized;

        if (gravityMode == GravityMode.Plane && direction.sqrMagnitude < 0.01)
        {
            Debug.LogWarning("GravityComponent on {gameObject.name}: A valid direction is required for plane gravity.");
        }
    }

    public Vector3 GetGravityVector(Vector3 point)
    {
        if (!isGravityEnabled)
        {
            return Vector3.zero;
        }

        if (gravityMode == GravityMode.Plane)
        {
            return direction * defaultGravity * G_multiplier;
        }
        else if (gravityMode == GravityMode.Point)
        {
            return (transform.position - point).normalized * defaultGravity * G_multiplier;
        }
        else if (gravityMode == GravityMode.Mesh)
        {
            Vector3 closestPoint = meshCollider.ClosestPoint(point);
            Vector3 normal = point - closestPoint;

            if (normal.sqrMagnitude < maxDistanceToSurface * maxDistanceToSurface)
            {
                return normal.normalized * defaultGravity * G_multiplier;
            }
        }

        return Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"GravityComponent on {gameObject.name}: {other.gameObject.name} entered trigger");

        GravityController gravityController = other.gameObject.GetComponent<GravityController>();
        if (gravityController != null)
        {
            gravityController.AddGravitySource(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"GravityComponent on {gameObject.name}: {other.gameObject.name} exited trigger");

        GravityController gravityController = other.gameObject.GetComponent<GravityController>();
        if (gravityController != null)
        {
            gravityController.RemoveGravitySource(this);
        }
    }
}
