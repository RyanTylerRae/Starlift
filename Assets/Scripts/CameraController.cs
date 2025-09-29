using Unity;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject cameraArm;

    public void Start()
    {
        cameraArm.SetActive(true);
    }
}
