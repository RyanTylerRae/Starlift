#nullable enable

using UnityEngine;

/// <summary>
/// A collectible the player picks up by walking into it. Needs a trigger collider.
/// On pickup it plays the player's ScreenJellyFlash (white edge flash) and, optionally, hides itself.
/// Collects only once.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleComponent : MonoBehaviour
{
    public bool hideOnCollect = true;

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!target.TryGetComponent(out FirstPersonController _))
        {
            return;
        }

        isCollected = true;

        // the flash lives on the player, so it keeps running after this object hides
        ScreenJellyFlash? flash = target.GetComponentInChildren<ScreenJellyFlash>();
        if (flash != null)
        {
            flash.Play();
        }

        if (hideOnCollect)
        {
            gameObject.SetActive(false);
        }
    }
}
