using UnityEngine;
using UnityEngine.InputSystem;

public class DebugControls : MonoBehaviour
{
    public void OnDebugSwapControlMode(InputValue value)
    {
        FirstPersonController playerController = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);

        if (playerController.MovementMode == FirstPersonController.ControllerMovementMode.ZeroG)
        {
            playerController.SetMovementMode(FirstPersonController.ControllerMovementMode.Gravity);
            Debug.Log("MoveMode: Gravity");
        }
        else
        {
            playerController.SetMovementMode(FirstPersonController.ControllerMovementMode.ZeroG);
            Debug.Log("MoveMode: ZeroG");
        }
    }

    public void OnEmbark(InputValue value)
    {
        if (value.isPressed)
        {
            Object.FindFirstObjectByType<EmbarkController>()?.OnEmbark();
        }
    }

    public void OnKillPlayer(InputValue value)
    {
        if (value.isPressed)
        {
            FirstPersonController playerController = Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
            var entity = playerController.gameObject.GetComponentInChildren<Entity>();
            if (entity != null)
            {
                DamageEvent damageEvent = new();
                entity.Kill(damageEvent);
            }
        }
    }
}
