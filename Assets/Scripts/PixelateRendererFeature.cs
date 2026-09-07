#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PixelateRendererFeature : ScriptableRendererFeature
{
    public Material? material;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private PixelateRenderPass? pixelateRenderPass = null;

    public override void Create()
    {
        pixelateRenderPass = new PixelateRenderPass();
        pixelateRenderPass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null)
        {
            Debug.LogWarning("PixelateRendererFeature: material is null and will be skipped.");
            return;
        }

        if (pixelateRenderPass != null)
        {
            pixelateRenderPass.Setup(material);
            renderer.EnqueuePass(pixelateRenderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Material is assigned in inspector, don't destroy it
    }
}
