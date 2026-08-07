#nullable enable

using UnityEngine;

public class ToggleTrigger : MonoBehaviour
{
    public ToggleObject? toggleObject;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
        {
            return;
        }

        if (other.gameObject.TryGetComponent(out FirstPersonController controller))
        {
            triggered = true;
            toggleObject?.Toggle();
        }
    }
}
