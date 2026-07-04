#nullable enable

using Unity;
using UnityEngine;

public class BreadcrumbController : MonoBehaviour
{
    public GameObject? breadcrumbGameObject;

    public float delayDistance;
    private Vector3 lastSpawnPos = Vector3.zero;
    private GravityController? gravityController;

    public void Start()
    {
        lastSpawnPos = gameObject.transform.position;
        gravityController = GetComponent<GravityController>();
    }

    public void Update()
    {
        Vector3 vec = gameObject.transform.position - lastSpawnPos;
        if (vec.sqrMagnitude < delayDistance * delayDistance)
        {
            return;
        }

        if (breadcrumbGameObject == null)
        {
            return;
        }

        GameObject breadcrumb = GameObject.Instantiate(breadcrumbGameObject, gameObject.transform.position, gameObject.transform.rotation);

        // grounded breadcrumbs
        GravitySourceComponent? activeSource = gravityController?.GetActiveGravitySource();
        if (activeSource != null && activeSource.isMagnetized)
        {
            BreadcrumbAnchor.Attach(breadcrumb.transform, activeSource.transform);
        }

        lastSpawnPos = gameObject.transform.position;
    }
}
