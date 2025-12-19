#nullable enable

using UnityEngine;

public class GravitySourcePoint : GravitySourceComponent
{
    public Collider triggerCollider;

    public void Start()
    {
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"GravityComponent on {gameObject.name}: Assigned collider is not set as a trigger!");
        }
    }

    public override void Update()
    {

    }

    public override Vector3 GetClosestSurfacePoint(Vector3 point)
    {
        // For a point gravity source, the "surface" is the point itself
        return transform.position;
    }

    public override Vector3 GetGravityVector(Vector3 point)
    {
        if (!isGravityEnabled)
        {
            return Vector3.zero;
        }

        return (transform.position - point).normalized * defaultGravity * G_multiplier;
    }
}
