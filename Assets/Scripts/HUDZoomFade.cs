#nullable enable

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades the composited 3D HUD out over a duration while the player holds zoom, and back in on release.
/// Lives on the 3D HUD camera. The player is spawned at runtime, so it is found lazily, and the fade is
/// applied to the player's RawImage that displays this camera's render texture - one alpha covers
/// every HUD element regardless of their shaders.
/// </summary>
[RequireComponent(typeof(Camera))]
public class HUDZoomFade : MonoBehaviour
{
    public float fadeOutDuration = 0.25f;
    public float fadeInDuration = 0.25f;

    // alpha while hidden - raise above 0 to dim the HUD rather than hide it
    [Range(0f, 1f)]
    public float hiddenAlpha = 0.0f;

    // inverts the fade: hidden normally and shown while zoom is held (e.g. world scan markers)
    public bool showOnlyWhileZoomed = false;

    private Camera? hudCamera = null;
    private CanvasGroup? canvasGroup = null;
    private FirstPersonController? playerController = null;
    private bool isHudHidden = false;

    public bool IsHudHidden { get { return isHudHidden; } }

    public void Awake()
    {
        hudCamera = GetComponent<Camera>();
    }

    // hides the whole HUD regardless of zoom (e.g. during scripted sequences) - hiding is instant,
    // showing fades back in over fadeInDuration. Safe to call before the player has been bound
    public void SetHudHidden(bool hidden)
    {
        isHudHidden = hidden;

        if (hidden && canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
        }
    }

    public void Update()
    {
        if (playerController == null || canvasGroup == null)
        {
            TryBindToPlayer();
        }

        if (playerController == null || canvasGroup == null)
        {
            return;
        }

        if (isHudHidden)
        {
            canvasGroup.alpha = 0.0f;
            return;
        }

        bool zoomHeld = playerController.IsZoomHeld;
        bool isShown = showOnlyWhileZoomed ? zoomHeld : !zoomHeld;
        float targetAlpha = isShown ? 1.0f : hiddenAlpha;
        float duration = isShown ? fadeInDuration : fadeOutDuration;

        // rate covers the full 1 -> hiddenAlpha span in the given duration
        float span = Mathf.Max(1.0f - hiddenAlpha, 0.0001f);
        float step = duration > 0.0f ? (span / duration) * Time.deltaTime : span;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, step);
    }

    // finds the spawned player and the RawImage showing this camera's render texture, and
    // gets (or adds) the CanvasGroup on it. Leaves references unset until the player exists.
    private void TryBindToPlayer()
    {
        GameObject? player = StarliftStatics.FindPlayer();
        if (player == null)
        {
            return;
        }

        if (!player.TryGetComponent(out FirstPersonController controller))
        {
            return;
        }

        RawImage? hudDisplay = null;
        RenderTexture? hudTexture = hudCamera != null ? hudCamera.targetTexture : null;
        foreach (RawImage rawImage in player.GetComponentsInChildren<RawImage>(true))
        {
            if (hudTexture != null && rawImage.texture == hudTexture)
            {
                hudDisplay = rawImage;
                break;
            }
        }

        if (hudDisplay == null)
        {
            return;
        }

        if (!hudDisplay.TryGetComponent(out CanvasGroup group))
        {
            group = hudDisplay.gameObject.AddComponent<CanvasGroup>();
        }

        // start hidden rather than fading out from full on the first frames
        if (showOnlyWhileZoomed)
        {
            group.alpha = hiddenAlpha;
        }

        playerController = controller;
        canvasGroup = group;
    }
}
