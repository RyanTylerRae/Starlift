using UnityEngine;

public class SlowlyRotate : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;
    public float rotationsPerSecond = 1.0f;

    void Update()
    {
        transform.Rotate(rotationAxis, 360.0f * rotationsPerSecond * Time.deltaTime);
    }
}
