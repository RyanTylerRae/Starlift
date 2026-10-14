#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Marks a world object as scannable and shows a WorldHUDElement over it (name + scan progress).
/// The player scans by looking at the object while holding zoom: inside chargeAngle the scan charges
/// to full, further out it settles at the level of the angle band the look direction falls in, and
/// outside every band (or with zoom released) it drains back to 0. When the scan reaches full,
/// onScanned fires once and the marker is removed.
/// The HUD element lives under the 3D HUD camera, not on this object; it's created when this component
/// is enabled and destroyed when it's disabled. The 3D HUD may not exist yet when this enables, so the
/// projector is found lazily.
/// </summary>
public class Scannable : MonoBehaviour
{
    [Serializable]
    public class ScanBand
    {
        // applies while the look direction is within this many degrees of the object
        // (and outside every smaller band)
        public float maxAngle = 20.0f;

        // progress settles at this level while the look direction is inside the band
        [Range(0f, 1f)]
        public float level = 0.5f;
    }

    public string displayName = "";

    [Range(0f, 1f)]
    public float scanProgress = 0.0f;

    public bool showProgressBar = true;

    [Header("Scanning")]
    // scanning only happens while the player holds zoom
    public bool requireZoom = true;

    // within this many degrees of the look direction the scan charges to full
    public float chargeAngle = 10.0f;

    // progress per second while charging to full
    public float chargeSpeed = 0.25f;

    // partial levels outside chargeAngle, in increasing angle order; outside the last band the level is 0
    public List<ScanBand> angleBands = new List<ScanBand>
    {
        new ScanBand { maxAngle = 20.0f, level = 0.5f },
        new ScanBand { maxAngle = 30.0f, level = 0.2f },
    };

    // progress per second when moving toward a band's level (up or down), including draining to 0
    public float levelChangeSpeed = 1.0f;

    // fires once when the scan reaches full
    public UnityEvent onScanned = new();

    [Header("HUD")]
    public WorldHUDElement.OffScreenMode offScreenMode = WorldHUDElement.OffScreenMode.Hide;

    // world-space offset applied to this object's bounds before the HUD element is placed below them
    public Vector3 hudOffset = Vector3.zero;

    private WorldHUDElement? hudElement;
    private FirstPersonController? playerController;
    private bool isScanned = false;

    public float ScanProgress { get { return scanProgress; } }

    public bool IsScanned { get { return isScanned; } }

    public void SetScanProgress(float progress)
    {
        scanProgress = Mathf.Clamp01(progress);
    }

    public void OnEnable()
    {
        if (!isScanned)
        {
            TryCreateHudElement();
        }
    }

    public void OnDisable()
    {
        DestroyHudElement();
    }

    public void Update()
    {
        if (isScanned)
        {
            return;
        }

        UpdateScanProgress();

        // checked here rather than in UpdateScanProgress so SetScanProgress (or the inspector) can complete it too
        if (scanProgress >= 1.0f)
        {
            CompleteScan();
            return;
        }

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

    private void UpdateScanProgress()
    {
        if (playerController == null)
        {
            playerController = StarliftStatics.FindFirstPersonController();
        }

        Camera? playerCamera = playerController != null ? playerController.playerCamera : null;
        if (playerController == null || playerCamera == null)
        {
            return;
        }

        bool isCharging = false;
        float targetLevel = 0.0f;

        if (!requireZoom || playerController.IsZoomHeld)
        {
            Transform cameraTransform = playerCamera.transform;
            float angle = Vector3.Angle(cameraTransform.forward, transform.position - cameraTransform.position);

            if (angle <= chargeAngle)
            {
                isCharging = true;
            }
            else
            {
                targetLevel = GetBandLevel(angle);
            }
        }

        if (isCharging)
        {
            scanProgress = Mathf.MoveTowards(scanProgress, 1.0f, chargeSpeed * Time.deltaTime);
        }
        else
        {
            scanProgress = Mathf.MoveTowards(scanProgress, targetLevel, levelChangeSpeed * Time.deltaTime);
        }
    }

    private float GetBandLevel(float angle)
    {
        foreach (ScanBand band in angleBands)
        {
            if (angle <= band.maxAngle)
            {
                return band.level;
            }
        }

        return 0.0f;
    }

    private void CompleteScan()
    {
        isScanned = true;
        scanProgress = 1.0f;
        DestroyHudElement();
        onScanned.Invoke();
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

    private void DestroyHudElement()
    {
        if (hudElement != null)
        {
            Destroy(hudElement.gameObject);
        }

        hudElement = null;
    }
}
