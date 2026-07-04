#nullable enable

using UnityEngine;

public class BreadcrumbAnchor : MonoBehaviour
{
    private GameObject? anchor;

    public static void Attach(Transform breadcrumb, Transform source)
    {
        var anchor = new GameObject("BreadcrumbAnchor");
        anchor.transform.SetParent(source, false);
        Vector3 sourceScale = source.lossyScale;
        anchor.transform.localScale = new Vector3(1f / sourceScale.x, 1f / sourceScale.y, 1f / sourceScale.z);

        breadcrumb.SetParent(anchor.transform, true);
        breadcrumb.gameObject.AddComponent<BreadcrumbAnchor>().anchor = anchor;
    }

    private void OnDestroy()
    {
        if (anchor != null)
        {
            Destroy(anchor);
        }
    }
}
