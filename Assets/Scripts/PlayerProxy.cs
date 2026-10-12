#nullable enable

using UnityEngine;

/// <summary>
/// Scene-side stand-in for the runtime-spawned player. Triggers and other scene objects can't
/// reference the player in the inspector because it doesn't exist until PlayerSpawner runs, so
/// they call these methods instead, which find the player on demand and forward the call.
/// </summary>
public class PlayerProxy : MonoBehaviour
{
    private FirstPersonController? playerController = null;

    private FirstPersonController? GetPlayerController()
    {
        if (playerController == null)
        {
            playerController = StarliftStatics.FindFirstPersonController();
        }

        if (playerController == null)
        {
            Debug.LogWarning("PlayerProxy: no player found", this);
        }

        return playerController;
    }

    public void SetZoomDistanceShort()
    {
        GetPlayerController()?.SetZoomDistanceShort();
    }

    public void SetZoomDistanceMedium()
    {
        GetPlayerController()?.SetZoomDistanceMedium();
    }

    public void SetZoomDistanceFar()
    {
        GetPlayerController()?.SetZoomDistanceFar();
    }
}
