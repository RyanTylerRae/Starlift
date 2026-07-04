#nullable enable

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OxygenPocket : MonoBehaviour
{
    public float replenishRate = 10f;

    public GameObject? checkpointRoot;

    private Collider? volumeCollider;
    private GameObject? playerInPocket;
    private OxygenSystem? playerOxygenSystem;

    void Start()
    {
        volumeCollider = GetComponent<Collider>();
        if (volumeCollider != null)
        {
            volumeCollider.isTrigger = true;
        }
    }

    void Update()
    {
        if (playerInPocket != null && playerOxygenSystem != null)
        {
            playerOxygenSystem.ReplenishOxygen(replenishRate);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInPocket = other.gameObject;
            OnPlayerEnterPocket(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject == playerInPocket)
        {
            OnPlayerExitPocket(other.gameObject);
            playerInPocket = null;
        }
    }

    private void OnPlayerEnterPocket(GameObject player)
    {
        playerOxygenSystem = player.GetComponent<OxygenSystem>();
        if (playerOxygenSystem == null)
        {
            Debug.LogWarning("Player entered oxygen pocket but has no OxygenSystem component");
        }
        else
        {
            AkUnitySoundEngine.PostEvent("play_oxygen_replenish", gameObject);
            Debug.Log("Player entered oxygen pocket - replenishing oxygen");
        }

        Object.FindFirstObjectByType<PlayerSpawner>()?.SetCheckpoint(checkpointRoot);

        if (player.TryGetComponent<SaveRadialIndicator>(out var saveRadialIndicator))
        {
            saveRadialIndicator.TriggerSave();
        }
    }

    private void OnPlayerExitPocket(GameObject player)
    {
        playerOxygenSystem = null;
        Debug.Log("Player exited oxygen pocket - stopping oxygen replenishment");
    }
}
