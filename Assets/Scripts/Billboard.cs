#nullable enable

using UnityEngine;

public class Billboard : MonoBehaviour
{
    public GameObject? cameraObject;

    void LateUpdate()
    {
        if (cameraObject == null)
        {
            return;
        }

        Camera cam = cameraObject.GetComponent<Camera>();

        // Get camera position but lock Y rotation
        Vector3 lookPos = cam.transform.position;
        lookPos.y = transform.position.y; // ignore vertical difference

        transform.LookAt(lookPos);
    }
}
