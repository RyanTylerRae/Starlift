#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

public class DebugControls : MonoBehaviour
{
    private InputAction? killPlayerAction;
    private InputAction? teleportPlayerAction;
    private InputAction? supplyMaxOxygenAction;

    private bool isInitialized = false;

    void Update()
    {
        if (!isInitialized)
        {
            PlayerInput? playerInput = Object.FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Exclude);
            if (playerInput != null)
            {
                InputActionMap? debugMap = playerInput.actions.FindActionMap("DEBUG");
                if (debugMap != null)
                {
                    debugMap.Enable();
                    killPlayerAction = debugMap.FindAction("KillPlayer");
                    teleportPlayerAction = debugMap.FindAction("TeleportPlayer");
                    supplyMaxOxygenAction = debugMap.FindAction("SupplyMaxOxygen");
                }
            }

            isInitialized = true;
        }

        if (killPlayerAction?.WasPressedThisFrame() == true)
        {
            KillPlayer();
        }

        if (teleportPlayerAction?.WasPressedThisFrame() == true)
        {
            TeleportPlayer();
        }

        if (supplyMaxOxygenAction?.WasPressedThisFrame() == true)
        {
            SupplyMaxOxygen();
        }
    }

    private void KillPlayer()
    {
        FirstPersonController playerController = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
        var entity = playerController.gameObject.GetComponentInChildren<Entity>();
        if (entity != null)
        {
            DamageEvent damageEvent = new();
            entity.Kill(damageEvent);
        }
    }

    private void SupplyMaxOxygen()
    {
        FirstPersonController playerController = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
        Modifiers? modifiers = playerController.GetComponent<Modifiers>();
        if (modifiers != null)
        {
            modifiers.Set(ModifierType.Oxygen, modifiers.GetMax(ModifierType.Oxygen));
        }
    }

    private void TeleportPlayer()
    {
        FirstPersonController playerController = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
        Camera? camera = playerController.playerCamera;
        if (camera == null)
        {
            return;
        }

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Default")))
        {
            Vector3 targetPosition = hit.point + hit.normal * 1.5f;
            playerController.transform.position = targetPosition;

            Rigidbody? rb = playerController.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = targetPosition;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
