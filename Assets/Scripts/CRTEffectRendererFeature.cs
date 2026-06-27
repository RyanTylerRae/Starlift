#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CRTEffectRendererFeature : ScriptableRendererFeature
{
    public Material? material;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private CRTEffectRenderPass? crtEffectRenderPass = null;

    public override void Create()
    {
        crtEffectRenderPass = new CRTEffectRenderPass();
        crtEffectRenderPass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null)
        {
            Debug.LogWarning("CRTRendererFeature: material is null and will be skipped.");
            return;
        }

        if (crtEffectRenderPass != null)
        {
            crtEffectRenderPass.Setup(material);
            renderer.EnqueuePass(crtEffectRenderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Material is assigned in inspector, don't destroy it
    }
}
