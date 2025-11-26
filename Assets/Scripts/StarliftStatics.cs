#nullable enable

using UnityEngine;

public static class StarliftStatics
{
    public static GameObject? FindPlayer()
    {
        FirstPersonController? controller = Object.FindObjectOfType<FirstPersonController>();
        return controller?.gameObject;
    }
}
