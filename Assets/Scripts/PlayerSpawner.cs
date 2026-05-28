#nullable enable

using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject? playerPrefab;
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
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            Vector3 basePosition = checkpointRoot != null
                ? checkpointRoot.TransformPoint(checkpointLocalPosition)
                : transform.position;
            Vector3 spawnPosition = basePosition + spawnOffset;
            var player = Instantiate(playerPrefab, spawnPosition, transform.rotation);
            var entity = player.GetComponentInChildren<Entity>();
            if (entity != null)
            {
                entity.OnKilled += OnPlayerKilled;
            }

            if (player.TryGetComponent(out ScreenFader screenFader))
            {
                screenFader.SetOpacity(1.0f);
                _ = screenFader.FadeToOpacity(0.0f, 8.0f);
            }
        }
    }

    public void OnPlayerKilled(DamageEvent damageEvent)
    {
        SpawnPlayer();
    }
}
