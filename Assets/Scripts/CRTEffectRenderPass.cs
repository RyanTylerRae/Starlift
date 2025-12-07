#nullable enable

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class CRTEffectRenderPass : ScriptableRenderPass
{
    private Material? crtEffectMaterial;

    public void Setup(Material material)
    {
        crtEffectMaterial = material;
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (crtEffectMaterial == null)
        {
            return;
        }

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        // Only apply to cameras with CRTCamera component
        Camera camera = cameraData.camera;
        CRTCamera? crtCamera = camera.GetComponent<CRTCamera>();

        if (crtCamera == null)
        {
            return;
        }

        // Check if we have an intermediate texture available
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogWarning("CRTEffectRenderPass: Cannot apply effect to BackBuffer");
            return;
        }

        // Set material parameters
        crtEffectMaterial.SetFloat("_ScanlineIntensity", crtCamera.scanlineIntensity);
        crtEffectMaterial.SetFloat("_ScanlineCount", crtCamera.scanlineCount);
        crtEffectMaterial.SetFloat("_Vignette", crtCamera.vignette);
        crtEffectMaterial.SetFloat("_ChromaticAberration", crtCamera.chromaticAbberation);
        crtEffectMaterial.SetFloat("_Brightness", crtCamera.brightness);

        // Get source texture
        var source = resourceData.activeColorTexture;

        // Create destination texture with same properties
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = "CameraColor-CRT";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Blit with CRT effect material
        RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, crtEffectMaterial, 0);
        renderGraph.AddBlitPass(blitParams, passName: "CRT Camera Effect");

        // Swap the active color texture to our processed result
        resourceData.cameraColor = destination;
    }
}
