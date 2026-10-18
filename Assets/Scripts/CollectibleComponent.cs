#nullable enable

using UnityEngine;

/// <summary>
/// A collectible the player picks up by walking into it. Needs a trigger collider.
/// On pickup it plays the player's ScreenJellyFlash (white edge flash) and, optionally, hides itself.
/// Collects only once. With a collectibleId, the pickup is saved and the collectible stays hidden on later loads.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleComponent : MonoBehaviour
{
    public bool hideOnCollect = true;
    public string collectibleId = "";

    private bool isCollected = false;

    public bool IsCollected { get { return isCollected; } }

    private void Start()
    {
        if (string.IsNullOrEmpty(collectibleId))
        {
            Debug.LogWarning($"{name} has no collectibleId, so its pickup won't be saved", this);
            return;
        }

        if (GameState.Instance.IsCollectibleFound(collectibleId))
        {
            isCollected = true;
            gameObject.SetActive(false);
        }
    }

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

        if (!string.IsNullOrEmpty(collectibleId))
        {
            GameState.Instance.MarkCollectibleFound(collectibleId);
        }

        // the flash lives on the player, so it keeps running after this object hides
        ScreenJellyFlash? flash = target.GetComponentInChildren<ScreenJellyFlash>();
        if (flash != null)
        {
            flash.Play();
        }

        CollectibleCounter? counter = target.GetComponentInChildren<CollectibleCounter>();
        if (counter != null)
        {
            counter.Show();
        }

        if (hideOnCollect)
        {
            gameObject.SetActive(false);
        }
    }
}
