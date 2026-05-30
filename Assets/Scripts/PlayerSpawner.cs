#nullable enable

using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject? playerPrefab;
    private GameObject? player;
    public Vector3 spawnOffset = new Vector3(0, 2, 0);

    private Transform? checkpointRoot;
    private Vector3 checkpointLocalPosition;

    public void SetCheckpoint(Transform pocketTransform)
    {
        checkpointRoot = pocketTransform.root;
        checkpointLocalPosition = checkpointRoot.InverseTransformPoint(pocketTransform.position);
    }

    private void Start()
    {
        // spawn the player for the first time
        if (playerPrefab != null)
        {
            Vector3 basePosition = checkpointRoot != null
                ? checkpointRoot.TransformPoint(checkpointLocalPosition)
                : transform.position;
            Vector3 spawnPosition = basePosition + spawnOffset;

            player = Instantiate(playerPrefab, spawnPosition, transform.rotation);

            // register for death
            var entity = player.GetComponentInChildren<Entity>();
            if (entity != null)
            {
                entity.OnKilled += OnPlayerKilled;
            }

            // fade in from black
            if (player.TryGetComponent<ScreenFader>(out ScreenFader screenFader))
            {
                screenFader.SetOpacity(1.0f);
                _ = screenFader.FadeToOpacity(0.0f, 8.0f);
            }
        }
    }

    void RespawnPlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 basePosition = checkpointRoot != null
            ? checkpointRoot.TransformPoint(checkpointLocalPosition)
            : transform.position;
        player.transform.SetPositionAndRotation(basePosition + spawnOffset, transform.rotation);

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

        if (player.TryGetComponent<ScreenFader>(out ScreenFader screenFader))
        {
            _ = screenFader.FadeToOpacity(0.0f, 8.0f);
        }
    }

    public void OnPlayerKilled(DamageEvent damageEvent)
    {
        RespawnPlayer();
    }
}
