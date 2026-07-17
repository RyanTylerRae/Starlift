#nullable enable

using UnityEngine;

public class GravitySourceMesh : GravitySourceComponent
{
    public Collider? meshCollider;
    public float maxDistanceToSurface = 1.0f;

    private bool isPlayerInRange = false;

    private bool isInitialized = false;
    private GameObject? player = null;
    private FirstPersonController? firstPersonController = null;
    private static float MIN_UPDATE_DISTANCE_SQRD = 100.0f * 100.0f;

    public override void Update()
    {
        if (!isInitialized)
        {
            player = StarliftStatics.FindPlayer();
            firstPersonController = StarliftStatics.FindFirstPersonController();
            isInitialized = true;
        }

        if (player == null || meshCollider == null)
        {
            return;
        }

        Vector3 checkDistance = player.transform.position - meshCollider.transform.position;
        if (checkDistance.sqrMagnitude > MIN_UPDATE_DISTANCE_SQRD)
        {
            return;
        }

        bool wasPlayerInRange = isPlayerInRange;

        if (firstPersonController != null)
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

    public override float GetDistanceToSurface(Vector3 point)
    {
        if (meshCollider == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(point, meshCollider.ClosestPoint(point));
    }
}
