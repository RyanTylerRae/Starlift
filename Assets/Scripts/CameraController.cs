#nullable enable

using Unity;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject? cameraArm = null;

    public void Start()
    {
        cameraArm?.SetActive(true);
    }
}
