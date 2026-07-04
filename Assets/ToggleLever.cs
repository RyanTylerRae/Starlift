#nullable enable

using UnityEngine;

public class ToggleLever : MonoBehaviour
{
    public Animator? leverAnimator;
    public string blackboardId = "";

    public void DoToggleLever()
    {
        if (leverAnimator == null)
        {
            return;
        }

        leverAnimator.SetBool("LeverDown", true);

        if (Blackboard.Instance.GetValue<bool>(blackboardId, out bool leverDown) && !leverDown)
        {
            Blackboard.Instance?.Set(blackboardId, true);
        }

        if (TryGetComponent(out InteractUseable interactUseable))
        {
            interactUseable.canUse = false;
        }
    }
}
