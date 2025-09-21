using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Setup")]
    public GameObject playerPrefab;
    public Vector3 spawnOffset = new Vector3(0, 2, 0);

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            CreatePlayerFromScratch();
        }
        else
        {
            Instantiate(playerPrefab, transform.position + spawnOffset, transform.rotation);
        }
    }

    void CreatePlayerFromScratch()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = transform.position + spawnOffset;

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.center = new Vector3(0, 1, 0);

        GameObject cameraHolder = new GameObject("CameraHolder");
        cameraHolder.transform.SetParent(player.transform);
        cameraHolder.transform.localPosition = new Vector3(0, 1.7f, 0);

        Camera playerCamera = cameraHolder.AddComponent<Camera>();
        playerCamera.tag = "MainCamera";

        AudioListener audioListener = cameraHolder.AddComponent<AudioListener>();

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        FirstPersonController controller = player.AddComponent<FirstPersonController>();

        GameObject playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerBody.name = "PlayerBody";
        playerBody.transform.SetParent(player.transform);
        playerBody.transform.localPosition = new Vector3(0, 1, 0);
        playerBody.transform.localScale = new Vector3(1, 1, 1);

        Collider bodyCollider = playerBody.GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        MeshRenderer bodyRenderer = playerBody.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = false;
        }
    }
}