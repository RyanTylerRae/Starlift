#nullable enable

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class PixelateRenderPass : ScriptableRenderPass
{
    private Material? pixelateMaterial = null;

    public void Setup(Material material)
    {
        pixelateMaterial = material;
        requiresIntermediateTexture = true;
        ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    private class PassData
    {
        internal Material material = null!;
        internal TextureHandle source;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (pixelateMaterial == null)
        {
            return;
        }

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        // Only apply to cameras with PixelateCamera component
        Camera camera = cameraData.camera;
        PixelateCamera? pixelateCamera = camera.GetComponent<PixelateCamera>();

        if (pixelateCamera == null)
        {
            return;
        }

        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogWarning("PixelateRenderPass: Cannot apply effect to BackBuffer");
            return;
        }

        pixelateMaterial.SetFloat("_PixelsPerScreenHeight", pixelateCamera.pixelsPerScreenHeight);

        var source = resourceData.activeColorTexture;
        var cameraDepth = resourceData.cameraDepthTexture;

        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = "CameraColor-Pixelate";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Pixelate Effect", out var passData))
        {
            passData.material = pixelateMaterial;
            passData.source = source;

            builder.UseTexture(source, AccessFlags.Read);
            builder.UseTexture(cameraDepth, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        resourceData.cameraColor = destination;
    }
}
