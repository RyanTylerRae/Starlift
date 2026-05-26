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

    void Update()
    {
        transform.Rotate(rotationAxis.normalized, 360.0f * rotationsPerSecond * Time.deltaTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var localAxis = randomizeAxis ? rotationAxis : rotationAxis.normalized;
        if (localAxis == Vector3.zero) return;
        var worldAxis = transform.TransformDirection(localAxis);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position - worldAxis * 10.0f, transform.position + worldAxis * 10.0f);
        Gizmos.DrawSphere(transform.position + worldAxis * 10.0f, 0.05f);
    }
#endif
}
