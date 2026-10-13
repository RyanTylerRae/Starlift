#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A 3D HUD element that tracks a world object: a centred label above a progress bar framed by [ ] brackets.
/// Built entirely in code by WorldHUDProjector.CreateElement - size, font and materials come from the
/// projector so every element looks the same. Positions itself each frame through the projector.
/// </summary>
public class WorldHUDElement : MonoBehaviour
{
    public enum OffScreenMode
    {
        Hide,
        ClampToEdge,
    }

    public OffScreenMode offScreenMode = OffScreenMode.Hide;

    // world-space offset applied to the target's bounds before the element is placed below them
    public Vector3 worldOffset = Vector3.zero;

    private WorldHUDProjector? projector;
    private Transform? target;
    private GameObject? visuals;
    private GameObject? bar;
    private TextMeshPro? label;
    private TextMeshPro? leftBracket;
    private Material? fillMaterial;

    // what the target's bounds are measured from: its meshes, or its colliders if it has no meshes
    private Renderer[] targetRenderers = System.Array.Empty<Renderer>();
    private Collider[] targetColliders = System.Array.Empty<Collider>();

    public void Build(WorldHUDProjector owner, Transform trackedTarget)
    {
        projector = owner;
        target = trackedTarget;

        // only mesh renderers describe the object's shape - particles, trails and lines would inflate it
        targetRenderers = System.Array.FindAll(trackedTarget.GetComponentsInChildren<Renderer>(true),
            (Renderer targetRenderer) => { return targetRenderer is MeshRenderer || targetRenderer is SkinnedMeshRenderer; });
        targetColliders = trackedTarget.GetComponentsInChildren<Collider>(true);

        int layer = owner.gameObject.layer;

        // starts hidden until the first placement in LateUpdate
        visuals = CreateChild("Visuals", transform, layer);
        visuals.SetActive(false);

        // the element's origin is the centre of the bar
        bar = CreateChild("Bar", visuals.transform, layer);
        Vector2 barSize = new Vector2(owner.barWidth, owner.barHeight);

        CreateBarQuad("Background", bar.transform, barSize, 0.0f, owner.barBackgroundMaterial, 0);

        // the ProgressBar shader is built on ShaderGraph's Canvas sub-target, which doesn't reliably
        // depth-sort, so draw order is forced with sortingOrder as well as a small step toward the camera
        MeshRenderer fillRenderer = CreateBarQuad("Fill", bar.transform, barSize, -0.001f, owner.barFillMaterial, 1);
        if (owner.barFillMaterial != null)
        {
            fillMaterial = fillRenderer.material;
        }

        // brackets belong to the bar so they hide with it. Midline alignment centres on the glyph's own
        // bounds rather than the font's line height, so each bracket sits level with the bar
        float bracketOffsetX = owner.barWidth * 0.5f + owner.bracketGap;
        Vector2 bracketSize = new Vector2(owner.barHeight, owner.barHeight);

        leftBracket = CreateText("LeftBracket", bar.transform, layer, owner, owner.bracketFontSize, owner.bracketColor,
            TextAlignmentOptions.MidlineRight, new Vector2(1.0f, 0.5f), bracketSize, new Vector3(-bracketOffsetX, 0.0f, -0.002f));
        leftBracket.text = "[";

        TextMeshPro rightBracket = CreateText("RightBracket", bar.transform, layer, owner, owner.bracketFontSize, owner.bracketColor,
            TextAlignmentOptions.MidlineLeft, new Vector2(0.0f, 0.5f), bracketSize, new Vector3(bracketOffsetX, 0.0f, -0.002f));
        rightBracket.text = "]";

        // bottom-centre of the label sits over the bar's centre, labelGap above its top
        label = CreateText("Label", visuals.transform, layer, owner, owner.fontSize, owner.labelColor,
            TextAlignmentOptions.Bottom, new Vector2(0.5f, 0.0f), barSize, new Vector3(0.0f, owner.barHeight * 0.5f + owner.labelGap, -0.002f));
        label.text = string.Empty;

        SetProgress(0.0f);
    }

    public void SetText(string text)
    {
        if (label == null)
        {
            return;
        }

        label.text = text;
        label.enabled = !string.IsNullOrEmpty(text);
    }

    public void SetProgress(float progress)
    {
        if (fillMaterial != null)
        {
            fillMaterial.SetFloat("_Progress", Mathf.Clamp01(progress));
        }
    }

    public void SetBarVisible(bool visible)
    {
        if (bar != null)
        {
            bar.SetActive(visible);
        }
    }

    // LateUpdate so the player camera has finished moving for the frame
    public void LateUpdate()
    {
        if (projector == null || visuals == null)
        {
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Bounds targetBounds = GetTargetBounds(target);
        targetBounds.center += worldOffset;

        bool isVisible = projector.TryGetHudPlacement(targetBounds, GetTopExtent(projector), offScreenMode, out Vector3 hudPosition, out Quaternion hudRotation, out float scale);
        if (isVisible)
        {
            transform.SetPositionAndRotation(hudPosition, hudRotation);
            transform.localScale = Vector3.one * scale;
        }

        if (visuals.activeSelf != isVisible)
        {
            visuals.SetActive(isVisible);
        }
    }

    // world-space AABB of the target's active meshes, falling back to its colliders, then to its pivot.
    // Recomputed every frame since the target can move, rotate or animate
    private Bounds GetTargetBounds(Transform trackedTarget)
    {
        Bounds bounds = new Bounds(trackedTarget.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer != null && targetRenderer.enabled && targetRenderer.gameObject.activeInHierarchy)
            {
                if (hasBounds)
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
                else
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        foreach (Collider targetCollider in targetColliders)
        {
            if (targetCollider != null && targetCollider.enabled && targetCollider.gameObject.activeInHierarchy)
            {
                if (hasBounds)
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
                else
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
            }
        }

        return bounds;
    }

    // how far the element reaches above its origin (the bar's centre), in its own unscaled units
    private float GetTopExtent(WorldHUDProjector owner)
    {
        float topExtent = 0.0f;

        if (bar != null && bar.activeSelf)
        {
            topExtent = owner.barHeight * 0.5f;
            if (leftBracket != null)
            {
                topExtent = Mathf.Max(topExtent, leftBracket.preferredHeight * 0.5f);
            }
        }

        if (label != null && label.enabled)
        {
            topExtent = Mathf.Max(topExtent, owner.barHeight * 0.5f + owner.labelGap + label.preferredHeight);
        }

        return topExtent;
    }

    public void OnDestroy()
    {
        if (fillMaterial != null)
        {
            Destroy(fillMaterial);
        }
    }

    private static GameObject CreateChild(string childName, Transform parent, int layer)
    {
        GameObject child = new GameObject(childName);
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    // single-line 3D text that never wraps; position and size are for its RectTransform around the given pivot
    private static TextMeshPro CreateText(string textName, Transform parent, int layer, WorldHUDProjector owner, float fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 pivot, Vector2 size, Vector3 localPosition)
    {
        GameObject textObject = CreateChild(textName, parent, layer);
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        if (owner.font != null)
        {
            text.font = owner.font;
        }

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.sortingOrder = 2;

        RectTransform textRect = text.rectTransform;
        textRect.pivot = pivot;
        textRect.sizeDelta = size;
        textRect.localPosition = localPosition;
        return text;
    }

    // a quad faces the HUD camera when given the camera's rotation, and its UVs run left to right on screen
    private static MeshRenderer CreateBarQuad(string quadName, Transform parent, Vector2 size, float depthOffset, Material? material, int sortingOrder)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;
        quad.layer = parent.gameObject.layer;

        if (quad.TryGetComponent(out Collider quadCollider))
        {
            Destroy(quadCollider);
        }

        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, depthOffset);
        quad.transform.localScale = new Vector3(size.x, size.y, 1.0f);

        MeshRenderer quadRenderer = quad.GetComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = material;
        quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        quadRenderer.sortingOrder = sortingOrder;
        return quadRenderer;
    }
}
