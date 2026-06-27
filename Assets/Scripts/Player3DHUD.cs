#nullable enable

using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Splines.Interpolators;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private GameObject? player = null;

    [Header("Center Dot Widget")]
    public GameObject? centerDotWidget = null;

    [Header("Jump Target Widget")]
    public GameObject? jumpTargetWidget = null;
    public Vector3 jumpTargetRotationOffset = Vector3.zero;

    [Header("Material Instances")]
    public MeshRenderer? oxygenProgressRendererForeground;
    private Material? oxygenProgressForegroundMaterialInstance = null;
    public MeshRenderer? oxygenProgressLaggyRenderer;
    private Material? oxygenProgressLaggyRendererMaterialInstance = null;
    public MeshRenderer? oxygenProgressRendererBackground;
    private Material? oxygenProgressBackgroundMaterialInstance = null;

    public MeshRenderer? jumpTier1Renderer;
    private Material? jumpTier1MaterialInstance = null;
    public MeshRenderer? jumpTier2Renderer;
    private Material? jumpTier2MaterialInstance = null;
    public MeshRenderer? jumpTier3Renderer;
    private Material? jumpTier3MaterialInstance = null;
    public MeshRenderer? magneticChargeRenderer;
    private Material? magneticChargeMaterialInstance = null;

    [Header("Mouse Look Impulse")]
    public GameObject? lookRoot;

    public float lookCorrectionSpeedZeroG;
    public float maxLookAngleZeroG;

    public float lookCorrectionSpeed;
    public float maxLookAngle;

    private Quaternion prevCameraRotation = Quaternion.identity;

    [Header("Laggy Oxygen Bar")]
    private bool wasBurningOxygen = false;
    private float laggyOxygenProgress = 0f;
    public float laggyOxygenSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        oxygenProgressForegroundMaterialInstance = oxygenProgressRendererForeground?.material;
        oxygenProgressLaggyRendererMaterialInstance = oxygenProgressLaggyRenderer?.material;
        oxygenProgressBackgroundMaterialInstance = oxygenProgressRendererBackground?.material;
        jumpTier1MaterialInstance = jumpTier1Renderer?.material;
        jumpTier2MaterialInstance = jumpTier2Renderer?.material;
        jumpTier3MaterialInstance = jumpTier3Renderer?.material;
        magneticChargeMaterialInstance = magneticChargeRenderer?.material;
    }

    // Reset orientation for the HUD itself to allow individual tracking
    public void LateUpdate()
    {
        //transform.rotation = Quaternion.identity;
    }

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

        if (!player.TryGetComponent(out Modifiers modifiers))
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
        }

        if (jumpTargetWidget != null)
        {
            bool shouldDisplayJumpTarget = false;

            if (playerController.ShouldDisplayJumpTarget && playerController.playerCamera != null)
            {
                // Raycast from player camera
                Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);
                RaycastHit hit;

                player.TryGetComponent(out GravityController gravityController);
                GravitySourceComponent? activeGravitySource = gravityController?.GetActiveGravitySource();

                if (Physics.Raycast(ray, out hit, playerController.jumpTargetRaycastDistance, LayerMask.GetMask("Default"))
                    && hit.collider.GetComponentInParent<GravitySourceComponent>() != activeGravitySource)
                {
                    shouldDisplayJumpTarget = true;

                    // Get hit point in player camera's local space
                    Vector3 playerCameraLocalHit = playerController.playerCamera.transform.InverseTransformPoint(hit.point);
                    // Use that same local offset for the widget relative to HUD camera
                    jumpTargetWidget.transform.localPosition = playerCameraLocalHit;

                    // Get normal in player camera's local space
                    Vector3 playerCameraLocalNormal = playerController.playerCamera.transform.InverseTransformDirection(hit.normal);
                    // Use that same local direction for the widget, with rotation offset applied
                    Quaternion normalRotation = Quaternion.LookRotation(playerCameraLocalNormal);
                    Quaternion offsetRotation = Quaternion.Euler(jumpTargetRotationOffset);
                    jumpTargetWidget.transform.localRotation = normalRotation * offsetRotation;
                }
            }

            jumpTargetWidget.SetActive(shouldDisplayJumpTarget);

            if (centerDotWidget != null)
            {
                centerDotWidget.SetActive(!playerController.ShouldDisplayJumpTarget);
            }

            prevCameraRotation = playerController.playerCamera?.transform.rotation ?? prevCameraRotation;
        }

        // handle oxygen burn logic for the laggy progress bar
        if (!wasBurningOxygen && playerController.OxygenBurnRate > 0.0f)
        {
            laggyOxygenProgress = modifiers.Get(ModifierType.Oxygen);
        }

        if (playerController.OxygenBurnRate <= 0.0f)
        {
            laggyOxygenProgress = Mathf.Lerp(laggyOxygenProgress, modifiers.Get(ModifierType.Oxygen), Time.deltaTime * laggyOxygenSpeed);
        }

        // ensure that laggy oxygen progress never dips below the normal oxygen bar
        laggyOxygenProgress = Mathf.Max(laggyOxygenProgress, modifiers.Get(ModifierType.Oxygen));

        wasBurningOxygen = playerController.OxygenBurnRate > 0.0f;

        // update modifiers
        if (oxygenProgressForegroundMaterialInstance != null)
        {
            oxygenProgressForegroundMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
        }

        if (oxygenProgressLaggyRendererMaterialInstance != null)
        {
            oxygenProgressLaggyRendererMaterialInstance.SetFloat("_Progress", laggyOxygenProgress / modifiers.GetMax(ModifierType.Oxygen));
        }

        // if (oxygenProgressBackgroundMaterialInstance != null)
        // {
        //     oxygenProgressBackgroundMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
        // }

        if (jumpTier1MaterialInstance != null)
        {
            jumpTier1MaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.JumpCharge_Tier1));
        }

        if (jumpTier2MaterialInstance != null)
        {
            jumpTier2MaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.JumpCharge_Tier2));
        }

        if (jumpTier3MaterialInstance != null)
        {
            jumpTier3MaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.JumpCharge_Tier3));
        }

        if (magneticChargeMaterialInstance != null)
        {
            magneticChargeMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.MagneticCharge));
        }
    }
}
