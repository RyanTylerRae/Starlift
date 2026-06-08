#nullable enable

using UnityEngine;

public enum ETooltipActionType
{
    Use
}

public class HoverTooltip : MonoBehaviour
{
    public Collider? trigger;

    private bool isHovered = false;
    public bool IsHovered
    {
        get { return isHovered; }
        set { isHovered = value; }
    }

}
