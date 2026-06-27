#nullable enable

using UnityEngine;

[ExecuteAlways]
public class HexColorToMaterial : MonoBehaviour
{
    public string colorProperty = "_BaseColor";
    public string hexColor = "#FFFFFF";

    private Renderer[]? renderers = null;
    private MaterialPropertyBlock? block = null;

    void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Apply()
    {
        if (renderers == null || block == null)
        {
            return;
        }

        if (!ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            return;
        }

        foreach (var r in renderers)
        {
            if (!r)
            {
                continue;
            }

            r.GetPropertyBlock(block);
            block.SetColor(colorProperty, color);
            r.SetPropertyBlock(block);
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }
}
