#nullable enable

using TMPro;
using UnityEngine;

/// <summary>
/// Places WorldHUDElements on the 3D HUD over the world objects they track. Lives on its own HUD camera
/// (3D_HUD_World_Camera) that renders only its layer to a separate render texture, so the elements can
/// fade independently of the main HUD. The HUD camera sits still somewhere in the world, so a target's
/// world position means nothing to it: targets are projected through the player camera first, corrected
/// for the HUD's fisheye pass, and only the resulting screen position is carried over onto a plane in
/// front of the HUD camera. Elements use this object's layer. Also holds the shared layout so every
/// element is built at the same size.
/// </summary>
[RequireComponent(typeof(Camera))]
public class WorldHUDProjector : MonoBehaviour
{
    [Header("Placement")]
    // distance in front of the HUD camera that elements sit at - matches the rest of the HUD
    public float planeDistance = 10.0f;

    // elements are kept this far inside the screen, as a fraction of the screen
    [Range(0f, 0.45f)]
    public float edgeMargin = 0.08f;

    // space between the bottom of the object on screen and the top of its element, in HUD units
    public float gapBelowObject = 0.1f;

    [Header("Distance Scaling")]
    // distance from the player camera at which elements are drawn at scale 1
    public float referenceDistance = 5.0f;
    public float minScale = 0.4f;
    public float maxScale = 1.5f;

    [Header("Layout")]
    public float barWidth = 2.0f;
    public float barHeight = 0.12f;
    public float fontSize = 3.0f;

    // space between the top of the bar and the bottom of the label
    public float labelGap = 0.05f;
    public Color labelColor = Color.white;

    // text brackets either side of the bar, sized to fit the bar inside them
    public float bracketFontSize = 2.5f;

    // space between each end of the bar and its bracket
    public float bracketGap = 0.03f;
    public Color bracketColor = Color.white;

    // leave empty to use TMP's default font
    public TMP_FontAsset? font;

    [Header("Bar Materials")]
    // optional full-width quad behind the fill - leave empty to let the brackets alone frame the bar.
    // Needs a colour clearly different from the fill, or progress won't read
    public Material? barBackgroundMaterial;
    public Material? barFillMaterial;

    private Camera? hudCamera;
    private FisheyeCamera? fisheyeCamera;
    private Camera? playerCamera;
    private Transform? elementRoot;

    public void Awake()
    {
        hudCamera = GetComponent<Camera>();
        fisheyeCamera = GetComponent<FisheyeCamera>();

        // elements hang directly off the camera rather than LookRoot, so mouse-look sway
        // doesn't pull them off their targets
        GameObject root = new GameObject("WorldElements");
        root.layer = gameObject.layer;
        root.transform.SetParent(transform, false);
        elementRoot = root.transform;
    }

    public WorldHUDElement CreateElement(Transform target)
    {
        GameObject elementObject = new GameObject($"WorldHUDElement_{target.name}");
        elementObject.layer = gameObject.layer;
        elementObject.transform.SetParent(elementRoot != null ? elementRoot : transform, false);

        WorldHUDElement element = elementObject.AddComponent<WorldHUDElement>();
        element.Build(this, target);
        return element;
    }

    // works out where on the HUD an element tracking an object with the given world bounds should sit:
    // centred under the object's on-screen bounding box, with the element's top (elementTopExtent above
    // its origin, in element units) clear of the object. Returns false when the element should be hidden
    // (object off-screen in Hide mode, or no player camera yet)
    public bool TryGetHudPlacement(Bounds worldBounds, float elementTopExtent, WorldHUDElement.OffScreenMode mode, out Vector3 hudPosition, out Quaternion hudRotation, out float scale)
    {
        hudPosition = Vector3.zero;
        hudRotation = Quaternion.identity;
        scale = 1.0f;

        if (hudCamera == null)
        {
            return false;
        }

        // the player camera isn't part of the player hierarchy - FirstPersonController creates it on a
        // separate CameraFollow object at runtime - so it has to come from the controller
        if (playerCamera == null)
        {
            playerCamera = StarliftStatics.FindFirstPersonController()?.playerCamera;
            if (playerCamera == null)
            {
                return false;
            }
        }

        // everything up to the screen position is worked out in the player camera's space
        Vector3 cameraSpaceCenter = playerCamera.transform.InverseTransformPoint(worldBounds.center);
        bool isInFront = cameraSpaceCenter.z > 0.0f;

        scale = Mathf.Clamp(referenceDistance / Mathf.Max(cameraSpaceCenter.magnitude, 0.0001f), minScale, maxScale);

        // screen-space bounding box of the object, in viewport units: project all 8 corners of its world
        // bounds. Projection breaks down for corners behind the camera, so if any are, fall back to the
        // bounds' centre as a single point
        Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 screenMax = new Vector2(float.MinValue, float.MinValue);
        bool areAllCornersInFront = true;
        for (int i = 0; i < 8; ++i)
        {
            Vector3 cornerDirection = new Vector3((i & 1) == 0 ? -1.0f : 1.0f, (i & 2) == 0 ? -1.0f : 1.0f, (i & 4) == 0 ? -1.0f : 1.0f);
            Vector3 corner = worldBounds.center + Vector3.Scale(worldBounds.extents, cornerDirection);
            Vector3 cornerViewport = playerCamera.WorldToViewportPoint(corner);
            if (cornerViewport.z <= 0.0f)
            {
                areAllCornersInFront = false;
                break;
            }

            screenMin = Vector2.Min(screenMin, cornerViewport);
            screenMax = Vector2.Max(screenMax, cornerViewport);
        }

        if (!areAllCornersInFront)
        {
            Vector3 centerViewport = playerCamera.WorldToViewportPoint(worldBounds.center);
            screenMin = centerViewport;
            screenMax = centerViewport;
        }

        bool isObjectOnScreen = isInFront && screenMax.x >= 0.0f && screenMin.x <= 1.0f && screenMax.y >= 0.0f && screenMin.y <= 1.0f;
        if (mode == WorldHUDElement.OffScreenMode.Hide && !isObjectOnScreen)
        {
            return false;
        }

        // drop the element's origin below the bottom of the box by its own top extent (scaled) plus the gap,
        // converted from HUD units on the element plane to viewport units
        float hudPlaneHeight = 2.0f * planeDistance * Mathf.Tan(hudCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float dropBelowObject = (gapBelowObject + elementTopExtent * scale) / hudPlaneHeight;

        // offset from the centre of the screen, in viewport units (-0.5..0.5 when on screen)
        Vector2 screenOffset = new Vector2((screenMin.x + screenMax.x) * 0.5f - 0.5f, screenMin.y - dropBelowObject - 0.5f);
        float halfExtent = Mathf.Max(0.5f - edgeMargin, 0.0001f);

        if (isObjectOnScreen)
        {
            // keep the element readable when the object is near the edge of the screen
            screenOffset.x = Mathf.Clamp(screenOffset.x, -halfExtent, halfExtent);
            screenOffset.y = Mathf.Clamp(screenOffset.y, -halfExtent, halfExtent);
        }
        else
        {
            if (!isInFront)
            {
                // the projection mirrors points behind the camera, so take the direction straight from
                // the camera-space position instead (x divided by aspect to match viewport units)
                screenOffset = new Vector2(cameraSpaceCenter.x / playerCamera.aspect, cameraSpaceCenter.y);
                if (screenOffset.sqrMagnitude < 0.000001f)
                {
                    screenOffset = Vector2.down;
                }
            }

            // shrink the offset uniformly until it sits on the edge of the margin box; uniform scaling
            // keeps the on-screen direction to the target intact
            float overshoot = Mathf.Max(Mathf.Abs(screenOffset.x), Mathf.Abs(screenOffset.y)) / halfExtent;
            if (!isInFront || overshoot > 1.0f)
            {
                screenOffset /= overshoot;
            }
        }

        // the HUD's fisheye pass shows the HUD pixel at  o * (1 + k * |o|^2)  at screen offset o
        // (see FisheyeEffect.shader), so placing the element there lands it on the target once distorted
        if (fisheyeCamera != null)
        {
            float distanceSquared = screenOffset.sqrMagnitude;
            screenOffset.x *= 1.0f + fisheyeCamera.horizontalDistortionStrength * distanceSquared;
            screenOffset.y *= 1.0f + fisheyeCamera.verticalDistortionStrength * distanceSquared;
        }

        // only now move into the HUD camera's space
        hudPosition = hudCamera.ViewportToWorldPoint(new Vector3(screenOffset.x + 0.5f, screenOffset.y + 0.5f, planeDistance));
        hudRotation = hudCamera.transform.rotation;
        return true;
    }
}
