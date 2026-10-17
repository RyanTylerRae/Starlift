#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shared helpers for building 3D HUD elements in code (WorldHUDElement, AudiologHUD), so every
/// element's text, bar quads and lines are set up the same way.
/// </summary>
public static class HUDBuilder
{
    public static GameObject CreateChild(string childName, Transform parent, int layer)
    {
        GameObject child = new GameObject(childName);
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    // single-line 3D text that never wraps; position and size are for its RectTransform around the given pivot.
    // Leave font empty to use TMP's default font
    public static TextMeshPro CreateText(string textName, Transform parent, int layer, TMP_FontAsset? font, float fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 pivot, Vector2 size, Vector3 localPosition)
    {
        GameObject textObject = CreateChild(textName, parent, layer);
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        if (font != null)
        {
            text.font = font;
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
    public static MeshRenderer CreateBarQuad(string quadName, Transform parent, Vector2 size, float depthOffset, Material? material, int sortingOrder)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;
        quad.layer = parent.gameObject.layer;

        if (quad.TryGetComponent(out Collider quadCollider))
        {
            Object.Destroy(quadCollider);
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

    // a line drawn flat in the parent's XY plane, in local space, so it faces the HUD camera like the quads
    public static LineRenderer CreateLine(string lineName, Transform parent, int layer, float width, Material? material, int sortingOrder)
    {
        GameObject lineObject = CreateChild(lineName, parent, layer);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.alignment = LineAlignment.TransformZ;
        line.widthMultiplier = width;
        line.numCornerVertices = 2;
        line.sharedMaterial = material;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = sortingOrder;
        line.positionCount = 0;
        return line;
    }
}
