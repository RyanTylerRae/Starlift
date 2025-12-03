#nullable enable

using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private GameObject? player = null;
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
