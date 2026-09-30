#nullable enable

using Unity.Cinemachine;
using UnityEngine;

public class SlowlyRotate : MonoBehaviour
{
    public bool randomizeAxis = false;
    public Vector3 rotationAxis = Vector3.up;
    public float rotationsPerSecond = 1.0f;

    void Start()
    {
        if (randomizeAxis)
        {
            rotationAxis = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            rotationAxis.Normalize();
        }
    }

    // rotated in step with physics rather than every rendered frame - with Auto Sync Transforms off,
    // an Update-driven rotation leaves the collider (only synced on the next physics step) lagging
    // several frames behind the rendered object and anything parented to it, so the player's ground
    // raycasts were hitting a stale pose
    void FixedUpdate()
    {
        transform.Rotate(rotationAxis.normalized, 360.0f * rotationsPerSecond * Time.fixedDeltaTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var localAxis = randomizeAxis ? rotationAxis : rotationAxis.normalized;
        if (localAxis == Vector3.zero)
        {
            return;
        }
        var worldAxis = transform.TransformDirection(localAxis);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position - worldAxis * 10.0f, transform.position + worldAxis * 10.0f);
        Gizmos.DrawSphere(transform.position + worldAxis * 10.0f, 0.05f);
    }
#endif
}
