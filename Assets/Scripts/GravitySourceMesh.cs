#nullable enable

using UnityEngine;

public class GravitySourceMesh : GravitySourceComponent
{
    public Collider meshCollider;
    public float maxDistanceToSurface = 1.0f;

    private bool isPlayerInRange = false;
    private Vector3 cachedClosestPoint;
    private Vector3 cachedNormal;

    public override void Update()
    {
        bool wasPlayerInRange = isPlayerInRange;

        GameObject? player = StarliftStatics.FindPlayer();
        if (player != null && meshCollider != null)
        {
            Vector3 playerPos = player.transform.position;
            cachedClosestPoint = meshCollider.ClosestPoint(player.transform.position);
            cachedNormal = playerPos - cachedClosestPoint;

            if (cachedNormal.sqrMagnitude < maxDistanceToSurface * maxDistanceToSurface)
            {
                isPlayerInRange = true;
            }
            else
            {
                isPlayerInRange = false;
            }

            cachedNormal.Normalize();

            if (wasPlayerInRange && !isPlayerInRange)
            {
                OnTriggerExitInternal(player);
            }

            if (!wasPlayerInRange && isPlayerInRange)
            {
                OnTriggerEnterInternal(player);
            }
        }
    }

    public override Vector3 GetGravityVector(Vector3 point)
    {
        if (!isGravityEnabled)
        {
            return Vector3.zero;
        }

        if (meshCollider == null)
        {
            return Vector3.zero;
        }

        Vector3 closestPoint = meshCollider.ClosestPoint(point);
        Vector3 normal = point - closestPoint;

        if (normal.sqrMagnitude < maxDistanceToSurface * maxDistanceToSurface)
        {
            return normal.normalized * defaultGravity * G_multiplier;
        }

        return Vector3.zero;
    }
}
