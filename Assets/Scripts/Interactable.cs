#nullable enable

using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    public abstract ETooltipActionType ActionType { get; }

    public bool CanInteract() => HandleCanInteract();

    protected abstract bool HandleCanInteract();

    public void Interact() => HandleInteract();

    protected abstract void HandleInteract();
}
