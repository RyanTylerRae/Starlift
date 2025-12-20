#nullable enable

using System;
using UnityEngine;

public static class StarliftStatics
{
    public static GameObject? FindPlayer()
    {
        FirstPersonController? controller = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        return controller?.gameObject;
    }

    public static Camera? FindPlayerCamera()
    {
        var player = FindPlayer();
        if (player != null)
        {
            try
            {
                return player.GetComponentInChildren<Camera>(includeInactive: true);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        return null;
    }

    public static FirstPersonController? FindFirstPersonController()
    {
        var player = FindPlayer();
        if (player != null)
        {
            try
            {
                return player.GetComponentInChildren<FirstPersonController>(includeInactive: true);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        return null;
    }
}
