#nullable enable

using Unity.VisualScripting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerSpawner : MonoBehaviour
{
    public GameObject? playerPrefab;
    private GameObject? player;
    private Entity? playerEntity = null;
    public Vector3 spawnOffset = new Vector3(0, 2, 0);
    public Vector3 spawnLookDirection = Vector3.forward;
    public Vector3 spawnUpDirection = Vector3.up;
    public bool fadeInOnSpawn = true;

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
            player = Instantiate(playerPrefab, transform.position + spawnOffset, GetSpawnBodyRotation());

            if (player.TryGetComponent(out FirstPersonController controller))
            {
                controller.SnapToView(GetSpawnLookDirection(), GetSpawnUpDirection());
            }

            // register for death
            playerEntity = player.GetComponentInChildren<Entity>();
            if (playerEntity != null)
            {
                playerEntity.OnKilled += OnPlayerKilled;
            }

            if (fadeInOnSpawn)
            {
                FadeInFromDeath(player);
            }
        }
    }

    private void OnDestroy()
    {
        if (playerEntity != null)
        {
            playerEntity.OnKilled -= OnPlayerKilled;
        }
    }

    void RespawnPlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 respawnPosition = transform.position;
        Vector3 respawnLookDirection = GetSpawnLookDirection();
        Vector3 respawnUpDirection = GetSpawnUpDirection();

        if (checkpointRoot != null)
        {
            respawnPosition = checkpointRoot.transform.position;
            respawnLookDirection = checkpointRoot.transform.forward;
            respawnUpDirection = checkpointRoot.transform.up;
        }

        respawnPosition += spawnOffset;

        player.transform.position = respawnPosition;

        if (player.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (player.TryGetComponent(out FirstPersonController controller))
        {
            controller.SnapToView(respawnLookDirection, respawnUpDirection);
        }
        else
        {
            player.transform.rotation = Quaternion.LookRotation(respawnLookDirection, respawnUpDirection);
        }

        if (player.TryGetComponent<Health>(out Health health))
        {
            health.ResetHealth();
        }

        if (player.TryGetComponent<OxygenSystem>(out OxygenSystem oxygenSystem))
        {
            oxygenSystem.ReplenishOxygen(float.PositiveInfinity);
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

            if (screenFader == null)
            {
                return;
            }

            _ = screenFader.FadeToOpacity(0.0f, 8.0f);
        }
    }

    public void OnPlayerKilled(DamageEvent damageEvent)
    {
        RespawnPlayer();
    }

    public Vector3 GetSpawnLookDirection()
    {
        Vector3 localLook = spawnLookDirection.sqrMagnitude > 0.0001f ? spawnLookDirection : Vector3.forward;
        return transform.TransformDirection(localLook).normalized;
    }

    public Vector3 GetSpawnUpDirection()
    {
        Vector3 localUp = spawnUpDirection.sqrMagnitude > 0.0001f ? spawnUpDirection : Vector3.up;
        return transform.TransformDirection(localUp).normalized;
    }

    private Quaternion GetSpawnBodyRotation()
    {
        Vector3 upDirection = GetSpawnUpDirection();
        Vector3 bodyForward = Vector3.ProjectOnPlane(GetSpawnLookDirection(), upDirection);
        if (bodyForward.sqrMagnitude < 0.0001f)
        {
            bodyForward = Vector3.ProjectOnPlane(transform.forward, upDirection);
        }

        return Quaternion.LookRotation(bodyForward, upDirection);
    }

#if UNITY_EDITOR
    private const float gizmoArrowLength = 1.0f;

    private void OnDrawGizmos()
    {
        Vector3 lookDirection = GetSpawnLookDirection();
        Vector3 upDirection = GetSpawnUpDirection();
        Quaternion bodyRotation = GetSpawnBodyRotation();

        Vector3 spawnPosition = transform.position + spawnOffset;
        Vector3 eyePosition = spawnPosition + bodyRotation * GetPrefabEyeOffset();

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPosition, 0.1f);
        Gizmos.DrawLine(spawnPosition, eyePosition);

        Handles.color = Color.green;
        Handles.ArrowHandleCap(0, spawnPosition, Quaternion.LookRotation(upDirection, bodyRotation * Vector3.back), gizmoArrowLength, EventType.Repaint);

        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, eyePosition, Quaternion.LookRotation(lookDirection, upDirection), gizmoArrowLength, EventType.Repaint);
    }

    private Vector3 GetPrefabEyeOffset()
    {
        if (playerPrefab == null || !playerPrefab.TryGetComponent(out FirstPersonController controller) || controller.cameraArm == null)
        {
            return Vector3.zero;
        }

        Transform root = playerPrefab.transform;
        return Quaternion.Inverse(root.rotation) * (controller.cameraArm.transform.position - root.position);
    }
#endif
}
