#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class ToggleLeverGate : MonoBehaviour
{
    public List<string> blackboardIds = new();
    public ToggleLever? lever;

    void Update()
    {
        if (lever == null)
        {
            return;
        }

        if (lever.TryGetComponent(out InteractUseable interactUseable))
        {
            foreach (string id in blackboardIds)
            {
                if (!Blackboard.Instance.GetValue<bool>(id, out bool value) || !value)
                {
                    return;
                }
            }

            interactUseable.canUse = true;
            Destroy(this);
        }
    }
}
