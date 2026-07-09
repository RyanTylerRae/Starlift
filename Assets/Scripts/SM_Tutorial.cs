#nullable enable

using UnityEngine;

public class SM_Tutorial : MonoBehaviour
{
    public Transform? lookAtTarget;
    public float startingWaitSeconds = 5f;
    public float rotationStopThreshold = 0.1f;
    public float lookAtAngleThreshold = 10f;
    public float initialRotationSpeed = 90f; // degrees/sec around camera forward

    private FirstPersonController? playerController = null;
    private Rigidbody? playerRigidbody = null;
    private PlayerHUD? playerHud = null;
    private StateMachine stateMachine = new();
    private float waitStartTime = 0f;
    private float defaultRollDamping = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildStateMachine();
        stateMachine.Start();
    }

    private void TryResolvePlayerReferences()
    {
        playerController = StarliftStatics.FindFirstPersonController();
        playerHud = FindFirstObjectByType<PlayerHUD>();

        GameObject? player = StarliftStatics.FindPlayer();
        if (player != null && player.TryGetComponent<Rigidbody>(out var rb))
        {
            playerRigidbody = rb;
        }
    }

    private void ApplyInitialSpin()
    {
        if (playerRigidbody == null || playerController?.playerCamera == null)
        {
            return;
        }

        Vector3 spinAxis = playerController.playerCamera.transform.forward;
        playerRigidbody.angularVelocity = spinAxis * (initialRotationSpeed * Mathf.Deg2Rad);
    }

    private void BuildStateMachine()
    {
        stateMachine.RegisterState("Init", isStartingState: true);
        stateMachine.RegisterState("Wait");
        stateMachine.RegisterState("Stabilize");
        stateMachine.RegisterState("Look");
        stateMachine.RegisterState("Thrust");
        stateMachine.RegisterState("Landed");
        stateMachine.RegisterState("Landed2");
        stateMachine.RegisterState("Landed3");
        stateMachine.RegisterState("Complete");

        // the player is spawned at runtime by PlayerSpawner, so wait until it exists
        stateMachine.AddEdge("Init", "Wait", () =>
        {
            if (playerController == null || playerRigidbody == null)
            {
                TryResolvePlayerReferences();
            }

            return playerController != null && playerRigidbody != null && playerController.playerCamera != null;
        });

        // 1. wait a few seconds
        stateMachine.AddEdge("Wait", "Stabilize", () => Time.time - waitStartTime >= startingWaitSeconds);

        // edge 2 -> 3 player is no longer rotating
        stateMachine.AddEdge("Stabilize", "Look", () => playerRigidbody != null && playerRigidbody.angularVelocity.magnitude < rotationStopThreshold);

        // edge 3 -> 4, player looks at specific game object target (reference as member)
        stateMachine.AddEdge("Look", "Thrust", () =>
        {
            if (lookAtTarget == null || playerController?.playerCamera == null)
            {
                return false;
            }

            Transform cameraTransform = playerController.playerCamera.transform;
            float angle = Vector3.Angle(cameraTransform.forward, lookAtTarget.position - cameraTransform.position);
            return angle < lookAtAngleThreshold;
        });

        // edge 4 -> 5, player attaches to the surface
        stateMachine.AddEdge("Thrust", "Landed", () => playerController != null && playerController.MovementMode == FirstPersonController.ControllerMovementMode.Magnetized);

        stateMachine.AddEdge("Landed", "Landed2", () => Time.time - waitStartTime >= startingWaitSeconds);

        stateMachine.AddEdge("Landed2", "Landed3", () => Time.time - waitStartTime >= startingWaitSeconds);

        stateMachine.AddEdge("Landed3", "Complete", () => Time.time - waitStartTime >= startingWaitSeconds);

        // disable player oxygen HUD, disable stabilize/look/thrust, disable roll damping, start the initial spin
        stateMachine.GetState("Wait").OnEnterState += () =>
        {
            playerHud?.SetOxygenHudEnabled(false);
            playerController?.SetStabilizeEnabled(false);
            playerController?.SetLookEnabled(false);
            playerController?.SetThrustEnabled(false);

            // rigidbody rotation is frozen outside ZeroG mode, so the initial spin needs ZeroG active first
            playerController?.SetMovementMode(FirstPersonController.ControllerMovementMode.ZeroG);

            if (playerController != null)
            {
                defaultRollDamping = playerController.rollDamping;
                playerController.rollDamping = 0f;
            }

            ApplyInitialSpin();

            waitStartTime = Time.time;
        };

        // 2. enable player stabilize, advance dialogue
        stateMachine.GetState("Stabilize").OnEnterState += () =>
        {
            playerController?.SetStabilizeEnabled(true);
            SubtitleManager.Instance?.AdvanceSubtitle();
        };

        // 3. enable player look, advance dialgoue
        stateMachine.GetState("Look").OnEnterState += () =>
        {
            // player has stabilized themselves by this point, ambient roll damping can resume
            if (playerController != null)
            {
                playerController.rollDamping = defaultRollDamping;
            }

            playerController?.SetLookEnabled(true);
            SubtitleManager.Instance?.AdvanceSubtitle();
        };

        // 4. enable player thrust, advance dialogue
        stateMachine.GetState("Thrust").OnEnterState += () =>
        {
            playerController?.SetThrustEnabled(true);
            SubtitleManager.Instance?.AdvanceSubtitle();
        };

        stateMachine.GetState("Landed").OnEnterState += () =>
        {
            playerHud?.SetOxygenHudEnabled(true);
            SubtitleManager.Instance?.AdvanceSubtitle();
            waitStartTime = Time.time;
        };

        stateMachine.GetState("Landed2").OnEnterState += () =>
        {
            SubtitleManager.Instance?.AdvanceSubtitle();
            waitStartTime = Time.time;
        };

        stateMachine.GetState("Landed3").OnEnterState += () =>
        {
            SubtitleManager.Instance?.AdvanceSubtitle();
            waitStartTime = Time.time;
        };

        // TODO: tutorial end behavior
        stateMachine.GetState("Complete").OnEnterState += () =>
        {
            SubtitleManager.Instance?.ClearSubtitle();
        };
    }

    // Update is called once per frame
    void Update()
    {
        stateMachine.Update();
    }
}
