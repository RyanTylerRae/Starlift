#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FogRendererFeature : ScriptableRendererFeature
{
    public Material? material;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private FogRenderPass? fogPass = null;

    public override void Create()
    {
        fogPass = new FogRenderPass();
        fogPass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null)
        {
            Debug.LogWarning("FogRendererFeature: material is null and will be skipped.");
            return;
        }

        if (fogPass != null)
        {
            fogPass.Setup(material);
            renderer.EnqueuePass(fogPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Material is assigned in inspector, don't destroy it
    }
}
