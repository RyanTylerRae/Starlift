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
    private FirstPersonController? playerController;

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
        if (playerInPocket != null && playerOxygenSystem != null && (playerController == null || playerController.OxygenBurnRate <= 0.0f))
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
        Entity playerEntity = player.GetComponent<Entity>();

        if (playerOxygenSystem == null || (playerEntity != null && !playerEntity.IsAlive))
        {
            Debug.LogWarning("Player entered oxygen pocket but is dead");
            return;
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

        AkUnitySoundEngine.SetRTPCValue("SpaceVacuum", 0.0f);
    }

    private void OnPlayerExitPocket(GameObject player)
    {
        playerOxygenSystem = null;
        playerController = null;
        Debug.Log("Player exited oxygen pocket - stopping oxygen replenishment");

        AkUnitySoundEngine.SetRTPCValue("SpaceVacuum", 100.0f);
    }
}
