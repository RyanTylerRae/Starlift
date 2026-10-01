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
        // no trail while walking on a magnetized surface - keep the spawn point following us so the
        // trail resumes a full delayDistance after leaving it, rather than dropping one at launch
        GravitySourceComponent? activeSource = gravityController?.GetActiveGravitySource();
        if (activeSource != null && activeSource.isMagnetized)
        {
            lastSpawnPos = gameObject.transform.position;
            return;
        }

        Vector3 vec = gameObject.transform.position - lastSpawnPos;
        if (vec.sqrMagnitude < delayDistance * delayDistance)
        {
            return;
        }

        if (breadcrumbGameObject == null)
        {
            return;
        }

        GameObject.Instantiate(breadcrumbGameObject, gameObject.transform.position, gameObject.transform.rotation);

        lastSpawnPos = gameObject.transform.position;
    }
}
