#nullable enable

using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    private Camera? targetCamera;
    private Rigidbody? playerRigidbody;
    private FirstPersonController? playerController;

    public float slowThreshold = 1f;
    public float mediumThreshold = 10f;
    public float fastThreshold = 20f;

    public GameObject? upSlow;
    public GameObject? upMedium;
    public GameObject? upFast;

    public GameObject? downSlow;
    public GameObject? downMedium;
    public GameObject? downFast;

    public GameObject? northSlow;
    public GameObject? northMedium;
    public GameObject? northFast;

    public GameObject? southSlow;
    public GameObject? southMedium;
    public GameObject? southFast;

    public GameObject? eastSlow;
    public GameObject? eastMedium;
    public GameObject? eastFast;

    public GameObject? westSlow;
    public GameObject? westMedium;
    public GameObject? westFast;

    private void Start()
    {
        SetActive(upSlow, false);
        SetActive(upMedium, false);
        SetActive(upFast, false);
        SetActive(downSlow, false);
        SetActive(downMedium, false);
        SetActive(downFast, false);
        SetActive(northSlow, false);
        SetActive(northMedium, false);
        SetActive(northFast, false);
        SetActive(southSlow, false);
        SetActive(southMedium, false);
        SetActive(southFast, false);
        SetActive(eastSlow, false);
        SetActive(eastMedium, false);
        SetActive(eastFast, false);
        SetActive(westSlow, false);
        SetActive(westMedium, false);
        SetActive(westFast, false);
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

        if (playerRigidbody == null || playerController == null)
        {
            GameObject? player = StarliftStatics.FindPlayer();
            playerRigidbody = player?.GetComponent<Rigidbody>();
            playerController = player?.GetComponent<FirstPersonController>();
        }

        if (playerRigidbody == null || playerController == null)
        {
            return;
        }

        if (playerController.MovementMode != FirstPersonController.ControllerMovementMode.ZeroG)
        {
            DisableAll();
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        Transform cam = targetCamera.transform;

        float upSpeed    = Mathf.Max(0f, Vector3.Dot(velocity,  cam.up));
        float downSpeed  = Mathf.Max(0f, Vector3.Dot(velocity, -cam.up));
        float northSpeed = Mathf.Max(0f, Vector3.Dot(velocity,  cam.forward));
        float southSpeed = Mathf.Max(0f, Vector3.Dot(velocity, -cam.forward));
        float eastSpeed  = Mathf.Max(0f, Vector3.Dot(velocity,  cam.right));
        float westSpeed  = Mathf.Max(0f, Vector3.Dot(velocity, -cam.right));

        ApplyThresholds(upSpeed,    upSlow,    upMedium,    upFast);
        ApplyThresholds(downSpeed,  downSlow,  downMedium,  downFast);
        ApplyThresholds(northSpeed, northSlow, northMedium, northFast);
        ApplyThresholds(southSpeed, southSlow, southMedium, southFast);
        ApplyThresholds(eastSpeed,  eastSlow,  eastMedium,  eastFast);
        ApplyThresholds(westSpeed,  westSlow,  westMedium,  westFast);
    }

    private void DisableAll()
    {
        ApplyThresholds(-1f, upSlow,    upMedium,    upFast);
        ApplyThresholds(-1f, downSlow,  downMedium,  downFast);
        ApplyThresholds(-1f, northSlow, northMedium, northFast);
        ApplyThresholds(-1f, southSlow, southMedium, southFast);
        ApplyThresholds(-1f, eastSlow,  eastMedium,  eastFast);
        ApplyThresholds(-1f, westSlow,  westMedium,  westFast);
    }

    private void ApplyThresholds(float speed, GameObject? slow, GameObject? medium, GameObject? fast)
    {
        SetActive(slow,   speed >= slowThreshold);
        SetActive(medium, speed >= mediumThreshold);
        SetActive(fast,   speed >= fastThreshold);
    }

    private static void SetActive(GameObject? obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }
}
