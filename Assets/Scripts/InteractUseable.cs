#nullable enable

using UnityEngine;
using UnityEngine.Events;

public class InteractUseable : Interactable
{
    public override ETooltipActionType ActionType => ETooltipActionType.Use;
    public UnityEvent? InteractEvent = new();

    protected override bool HandleCanInteract() => true;
    protected override void HandleInteract()
    {
        InteractEvent?.Invoke();
    }
}
