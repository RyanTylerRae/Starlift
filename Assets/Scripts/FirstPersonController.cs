#nullable enable

using System;
using Dissonance;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    public float zeroGRollSpeed = 45f;

    private float xRotation = 0f;

    // jump
    private bool isGrounded;
    public bool IsGrounded
    {
        get => isGrounded;
    }

    // flight
    public float stabilizeMultiplier;

    public float flightForce;
    public float maxFlightSpeed;

    // gravity alignment
    public float gravityAlignmentSpeed = 5f;

    [Header("Player")]
    private CharacterController characterController;
    private PlayerInput playerInput;
    private Rigidbody _rigidbody;
    private GravityController gravityController;

    [Header("Camera")]
    public GameObject cameraArm;
    public Camera? playerCamera;

    // input actions
    private InputAction? moveAction;
    private InputAction? lookAction;
    private InputAction? sprintAction;
    private InputAction? stabilizeAction;
    private InputAction? forwardThrustAction;
    private InputAction? backwardThrustAction;
    private InputAction? leftThrustAction;
    private InputAction? rightThrustAction;
    private InputAction? upThrustAction;
    private InputAction? downThrustAction;
    private InputAction? rotateLeftAction;
    private InputAction? rotateRightAction;

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
        //if (TryGetComponent<CoherenceSync>(out var _sync) && _sync.HasStateAuthority)
        //{
        playerInput = GetComponent<PlayerInput>();
        _rigidbody = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        gravityController = GetComponent<GravityController>();

        SetMovementMode(ControllerMovementMode.Gravity);

        Camera mainCamera = cameraArm.AddComponent<Camera>();
        mainCamera.cullingMask &= ~LayerMask.GetMask("3D_HUD");

        cameraArm.AddComponent<AkAudioListener>();
        playerCamera = mainCamera;
        //}

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isGrounded = true;
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
            forwardThrustAction = null;
            backwardThrustAction = null;
            leftThrustAction = null;
            rightThrustAction = null;
            upThrustAction = null;
            downThrustAction = null;
            rotateLeftAction = null;
            rotateRightAction = null;

            _rigidbody.freezeRotation = true;
            _rigidbody.rotation = Quaternion.identity;
        }
        else
        {
            playerInput.SwitchCurrentActionMap("MovementZeroG");

            moveAction = null;
            lookAction = playerInput.currentActionMap.FindAction("Look");
            sprintAction = null;
            stabilizeAction = playerInput.currentActionMap.FindAction("Stabilize");
            forwardThrustAction = playerInput.currentActionMap.FindAction("ForwardThrust");
            backwardThrustAction = playerInput.currentActionMap.FindAction("BackwardThrust");
            leftThrustAction = playerInput.currentActionMap.FindAction("LeftThrust");
            rightThrustAction = playerInput.currentActionMap.FindAction("RightThrust");
            upThrustAction = playerInput.currentActionMap.FindAction("UpThrust");
            downThrustAction = playerInput.currentActionMap.FindAction("DownThrust");
            rotateLeftAction = playerInput.currentActionMap.FindAction("RotateLeft");
            rotateRightAction = playerInput.currentActionMap.FindAction("RotateRight");

            _rigidbody.freezeRotation = false;
        }
    }

    void Update()
    {
        if (gravityController == null)
        {
            Debug.LogWarning("FirstPersonController does not have a sibling GravityController!");
            return;
        }

        Vector3 gravity = gravityController.GetGravityVector();
        if (MovementMode == ControllerMovementMode.ZeroG && gravity.sqrMagnitude > 0.0f)
        {
            SetMovementMode(ControllerMovementMode.Gravity);
        }
        else if (MovementMode == ControllerMovementMode.Gravity && gravity.sqrMagnitude < 0.01f)
        {
            SetMovementMode(ControllerMovementMode.ZeroG);
        }

        if (MovementMode == ControllerMovementMode.Gravity)
        {
            HandleMouseLook();
            HandleMovement();

            // orient player to align with gravity
            Vector3 upVector = -gravity.normalized;
            if (upVector.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * gravityAlignmentSpeed);
            }
        }
        else
        {
            HandleZeroGLook();
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

        // @trae todo - add grounded flag and logic
        // 1. IsGrounded flag actual check
        // 3. if !IsGrounded -> only allow look input, no air control (or maybe reduce it? x0.2 or something?)

        // apply gravity and movement forces based on input
        Vector3 direction = (transform.right * moveInput.Value.x + transform.forward * moveInput.Value.y).normalized;

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

    void HandleZeroGLook()
    {
        Vector2? lookInput = lookAction?.ReadValue<Vector2>();
        if (lookInput != null)
        {
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

            // Apply pitch and yaw rotation to the rigidbody using camera's forward as reference
            _rigidbody.transform.Rotate(playerCamera.transform.up, lookX, Space.World);
            _rigidbody.transform.Rotate(playerCamera.transform.right, -lookY, Space.World);
        }

        float rotateLeftInput = rotateLeftAction?.ReadValue<float>() ?? 0f;
        float rotateRightInput = rotateRightAction?.ReadValue<float>() ?? 0f;

        // Apply roll rotation around the camera's forward vector
        float rollInput = rotateLeftInput - rotateRightInput;
        if (Mathf.Abs(rollInput) > 0.01f)
        {
            float rollAmount = rollInput * zeroGRollSpeed * Time.deltaTime;
            _rigidbody.transform.Rotate(playerCamera.transform.forward, rollAmount, Space.World);
        }
    }

    void HandleZeroGMovement()
    {
        bool? isStabilizePressed = stabilizeAction?.IsPressed();

        if (isStabilizePressed == null)
        {
            return;
        }

        float forwardThrustInput = forwardThrustAction?.ReadValue<float>() ?? 0f;
        float backwardThrustInput = backwardThrustAction?.ReadValue<float>() ?? 0f;
        float leftThrustInput = leftThrustAction?.ReadValue<float>() ?? 0f;
        float rightThrustInput = rightThrustAction?.ReadValue<float>() ?? 0f;
        float upThrustInput = upThrustAction?.ReadValue<float>() ?? 0f;
        float downThrustInput = downThrustAction?.ReadValue<float>() ?? 0f;

        Vector3 velocity = _rigidbody.linearVelocity;

        if (isStabilizePressed.Value)
        {
            Vector3 stabilizationForce = -velocity * (1.0f - stabilizeMultiplier);
            _rigidbody.AddForce(stabilizationForce, ForceMode.Acceleration);

            Vector3 angularVelocity = _rigidbody.angularVelocity;
            Vector3 stabilizationTorque = -angularVelocity * (1.0f - stabilizeMultiplier);
            _rigidbody.AddTorque(stabilizationTorque, ForceMode.Acceleration);
        }

        Vector3 thrustVector = Vector3.zero;
        thrustVector += playerCamera.transform.forward * forwardThrustInput;
        thrustVector += -playerCamera.transform.forward * backwardThrustInput;
        thrustVector += -playerCamera.transform.right * leftThrustInput;
        thrustVector += playerCamera.transform.right * rightThrustInput;
        thrustVector += playerCamera.transform.up * upThrustInput;
        thrustVector += -playerCamera.transform.up * downThrustInput;

        _rigidbody.AddForce(thrustVector.normalized * flightForce);

        velocity = _rigidbody.linearVelocity;
        if (velocity.sqrMagnitude > maxFlightSpeed * maxFlightSpeed)
        {
            _rigidbody.linearVelocity = velocity.normalized * maxFlightSpeed;
        }
    }
}