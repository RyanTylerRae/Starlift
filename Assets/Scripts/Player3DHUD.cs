#nullable enable

using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private GameObject? player = null;
    public GameObject? jumpTargetWidget = null;
    public float jumpTargetRaycastDistance = 100f;
    public Vector3 jumpTargetRotationOffset = Vector3.zero;

    public MeshRenderer oxygenProgressRendererForeground;
    private Material? oxygenProgressForegroundMaterialInstance = null;
    public MeshRenderer oxygenProgressRendererBackground;
    private Material? oxygenProgressBackgroundMaterialInstance = null;

    public MeshRenderer jumpTier1Renderer;
    private Material? jumpTier1MaterialInstance = null;
    public MeshRenderer jumpTier2Renderer;
    private Material? jumpTier2MaterialInstance = null;
    public MeshRenderer jumpTier3Renderer;
    private Material? jumpTier3MaterialInstance = null;
    public MeshRenderer magneticChargeRenderer;
    private Material? magneticChargeMaterialInstance = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        oxygenProgressForegroundMaterialInstance = oxygenProgressRendererForeground.material;
        oxygenProgressBackgroundMaterialInstance = oxygenProgressRendererBackground.material;
        jumpTier1MaterialInstance = jumpTier1Renderer.material;
        jumpTier2MaterialInstance = jumpTier2Renderer.material;
        jumpTier3MaterialInstance = jumpTier3Renderer.material;
        magneticChargeMaterialInstance = magneticChargeRenderer.material;
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

        if (jumpTargetWidget != null)
        {
            if (player.TryGetComponent(out FirstPersonController playerController))
            {
                jumpTargetWidget.SetActive(playerController.ShouldDisplayJumpTarget);

                if (playerController.ShouldDisplayJumpTarget && playerController.playerCamera != null)
                {
                    // Raycast from player camera
                    Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, jumpTargetRaycastDistance))
                    {
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
            }
        }

        if (player.TryGetComponent(out Modifiers modifiers))
        {
            if (oxygenProgressForegroundMaterialInstance != null)
            {
                oxygenProgressForegroundMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
            }

            if (oxygenProgressBackgroundMaterialInstance != null)
            {
                oxygenProgressBackgroundMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
            }

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
}
