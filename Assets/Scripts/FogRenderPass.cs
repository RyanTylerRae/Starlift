#nullable enable

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class FogRenderPass : ScriptableRenderPass
{
    private class PassData
    {
        public Material material = null!;
        public TextureHandle source;
        public TextureHandle depth;
    }

    private Material? fogMaterial = null;

    public void Setup(Material material)
    {
        fogMaterial = material;
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (fogMaterial == null)
        {
            return;
        }

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        // Only apply to cameras with FogCamera component
        Camera camera = cameraData.camera;
        FogCamera? fogCamera = camera.GetComponent<FogCamera>();

        if (fogCamera == null)
        {
            return;
        }

        // Check if we have an intermediate texture available
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogWarning("FogRenderPass: Cannot apply effect to BackBuffer");
            return;
        }

        // Set material parameters
        fogMaterial.SetColor("_FogColor", fogCamera.fogColor);
        fogMaterial.SetFloat("_FogStartDistance", fogCamera.fogStartDistance);
        fogMaterial.SetFloat("_FogPower", fogCamera.fogPower);

        // Get source and depth textures
        var source = resourceData.activeColorTexture;
        var depth = resourceData.cameraDepthTexture;

        // Create destination texture with same properties
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = "CameraColor-Fog";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Manual raster pass so we can bind both color and depth as inputs
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Distance Fog", out var passData))
        {
            passData.material = fogMaterial;
            passData.source = source;
            passData.depth = depth;

            builder.UseTexture(source);
            builder.UseTexture(depth);
            builder.SetRenderAttachment(destination, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                data.material.SetTexture("_CameraDepthTexture", data.depth);
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // Swap the active color texture to our processed result
        resourceData.cameraColor = destination;
    }
}
