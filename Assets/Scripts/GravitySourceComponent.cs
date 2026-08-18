#nullable enable

using System.Collections;
using UnityEngine;

public abstract class GravitySourceComponent : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float G_multiplier = 1.0f;
    public int priority = 0;

    // @todo trae - move global gravity somewhere else
    public float defaultGravity = -10.0f;

    public bool isGravityEnabled = true;
    public bool isMagnetized = true;

    private Coroutine? disableCoroutine;

    public abstract Vector3 GetGravityVector(Vector3 point);

    public abstract void Update();

    // raw geometric distance from a point to this source's surface; used by FirstPersonController
    // to drive proximity-based audio generically across every source type. Defaults to "no surface"
    // for sources that don't represent a physical surface to approach.
    public virtual float GetDistanceToSurface(Vector3 point)
    {
        return float.PositiveInfinity;
    }

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

    public virtual void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterInternal(other.gameObject);
    }

    public virtual void OnTriggerExit(Collider other)
    {
        OnTriggerExitInternal(other.gameObject);
    }

    protected void OnTriggerEnterInternal(GameObject gameObject)
    {
        if (gameObject.TryGetComponent(out Entity entity) && !entity.IsAlive)
        {
            return;
        }

        if (gameObject.TryGetComponent(out GravityController gravityController))
        {
            gravityController.AddGravitySource(this);
        }
    }

    protected void OnTriggerExitInternal(GameObject gameObject)
    {
        if (gameObject.TryGetComponent(out Entity entity) && !entity.IsAlive)
        {
            return;
        }

        if (gameObject.TryGetComponent(out GravityController gravityController))
        {
            gravityController.RemoveGravitySource(this);
        }
    }
}
