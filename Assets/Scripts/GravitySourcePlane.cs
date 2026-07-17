#nullable enable

using UnityEngine;

public class GravitySourcePlane : GravitySourceComponent
{
    public Collider? triggerCollider;

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

    public override Vector3 GetGravityVector(Vector3 point)
    {
        if (!isGravityEnabled)
        {
            return Vector3.zero;
        }

        return transform.TransformDirection(direction.normalized) * defaultGravity * G_multiplier;
    }

    public override float GetDistanceToSurface(Vector3 point)
    {
        if (triggerCollider == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(point, triggerCollider.ClosestPoint(point));
    }
}
