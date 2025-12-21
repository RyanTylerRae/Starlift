#nullable enable

using UnityEngine;

public class GravitySourceMesh : GravitySourceComponent
{
    public Collider meshCollider;
    public float maxDistanceToSurface = 1.0f;

    private bool isPlayerInRange = false;

    public override void Update()
    {
        bool wasPlayerInRange = isPlayerInRange;

        GameObject? player = StarliftStatics.FindPlayer();
        FirstPersonController? firstPersonController = StarliftStatics.FindFirstPersonController();

        if (player != null && firstPersonController != null && meshCollider != null)
        {
            Vector3 playerPos = player.transform.position;
            Vector3 closestPoint = meshCollider.ClosestPoint(player.transform.position);
            Vector3 normal = playerPos - closestPoint;

            if (normal.magnitude - firstPersonController.magnetizeRadius < maxDistanceToSurface)
            {
                isPlayerInRange = true;
            }
            else
            {
                isPlayerInRange = false;
            }

            normal.Normalize();

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

    public override void OnTriggerEnter(Collider other)
    {
        // overriding to do nothing
    }

    public override void OnTriggerExit(Collider other)
    {
        // overriding to do nothing
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

        return normal.normalized * defaultGravity * G_multiplier;
    }
}
