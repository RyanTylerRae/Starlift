#nullable enable

using Unity;
using UnityEngine;

public class BreadcrumbController : MonoBehaviour
{
    public GameObject breadcrumbGameObject;

    public float delayDistance;
    private Vector3 lastSpawnPos;

    public void Start()
    {
        lastSpawnPos = gameObject.transform.position;
    }

    public void Update()
    {
        Vector3 vec = gameObject.transform.position - lastSpawnPos;
        if (vec.sqrMagnitude < delayDistance * delayDistance)
        {
            return;
        }

        GameObject.Instantiate(breadcrumbGameObject, gameObject.transform.position, gameObject.transform.rotation);
        lastSpawnPos = gameObject.transform.position;
    }
}
