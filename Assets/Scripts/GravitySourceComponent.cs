#nullable enable

using System.Collections;
using UnityEngine;

public abstract class GravitySourceComponent : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float G_multiplier = 1.0f;

    // @todo trae - move global gravity somewhere else
    public float defaultGravity = -10.0f;

    public bool isGravityEnabled = true;
    public bool isMagnetized = true;

    private Coroutine? disableCoroutine;

    public abstract Vector3 GetGravityVector(Vector3 point);

    public abstract void Update();

    public void DisableForSeconds(float seconds)
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
        }

        disableCoroutine = StartCoroutine(DisableCoroutine(seconds));
    }

    private IEnumerator DisableCoroutine(float seconds)
    {
        isGravityEnabled = false;
        yield return new WaitForSeconds(seconds);
        isGravityEnabled = true;
        disableCoroutine = null;
    }

    public void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterInternal(other.gameObject);
    }

    public void OnTriggerExit(Collider other)
    {
        OnTriggerExitInternal(other.gameObject);
    }

    protected void OnTriggerEnterInternal(GameObject gameObject)
    {
        Debug.Log($"GravityComponent on {gameObject.name}: {gameObject.name} entered trigger");

        GravityController gravityController = gameObject.GetComponent<GravityController>();
        if (gravityController != null)
        {
            gravityController.AddGravitySource(this);
        }
    }

    protected void OnTriggerExitInternal(GameObject gameObject)
    {
        Debug.Log($"GravityComponent on {gameObject.name}: {gameObject.name} exited trigger");

        GravityController gravityController = gameObject.GetComponent<GravityController>();
        if (gravityController != null)
        {
            gravityController.RemoveGravitySource(this);
        }
    }
}
