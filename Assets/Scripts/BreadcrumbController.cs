#nullable enable

using Unity;
using UnityEngine;

public class BreadcrumbController : MonoBehaviour
{
    public GameObject? breadcrumbGameObject;

    public float delayDistance;
    private Vector3 lastSpawnPos;
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

        GravitySourceComponent? activeSource = gravityController?.GetActiveGravitySource();
        if (activeSource != null && activeSource.isMagnetized)
        {
            breadcrumb.transform.SetParent(activeSource.transform, true);
        }

        lastSpawnPos = gameObject.transform.position;
    }
}
