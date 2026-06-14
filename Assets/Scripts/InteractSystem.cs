#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class InteractSystem : MonoBehaviour
{
    public float interactRange = 3f;

    public GameObject? useActionUI = null;

    private FirstPersonController? playerController = null;

    private List<HoverTooltip> tooltipsInContext = new();

    public void Start()
    {
        playerController = GetComponent<FirstPersonController>();
    }

    public void Update()
    {
        if (playerController == null || playerController.playerCamera == null)
        {
            return;
        }

        foreach (HoverTooltip hoverTooltip in tooltipsInContext)
        {
            var interactable = hoverTooltip.gameObject.GetComponent<Interactable>();
            if (interactable != null)
            {
                SetUIActive(false, interactable.ActionType);
            }
            hoverTooltip.IsHovered = false;
        }
        tooltipsInContext.Clear();

        Camera camera = playerController.playerCamera;
        RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, interactRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.TryGetComponent(out HoverTooltip hoverTooltip))
            {
                tooltipsInContext.Add(hoverTooltip);
                hoverTooltip.IsHovered = true;

                var interactable = hit.collider.gameObject.GetComponent<Interactable>();
                if (interactable != null && interactable.CanInteract())
                {
                    SetUIActive(true, interactable.ActionType);
                }
            }
        }
    }

    public void TryInteractFirst()
    {
        if (tooltipsInContext.Count == 0)
        {
            return;
        }

        var interactable = tooltipsInContext[0].gameObject.GetComponent<Interactable>();
        if (interactable != null && interactable.CanInteract())
        {
            interactable.Interact();
        }
    }

    private void SetUIActive(bool state, ETooltipActionType tooltipType)
    {
        GameObject? uiObject = null;

        switch (tooltipType)
        {
            case ETooltipActionType.Use:
                uiObject = useActionUI;
                break;
        }

        if (uiObject != null)
        {
            uiObject?.SetActive(state);
        }
    }
}
