#nullable enable

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class FisheyeRenderPass : ScriptableRenderPass
{
    private Material? fisheyeMaterial;

    public void Setup(Material material)
    {
        fisheyeMaterial = material;
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (fisheyeMaterial == null)
            return;

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        // Only apply to cameras with FisheyeCamera component
        Camera camera = cameraData.camera;
        FisheyeCamera? fisheyeCamera = camera.GetComponent<FisheyeCamera>();

        if (fisheyeCamera == null)
            return;

        // Check if we have an intermediate texture available
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogWarning("FisheyeRenderPass: Cannot apply effect to BackBuffer");
            return;
        }

        // Set material parameters
        fisheyeMaterial.SetFloat("_Strength", fisheyeCamera.distortionStrength);

        // Get source texture
        var source = resourceData.activeColorTexture;

        // Create destination texture with same properties
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = "CameraColor-Fisheye";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Blit with fisheye material
        RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, fisheyeMaterial, 0);
        renderGraph.AddBlitPass(blitParams, passName: "Fisheye Effect");

        // Swap the active color texture to our processed result
        resourceData.cameraColor = destination;
    }
}
