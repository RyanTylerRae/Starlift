#nullable enable

using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject? playerPrefab;
    public Vector3 spawnOffset = new Vector3(0, 2, 0);

    private void Start()
    {
        SpawnPlayer();
    }

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
