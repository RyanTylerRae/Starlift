#nullable enable

using Coherence.Toolkit;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Vector3 spawnOffset = new Vector3(0, 2, 0);

    private CoherenceBridge? coherenceBridge;

    private void Awake()
    {
        coherenceBridge = FindFirstObjectByType<CoherenceBridge>();
        if (coherenceBridge != null)
        {
            coherenceBridge.onConnected.AddListener(OnConnection);
        }
        else
        {
            SpawnPlayer();
        }
    }

    private void OnConnection(CoherenceBridge bridge) => SpawnPlayer();

    void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            var player = Instantiate(playerPrefab, transform.position + spawnOffset, transform.rotation);
            var entity = player.GetComponentInChildren<Entity>();
            if (entity != null)
            {
                entity.OnKilled += OnPlayerKilled;
            }
        }
    }

    public void OnPlayerKilled(DamageEvent damageEvent)
    {
        SpawnPlayer();
    }
}
