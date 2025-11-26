#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FisheyeRendererFeature : ScriptableRendererFeature
{
    public Material? material;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private FisheyeRenderPass? fisheyePass;

    public override void Create()
    {
        fisheyePass = new FisheyeRenderPass();
        fisheyePass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null)
        {
            Debug.LogWarning("FisheyeRendererFeature: material is null and will be skipped.");
            return;
        }

        if (fisheyePass != null)
        {
            fisheyePass.Setup(material);
            renderer.EnqueuePass(fisheyePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Material is assigned in inspector, don't destroy it
    }
}
