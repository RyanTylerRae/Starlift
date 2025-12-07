#nullable enable

using UnityEngine;

public class PlayerCameraRelativeUpAlignment : MonoBehaviour
{
    private Camera? targetCamera;

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = StarliftStatics.FindPlayerCamera();
        }

        if (targetCamera == null)
        {
            return;
        }

        // Get the world up vector
        Vector3 worldUp = Vector3.up;

        // Calculate the rotation that would align the camera's forward direction
        // with maintaining world up as the up direction
        Vector3 cameraForward = targetCamera.transform.forward;

        // Create a rotation that looks in the camera's forward direction
        // but keeps world up as the up vector
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward, worldUp);

        // Set the local rotation of this object
        transform.localRotation = targetRotation;
    }
}
