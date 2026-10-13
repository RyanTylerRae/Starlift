#nullable enable

using UnityEngine;

/// <summary>
/// Marks a world object as scannable and shows a WorldHUDElement over it (name + scan progress).
/// The HUD element lives under the 3D HUD camera, not on this object; it's created when this component
/// is enabled and destroyed when it's disabled. The 3D HUD may not exist yet when this enables, so the
/// projector is found lazily.
/// </summary>
public class Scannable : MonoBehaviour
{
    public string displayName = "";

    [Range(0f, 1f)]
    public float scanProgress = 0.0f;

    public bool showProgressBar = true;

    [Header("HUD")]
    public WorldHUDElement.OffScreenMode offScreenMode = WorldHUDElement.OffScreenMode.Hide;

    // world-space offset applied to this object's bounds before the HUD element is placed below them
    public Vector3 hudOffset = Vector3.zero;

    private WorldHUDElement? hudElement;

    public float ScanProgress { get { return scanProgress; } }

    public void SetScanProgress(float progress)
    {
        scanProgress = Mathf.Clamp01(progress);
    }

    public void OnEnable()
    {
        TryCreateHudElement();
    }

    public void OnDisable()
    {
        if (hudElement != null)
        {
            Destroy(hudElement.gameObject);
        }

        hudElement = null;
    }

    public void Update()
    {
        if (hudElement == null)
        {
            TryCreateHudElement();
            if (hudElement == null)
            {
                return;
            }
        }

        hudElement.offScreenMode = offScreenMode;
        hudElement.worldOffset = hudOffset;
        hudElement.SetText(displayName);
        hudElement.SetProgress(scanProgress);
        hudElement.SetBarVisible(showProgressBar);
    }

    private void TryCreateHudElement()
    {
        WorldHUDProjector? projector = FindFirstObjectByType<WorldHUDProjector>();
        if (projector == null)
        {
            return;
        }

        hudElement = projector.CreateElement(transform);
    }
}
