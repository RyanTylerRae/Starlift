#nullable enable

using UnityEngine;

public class PlayerCameraRelativeUpAlignment : MonoBehaviour
{
    private Camera? targetCamera = null;
    private Quaternion defaultRotation = Quaternion.identity;

    private void Start()
    {
        defaultRotation = targetCamera?.transform.rotation ?? Quaternion.identity;
    }

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

        Vector3 cameraForward = targetCamera.transform.forward;
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
        transform.localRotation = targetRotation;
    }
}
