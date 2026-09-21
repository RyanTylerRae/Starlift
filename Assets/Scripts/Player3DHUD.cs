#nullable enable

using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    private GameObject? player = null;

    [Header("Mouse Look Impulse")]
    public GameObject? lookRoot;

    public float lookCorrectionSpeedZeroG;
    public float maxLookAngleZeroG;

    public float lookCorrectionSpeed;
    public float maxLookAngle;

    private Quaternion prevCameraRotation = Quaternion.identity;

    // Update is called once per frame
    public void Update()
    {
        if (player == null)
        {
            player = StarliftStatics.FindPlayer();
            if (player == null)
            {
                return;
            }
        }

        if (!player.TryGetComponent(out FirstPersonController playerController))
        {
            return;
        }

        // Handle HUD orientation based on movement mode
        if (lookRoot != null)
        {
            float _maxLookAngle = maxLookAngle;
            float _lookCorrectionSpeed = lookCorrectionSpeed;

            if (playerController.MovementMode == FirstPersonController.ControllerMovementMode.ZeroG)
            {
                _maxLookAngle = maxLookAngleZeroG;
                _lookCorrectionSpeed = lookCorrectionSpeedZeroG;
            }

            // find the new local rotation, clamped to a maximum angle
            Quaternion deltaRotation = Quaternion.Inverse(prevCameraRotation) * (playerController.playerCamera?.transform.rotation ?? Quaternion.identity);

            // remove roll, because it feels wrong
            Vector3 eulerDeltaRotation = deltaRotation.eulerAngles;
            eulerDeltaRotation.z = 0.0f;
            deltaRotation = Quaternion.Euler(eulerDeltaRotation);

            Quaternion localRotation = Quaternion.Inverse(deltaRotation) * lookRoot.transform.localRotation;
            localRotation = Quaternion.RotateTowards(Quaternion.identity, localRotation, _maxLookAngle);

            // slerp towards identity at a set speed
            lookRoot.transform.localRotation = Quaternion.Slerp(localRotation, Quaternion.identity, _lookCorrectionSpeed * Time.deltaTime);

            prevCameraRotation = playerController.playerCamera?.transform.rotation ?? prevCameraRotation;
        }
    }
}
