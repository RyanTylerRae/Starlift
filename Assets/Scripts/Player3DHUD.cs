#nullable enable

using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private GameObject? player = null;
    public MeshRenderer oxygenProgressRenderer;
    private Material? oxygenProgressMaterialInstance = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        oxygenProgressMaterialInstance = oxygenProgressRenderer.material;
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
            if (oxygenProgressMaterialInstance != null)
            {
                oxygenProgressMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
            }
        }
    }
}
