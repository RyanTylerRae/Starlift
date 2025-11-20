#nullable enable

using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private FirstPersonController? playerController = null;
    private Modifiers? modifiers = null;

    public Image oxygenProgressImage;
    private Material? oxygenProgressMaterialInstance = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        modifiers = GetComponent<Modifiers>();

        if (TryGetComponent(out playerController) && playerController != null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            canvas.worldCamera = playerController.playerCamera;
        }

        oxygenProgressMaterialInstance = Instantiate(oxygenProgressImage.material);
        oxygenProgressImage.material = oxygenProgressMaterialInstance;
    }

    // Update is called once per frame
    public void Update()
    {
        if (modifiers == null)
        {
            return;
        }

        if (oxygenProgressMaterialInstance != null)
        {
            oxygenProgressMaterialInstance.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
        }
    }
}
