#nullable enable

using UnityEngine;
using UnityEngine.Events;

public class InteractUseable : Interactable
{
    public override ETooltipActionType ActionType
    {
        get { return ETooltipActionType.Use; }
    }

    public UnityEvent? InteractEvent = new();
    public bool canUse = true;

    protected override bool HandleCanInteract()
    {
        return canUse;
    }

    protected override void HandleInteract()
    {
        InteractEvent?.Invoke();
    }
}
