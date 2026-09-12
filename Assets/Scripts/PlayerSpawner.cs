#nullable enable

using Unity.VisualScripting;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject? playerPrefab;
    private GameObject? player;
    public Vector3 spawnOffset = new Vector3(0, 2, 0);

    private GameObject? checkpointRoot = null;

    public void SetCheckpoint(GameObject? checkpoint)
    {
        checkpointRoot = checkpoint;
    }

    private void Start()
    {
        // spawn the player for the first time
        if (playerPrefab != null)
        {
            player = Instantiate(playerPrefab, transform.position + spawnOffset, transform.rotation);

            // register for death
            var entity = player.GetComponentInChildren<Entity>();
            if (entity != null)
            {
                entity.OnKilled += OnPlayerKilled;
            }

            FadeInFromDeath(player);
        }
    }

    private void OnDestroy()
    {
        var entity = player?.GetComponentInChildren<Entity>();
        if (entity != null)
        {
            entity.OnKilled -= OnPlayerKilled;
        }
    }

    void RespawnPlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 respawnPosition = transform.position;
        Quaternion respawnRotation = transform.rotation;

        if (checkpointRoot != null)
        {
            respawnPosition = checkpointRoot.transform.position;
            respawnRotation = checkpointRoot.transform.rotation;
        }

        respawnPosition += spawnOffset;

        player.transform.SetPositionAndRotation(respawnPosition, respawnRotation);

        if (player.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (player.TryGetComponent<Health>(out Health health))
        {
            health.ResetHealth();
        }

        if (player.TryGetComponent<Modifiers>(out Modifiers modifiers))
        {
            modifiers.ResetModifier(ModifierType.Oxygen);
        }

        if (player.TryGetComponent<Entity>(out Entity entity))
        {
            entity.Respawn();
        }

        FadeInFromDeath(player);
    }

    private async void FadeInFromDeath(GameObject player)
    {
        if (player.TryGetComponent<ScreenFader>(out ScreenFader screenFader))
        {
            // fades in from black, with an initial delay
            screenFader.SetOpacity(1.0f);
            await screenFader.FadeToOpacity(1.0f, 1.0f);
            _ = screenFader.FadeToOpacity(0.0f, 8.0f);
        }
    }

    public void OnPlayerKilled(DamageEvent damageEvent)
    {
        RespawnPlayer();
    }
}
