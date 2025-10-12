#nullable enable

using NUnit.Framework;
using UnityEngine;

public class GravitySourceComponent : MonoBehaviour
{
    [Header("Collider")]
    public Collider triggerCollider;

    public enum GravityMode
    {
        Plane,
        Point
    }

    [Header("Gravity Settings")]
    public GravityMode gravityMode;
    public Vector3 direction = new();
    public float G_multiplier = 1.0f;

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

        // add self to all overlapping GravityControllers at initialization
        Collider[] overlappingColliders = Physics.OverlapBox(
            triggerCollider.bounds.center,
            triggerCollider.bounds.extents,
            triggerCollider.transform.rotation
        );

        // foreach (Collider collider in overlappingColliders)
        // {
        //     GravityController gravityController = collider.GetComponent<GravityController>();
        //     if (gravityController != null)
        //     {
        //         gravityController.AddGravitySource(this);
        //         Debug.Log($"GravitySourceComponent on {gameObject.name}: Added to {collider.gameObject.name} on Start");
        //     }
        // }
    }

    public Vector3 GetGravityVector(GameObject gameObject)
    {
        if (!isGravityEnabled)
        {
            return Vector3.zero;
        }

        if (gravityMode == GravityMode.Plane)
        {
            return direction * defaultGravity * G_multiplier;
        }
        else
        {
            return (transform.position - gameObject.transform.position).normalized * defaultGravity * G_multiplier;
        }
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
