#nullable enable

using System;
using Coherence.Toolkit;
using Dissonance;
using Dissonance.Integrations.Coherence;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce;
    public float maxWalkSpeed;
    public float maxRunSpeed;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 90f;
    public float controllerLookMultiplier = 2.0f;

    private float xRotation = 0f;

    // jump
    private bool isGrounded;
    public bool IsGrounded
    {
        get => isGrounded;
    }

    // flight
    public float stabilizeSpeedDecrease;
    public float stabilizeTorqueMultiplier;

    // forces
    public Vector3 defaultGravity;
    private Vector3 gravity;

    [Header("Player")]
    private CharacterController characterController;
    private PlayerInput playerInput;
    private Rigidbody _rigidbody;

    [Header("Camera")]
    public GameObject cameraArm;
    private Camera playerCamera;

    // input actions
    private InputAction? moveAction;
    private InputAction? lookAction;
    private InputAction? sprintAction;
    private InputAction? stabilizeAction;

    public bool IsUsingGamepad => playerInput != null && playerInput.currentControlScheme == "Gamepad";

    public enum ControllerMovementMode
    {
        Gravity,
        ZeroG
    }

    private ControllerMovementMode movementMode = ControllerMovementMode.Gravity;
    public ControllerMovementMode MovementMode
    {
        get => movementMode;
    }

    void Start()
    {
        if (TryGetComponent<CoherenceSync>(out var _sync) && _sync.HasStateAuthority)
        {
            playerInput = GetComponent<PlayerInput>();
            _rigidbody = GetComponent<Rigidbody>();
            characterController = GetComponent<CharacterController>();

            SetMovementMode(ControllerMovementMode.Gravity);

            Camera mainCamera = cameraArm.AddComponent<Camera>();
            cameraArm.AddComponent<AudioListener>();
            playerCamera = mainCamera;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isGrounded = true;
        gravity = defaultGravity;
    }

    public void SetMovementMode(ControllerMovementMode newMovementMode)
    {
        movementMode = newMovementMode;

        if (movementMode == ControllerMovementMode.Gravity)
        {
            playerInput.SwitchCurrentActionMap("MovementGravity");

            moveAction = playerInput.currentActionMap.FindAction("Move");
            lookAction = playerInput.currentActionMap.FindAction("Look");
            sprintAction = playerInput.currentActionMap.FindAction("Sprint");
            stabilizeAction = null;

            gravity = defaultGravity;
            _rigidbody.freezeRotation = true;
            _rigidbody.rotation = Quaternion.identity;
        }
        else
        {
            playerInput.SwitchCurrentActionMap("MovementZeroG");

            moveAction = null;
            lookAction = null;
            sprintAction = null;
            stabilizeAction = playerInput.currentActionMap.FindAction("Stabilize");

            gravity = Vector3.zero;
            _rigidbody.freezeRotation = false;

            _rigidbody.AddForce(new Vector3(0, 25.0f, 0));

            Vector3 randomVector = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            );

            _rigidbody.AddTorque(randomVector * 0.5f);
        }
    }

    void Update()
    {
        if (MovementMode == ControllerMovementMode.Gravity)
        {
            HandleMouseLook();
            HandleMovement();
        }
        else
        {
            HandleZeroGMovement();
        }
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

        // @trae todo
        // 1. IsGrounded flag actual check
        // 3. if !IsGrounded -> only allow look input, no air control (or maybe reduce it? x0.2 or something?)

        // apply gravity and movement forces based on input
        Vector3 direction = (transform.right * moveInput.Value.x + transform.forward * moveInput.Value.y).normalized;

        _rigidbody.AddForce(gravity);
        _rigidbody.AddForce(direction * moveForce);

        // clamp velocity in the XZ-direction to a maximum speed
        Vector3 velocity = _rigidbody.linearVelocity;
        float yComponent = velocity.y;
        velocity.y = 0.0f;

        float maxSpeed = sprintIsPressed.Value ? maxRunSpeed : maxWalkSpeed;
        if (isGrounded && velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            velocity.Normalize();
            velocity *= maxSpeed;
        }

        velocity.y = yComponent;
        _rigidbody.linearVelocity = velocity;
    }

    void HandleZeroGMovement()
    {
        bool? isStabilizePressed = stabilizeAction?.IsPressed();

        if (isStabilizePressed == null)
        {
            return;
        }

        if (isStabilizePressed.Value)
        {
            // Dampen linear velocity
            Vector3 velocity = _rigidbody.linearVelocity;
            double speed = Math.Sqrt(velocity.sqrMagnitude);

            speed = Math.Clamp(speed - stabilizeSpeedDecrease * Time.deltaTime, 0.0, speed);
            _rigidbody.linearVelocity = velocity.normalized * (float)speed;

            // Dampen angular velocity
            // Apply a counter-torque proportional to the current angular velocity
            Vector3 angularVelocity = _rigidbody.angularVelocity;
            Vector3 stabilizationTorque = -angularVelocity * (1.0f - stabilizeTorqueMultiplier);
            _rigidbody.AddTorque(stabilizationTorque, ForceMode.Acceleration);
        }
    }
}