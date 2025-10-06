#nullable enable

using Coherence.Toolkit;
using Dissonance;
using Dissonance.Integrations.Coherence;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 90f;

    public float controllerLookMultiplier = 2.0f;

    private float xRotation = 0f;

    [Header("Player")]
    private CharacterController characterController;
    private PlayerInput playerInput;

    [Header("Camera")]
    public GameObject cameraArm;
    private Camera playerCamera;

    [Header("Input Actions")]
    private InputAction? moveAction;
    private InputAction? lookAction;
    private InputAction? sprintAction;

    public bool IsUsingGamepad => playerInput != null && playerInput.currentControlScheme == "Gamepad";

    void Start()
    {
        if (TryGetComponent<CoherenceSync>(out var _sync) && _sync.HasStateAuthority)
        {
            moveAction = InputSystem.actions.FindAction("Move");
            lookAction = InputSystem.actions.FindAction("Look");
            sprintAction = InputSystem.actions.FindAction("Sprint");

            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("InGame");
            }

            Camera mainCamera = cameraArm.AddComponent<Camera>();
            cameraArm.AddComponent<AudioListener>();
        }

        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        Vector2? lookInput = lookAction?.ReadValue<Vector2>();
        if (lookInput == null)
        {
            return;
        }

        float lookX, lookY;

        if (IsUsingGamepad)
        {
            // Gamepad: use Time.deltaTime for smooth, frame-independent rotation
            lookX = lookInput.Value.x * lookSensitivity * controllerLookMultiplier * Time.deltaTime;
            lookY = lookInput.Value.y * lookSensitivity * controllerLookMultiplier * Time.deltaTime;
        }
        else
        {
            // Mouse: don't use Time.deltaTime (mouse delta is already frame-independent)
            lookX = lookInput.Value.x * lookSensitivity;
            lookY = lookInput.Value.y * lookSensitivity;
        }

        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * lookX);
    }

    void HandleMovement()
    {
        Vector2? moveInput = moveAction?.ReadValue<Vector2>();
        bool? sprintIsPressed = sprintAction?.IsPressed();

        if (moveInput == null || sprintIsPressed == null)
        {
            return;
        }

        Vector3 direction = (transform.right * moveInput.Value.x + transform.forward * moveInput.Value.y).normalized;

        float speed = sprintIsPressed.Value ? runSpeed : walkSpeed;

        characterController.Move(direction * speed * Time.deltaTime);
    }
}