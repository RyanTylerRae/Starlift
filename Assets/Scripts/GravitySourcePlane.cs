#nullable enable

using UnityEngine;

public class GravitySourcePlane : GravitySourceComponent
{
    public Collider triggerCollider;

    public Vector3 direction = new();

    public override void Update()
    {

    }

    public void Start()
    {
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"GravityComponent on {gameObject.name}: Assigned collider is not set as a trigger!");
        }
    }

    public override Vector3 GetClosestSurfacePoint(Vector3 point)
    {
        // Project point onto the plane
        Vector3 planeNormal = direction.normalized;
        Vector3 planePoint = transform.position;

        float distance = Vector3.Dot(point - planePoint, planeNormal);
        return point - planeNormal * distance;
    }

    public override Vector3 GetGravityVector(Vector3 point)
    {
        if (!isGravityEnabled)
        {
            return Vector3.zero;
        }

        return direction.normalized * defaultGravity * G_multiplier;
    }
}
