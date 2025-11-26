using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class OxygenPocket : MonoBehaviour
{
    public float replenishRate = 10f;

    private SphereCollider sphereCollider;
    private GameObject playerInPocket;
    private OxygenSystem playerOxygenSystem;

    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
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
    }

    private void OnPlayerExitPocket(GameObject player)
    {
        playerOxygenSystem = null;
        Debug.Log("Player exited oxygen pocket - stopping oxygen replenishment");
    }
}
