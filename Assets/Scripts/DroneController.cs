#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    [Header("Points")]
    public List<Transform> points = new List<Transform>();
    // a candidate point must be this much closer to the player than the current one before the drone
    // switches, so it doesn't flip-flop when the player stands between two points
    public float minCloserDistanceToSwitch = 1f;

    [Header("Motion")]
    public float maxSpeed = 4f;
    public float acceleration = 3f;
    public float arriveDistance = 0.01f;

    [Header("Rotation")]
    // degrees per second
    public float maxAngularSpeed = 90f;
    // degrees per second squared
    public float angularAcceleration = 90f;
    public float arriveAngle = 0.1f;

    private GameObject? player;
    private Transform? currentPoint;
    private Vector3 velocity = Vector3.zero;
    private float angularSpeed = 0f;

    private void Update()
    {
        if (player == null)
        {
            player = StarliftStatics.FindPlayer();
        }

        if (player == null)
        {
            return;
        }

        UpdateCurrentPoint(player.transform.position);

        if (currentPoint == null)
        {
            return;
        }

        MoveTowardsPoint(currentPoint.position);
        RotateTowardsPoint(currentPoint.rotation);
    }

    private void UpdateCurrentPoint(Vector3 playerPosition)
    {
        Transform? closestPoint = null;
        float closestSqrDistance = float.MaxValue;

        foreach (Transform point in points)
        {
            if (point == null)
            {
                continue;
            }

            float sqrDistance = (point.position - playerPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestPoint = point;
            }
        }

        if (closestPoint == null || closestPoint == currentPoint)
        {
            return;
        }

        if (currentPoint == null)
        {
            currentPoint = closestPoint;
            return;
        }

        float currentDistance = Vector3.Distance(currentPoint.position, playerPosition);
        float closestDistance = Mathf.Sqrt(closestSqrDistance);
        if (currentDistance - closestDistance > minCloserDistanceToSwitch)
        {
            currentPoint = closestPoint;
        }
    }

    private void MoveTowardsPoint(Vector3 target)
    {
        Vector3 toTarget = target - transform.position;
        float distance = toTarget.magnitude;

        // fastest speed that can still brake to a stop in the remaining distance: v^2 = 2ad
        float desiredSpeed = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * acceleration * distance));
        Vector3 desiredVelocity = Vector3.zero;
        if (distance > arriveDistance)
        {
            desiredVelocity = toTarget / distance * desiredSpeed;
        }

        // velocity changes by at most acceleration * dt, which eases in from rest and out on arrival,
        // and bends smoothly toward a new target when the point switches mid-flight
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, acceleration * Time.deltaTime);
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        Vector3 step = velocity * Time.deltaTime;

        // snap on the final frame rather than overshooting
        if (distance <= arriveDistance || step.magnitude >= distance)
        {
            transform.position = target;
            velocity = Vector3.zero;
            return;
        }

        transform.position += step;
    }

    private void RotateTowardsPoint(Quaternion target)
    {
        float angle = Quaternion.Angle(transform.rotation, target);

        // same profile as MoveTowardsPoint, applied to the remaining angle
        float desiredAngularSpeed = 0f;
        if (angle > arriveAngle)
        {
            desiredAngularSpeed = Mathf.Min(maxAngularSpeed, Mathf.Sqrt(2f * angularAcceleration * angle));
        }

        angularSpeed = Mathf.MoveTowards(angularSpeed, desiredAngularSpeed, angularAcceleration * Time.deltaTime);
        angularSpeed = Mathf.Min(angularSpeed, maxAngularSpeed);

        float step = angularSpeed * Time.deltaTime;

        if (angle <= arriveAngle || step >= angle)
        {
            transform.rotation = target;
            angularSpeed = 0f;
            return;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, step);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        foreach (Transform point in points)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.25f);
                // show the orientation the drone will match
                Gizmos.DrawRay(point.position, point.forward * 0.5f);
            }
        }

        if (currentPoint != null)
        {
            Gizmos.DrawLine(transform.position, currentPoint.position);
        }
    }
}
