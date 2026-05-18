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
}
