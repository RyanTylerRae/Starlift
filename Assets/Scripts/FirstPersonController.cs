#nullable enable

using System;
using System.Collections;
using Dissonance;
using Steamworks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FirstPersonController : MonoBehaviour
{
    [Header("Gravity-Based Movement")]
    public float moveForce;
    public float maxWalkSpeed;
    public float maxRunSpeed;

    [Header("Magnetized Movement")]
    public float maxMagnetizedWalkSpeed;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 90f;
    public float controllerLookMultiplier = 2.0f;
    public float zeroGRollSpeed = 45f;

    private float xRotation = 0f;

    [Header("Zero Gravity")]
    public float stabilizeMultiplier;

    public float flightForce;
    public float maxFlightSpeed;

    // gravity alignment
    public float gravityAlignmentSpeed = 5f;

    // camera angle tracking
    private float cameraAngleFromGravity = 0f;
    public float CameraAngleFromGravity => cameraAngleFromGravity;

    // jump charge
    private float jumpPressStartTime = 0f;
    public float jumpTier1Time = 0f;
    public float jumpTier2Time = 0f;
    public float jumpTier3Time = 0f;
    public float jumpDirectionalAngleThreshold = 135f;

    [Header("Physics Sub-stepping")]
    public float substepDistance = 0.05f;

    // Desired movement velocity from input (used in FixedUpdate)
    private Vector3 desiredMovementVelocity = Vector3.zero;

    [Header("Player")]
    private CharacterController characterController;
    private PlayerInput playerInput;
    private Rigidbody _rigidbody;
    private GravityController gravityController;

    [Header("Camera")]
    public GameObject cameraArm;
    public Camera playerCamera;

    // input actions
    private InputAction? moveAction;
    private InputAction? lookAction;
    private InputAction? jumpAction;
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

    private Modifiers? modifiers = null;

    public bool IsUsingGamepad => playerInput != null && playerInput.currentControlScheme == "Gamepad";

    public bool ShouldDisplayJumpTarget => CameraAngleFromGravity > jumpDirectionalAngleThreshold && MovementMode == ControllerMovementMode.Magnetized;

    public enum ControllerMovementMode
    {
        Magnetized,
        ZeroG,
        Gravity
    }

    private ControllerMovementMode movementMode = ControllerMovementMode.Magnetized;
    public ControllerMovementMode MovementMode
    {
        get => movementMode;
    }

    void Start()
    {
        modifiers = GetComponent<Modifiers>();

        //if (TryGetComponent<CoherenceSync>(out var _sync) && _sync.HasStateAuthority)
        //{
        playerInput = GetComponent<PlayerInput>();
        _rigidbody = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        gravityController = GetComponent<GravityController>();

        SetMovementMode(ControllerMovementMode.Gravity);

        Camera mainCamera = cameraArm.AddComponent<Camera>();
        mainCamera.cullingMask &= ~LayerMask.GetMask("3D_HUD");
        mainCamera.depth = -1.0f;


        cameraArm.AddComponent<AkAudioListener>();
        playerCamera = mainCamera;
        //}

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetMovementMode(ControllerMovementMode newMovementMode)
    {
        movementMode = newMovementMode;

        if (MovementMode == ControllerMovementMode.Gravity)
        {
            playerInput.SwitchCurrentActionMap("MovementGravity");

            moveAction = playerInput.currentActionMap.FindAction("Move");
            lookAction = playerInput.currentActionMap.FindAction("Look");
            sprintAction = playerInput.currentActionMap.FindAction("Sprint");

            jumpAction = null;
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
        else if (movementMode == ControllerMovementMode.Magnetized)
        {
            playerInput.SwitchCurrentActionMap("MovementMagnetized");

            moveAction = playerInput.currentActionMap.FindAction("Move");
            lookAction = playerInput.currentActionMap.FindAction("Look");
            jumpAction = playerInput.currentActionMap.FindAction("Jump");

            // Subscribe to jump action events
            if (jumpAction != null)
            {
                jumpAction.started -= OnJumpStarted;
                jumpAction.started += OnJumpStarted;

                jumpAction.canceled -= OnJumpCanceled;
                jumpAction.canceled += OnJumpCanceled;
            }

            sprintAction = null;
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
        else if (movementMode == ControllerMovementMode.ZeroG)
        {
            playerInput.SwitchCurrentActionMap("MovementZeroG");

            lookAction = playerInput.currentActionMap.FindAction("Look");
            stabilizeAction = playerInput.currentActionMap.FindAction("Stabilize");
            forwardThrustAction = playerInput.currentActionMap.FindAction("ForwardThrust");
            backwardThrustAction = playerInput.currentActionMap.FindAction("BackwardThrust");
            leftThrustAction = playerInput.currentActionMap.FindAction("LeftThrust");
            rightThrustAction = playerInput.currentActionMap.FindAction("RightThrust");
            upThrustAction = playerInput.currentActionMap.FindAction("UpThrust");
            downThrustAction = playerInput.currentActionMap.FindAction("DownThrust");
            rotateLeftAction = playerInput.currentActionMap.FindAction("RotateLeft");
            rotateRightAction = playerInput.currentActionMap.FindAction("RotateRight");

            moveAction = null;
            jumpAction = null;
            sprintAction = null;

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

        if (modifiers != null)
        {
            float pressDuration = 0.0f;
            if (jumpPressStartTime > 0.0f && MovementMode == ControllerMovementMode.Magnetized)
            {
                pressDuration = Time.time - jumpPressStartTime;
            }

            float tier1Norm = Math.Clamp(pressDuration / jumpTier1Time, 0.0f, 1.0f);
            float tier2Norm = Math.Clamp((pressDuration - jumpTier1Time) / jumpTier2Time, 0.0f, 1.0f);
            float tier3Norm = Math.Clamp((pressDuration - jumpTier1Time - jumpTier2Time) / jumpTier3Time, 0.0f, 1.0f);

            modifiers.Set(ModifierType.JumpCharge_Tier1, tier1Norm);
            modifiers.Set(ModifierType.JumpCharge_Tier2, tier2Norm);
            modifiers.Set(ModifierType.JumpCharge_Tier3, tier3Norm);

            if (MovementMode != ControllerMovementMode.Magnetized)
            {
                modifiers.Set(ModifierType.MagneticCharge, 0.0f);
            }
            else if (MovementMode == ControllerMovementMode.Magnetized)
            {
                float magneticCharge = Math.Clamp((jumpTier1Time - pressDuration) / jumpTier1Time, 0.0f, 1.0f);
                if (magneticCharge <= 0.0f && modifiers.Get(ModifierType.MagneticCharge) > 0.0f)
                {
                    TriggerCameraShake(0.05f, 0.1f, 8);
                }

                modifiers.Set(ModifierType.MagneticCharge, magneticCharge);
            }
        }

        Vector3 gravity = gravityController.GetGravityVector();

        // Calculate camera angle from gravity direction
        if (gravity.sqrMagnitude > 0.01f)
        {
            cameraAngleFromGravity = Vector3.Angle(playerCamera.transform.forward, gravity);
        }

        bool isMagnetized = gravityController.GetActiveGravitySource()?.isMagnetized ?? false;

        if (MovementMode == ControllerMovementMode.ZeroG && gravity.sqrMagnitude > 0.0f)
        {
            if (isMagnetized)
            {
                SetMovementMode(ControllerMovementMode.Magnetized);
            }
            else
            {
                SetMovementMode(ControllerMovementMode.Gravity);
            }

            TriggerCameraShake(0.05f, 0.1f, 8);
        }
        else if (MovementMode != ControllerMovementMode.ZeroG && gravity.sqrMagnitude < 0.01f)
        {
            SetMovementMode(ControllerMovementMode.ZeroG);
        }
        else if (MovementMode == ControllerMovementMode.Magnetized && !isMagnetized)
        {
            SetMovementMode(ControllerMovementMode.Gravity);
        }
        else if (MovementMode == ControllerMovementMode.Gravity && isMagnetized)
        {
            SetMovementMode(ControllerMovementMode.Magnetized);
        }

        if (MovementMode != ControllerMovementMode.ZeroG)
        {
            HandleMouseLook();

            if (jumpPressStartTime == 0.0f)
            {
                if (MovementMode == ControllerMovementMode.Magnetized)
                {
                    HandleMovementSubStepped();
                }
                else if (MovementMode == ControllerMovementMode.Gravity)
                {
                    HandleMovement();
                }
            }

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

    public void FixedUpdate()
    {
        // if we are in magnetized mode, apply sub-stepping to move across surfaces smoothly
        // we will move in increments, and reproject to find the new surface normal at each step
        if (MovementMode != ControllerMovementMode.Magnetized)
        {
            return;
        }

        GravitySourceComponent? gravitySource = gravityController?.GetActiveGravitySource();
        if (gravitySource == null)
        {
            Debug.Log("FirstPersonController: FixedUpdate - no gravity source");
            return;
        }

        Vector3 velocity = _rigidbody.linearVelocity;
        Vector3 position = _rigidbody.position;

        // calculate relative velocity to the gravity source and the distance it represents
        Vector3 gravitySourceVelocity = Vector3.zero;
        if (gravitySource.TryGetComponent(out Rigidbody rBody))
        {
            gravitySourceVelocity = rBody.linearVelocity;
        }

        // Replace horizontal velocity with desired movement + gravity source velocity (to make it absolute)
        // Keep vertical component for gravity. Gravity source velocity will be added back at end.
        float verticalVelocity = velocity.y - gravitySourceVelocity.y;
        velocity = desiredMovementVelocity + new Vector3(0, verticalVelocity, 0);

        Vector3 relativeVelocity = velocity;
        float totalDistance = relativeVelocity.magnitude * Time.fixedDeltaTime;

        int substeps = Mathf.Max(1, Mathf.CeilToInt(totalDistance / substepDistance));
        float subDeltaTime = Time.fixedDeltaTime / substeps;

        for (int i = 0; i < substeps; i++)
        {
            // manually integrate position: p = p + v * dt
            position += velocity * subDeltaTime;

            // Adjust velocity based on current position
            velocity = AdjustVelocityPerSubstep(gravitySource, velocity, position, subDeltaTime);
        }

        velocity = ClampVelocityInGravity(velocity);

        // apply final state to rigidbody, included the new adjusted velocity
        _rigidbody.position = position;
        _rigidbody.linearVelocity = gravitySourceVelocity + velocity;
    }

    private Vector3 AdjustVelocityPerSubstep(GravitySourceComponent gravitySource, Vector3 velocity, Vector3 position, float deltaTime)
    {
        Vector3 gravityVector = gravitySource.GetGravityVector(position);
        Vector3 normal = -gravityVector.normalized;

        return velocity - Vector3.Dot(velocity, normal) * normal;
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

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressStartTime = Time.time;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        float pressDuration = Time.time - jumpPressStartTime;
        jumpPressStartTime = 0.0f;

        Debug.Log($"Jump was held for: {pressDuration} seconds");
        // Use pressDuration for your jump mechanics

        float jumpForce = 0.0f;
        if (pressDuration > jumpTier3Time + jumpTier2Time + jumpTier1Time)
        {
            jumpForce = 500.0f;
            Debug.Log($"Jump tier 3!");
        }
        else if (pressDuration > jumpTier2Time + jumpTier1Time)
        {
            jumpForce = 300.0f;
            Debug.Log($"Jump tier 2!");
        }
        else if (pressDuration > jumpTier1Time)
        {
            jumpForce = 150.0f;
            Debug.Log($"Jump tier 1!");
        }

        if (jumpForce > 0.0f)
        {
            Vector3 jumpDirection;

            // If camera angle exceeds threshold, jump in camera direction
            if (cameraAngleFromGravity > jumpDirectionalAngleThreshold)
            {
                jumpDirection = playerCamera.transform.forward;
            }
            else
            {
                // Otherwise jump against gravity
                Vector3 gravity = gravityController.GetGravityVector();
                jumpDirection = -1.0f * gravity.normalized;
            }

            _rigidbody.AddForce(jumpDirection * jumpForce);

            // Disable active gravity source for 0.5 seconds on tier 1+ jump
            GravitySourceComponent? activeSource = gravityController.GetActiveGravitySource();
            if (activeSource != null)
            {
                activeSource.DisableForSeconds(0.5f);
            }
        }
    }

    private void HandleMovementSubStepped()
    {
        Vector2? moveInput = moveAction?.ReadValue<Vector2>();
        if (moveInput == null)
        {
            desiredMovementVelocity = Vector3.zero;
            return;
        }

        // calculate movement direction in world space
        Vector3 direction = (transform.right * moveInput.Value.x + transform.forward * moveInput.Value.y).normalized;

        // store desired movement velocity (will be applied in FixedUpdate)
        desiredMovementVelocity = direction * maxMagnetizedWalkSpeed * moveInput.Value.magnitude;
    }

    private void HandleMovement()
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
        bool isGrounded = true;

        // apply gravity and movement forces based on input
        Vector3 direction = (transform.right * moveInput.Value.x + transform.forward * moveInput.Value.y).normalized;
        _rigidbody.AddForce(direction * moveForce);

        Vector3 gravity = gravityController.GetGravityVector();

        // clamp velocity tangent to gravity to a maximum speed
        Vector3 velocity = _rigidbody.linearVelocity;

        // project velocity onto gravity direction and save it
        Vector3 gravityDir = gravity.normalized;
        Vector3 velocityInGravityDir = Vector3.Dot(velocity, gravityDir) * gravityDir;

        // remove gravity component from velocity
        Vector3 velocityTangent = velocity - velocityInGravityDir;

        float maxSpeed = sprintIsPressed.Value ? maxRunSpeed : maxWalkSpeed;

        if (isGrounded && velocityTangent.sqrMagnitude > maxSpeed * maxSpeed)
        {
            velocityTangent.Normalize();
            velocityTangent *= maxSpeed;
        }

        // restore gravity component
        velocity = velocityTangent + velocityInGravityDir;
        _rigidbody.linearVelocity = velocity;

    }

    private Vector3 ClampVelocityInGravity(Vector3 velocity)
    {
        bool? sprintIsPressed = sprintAction?.IsPressed();
        if (sprintIsPressed != null)
        {
            float maxSpeed = sprintIsPressed.Value ? maxRunSpeed : maxWalkSpeed;
            if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                velocity.Normalize();
                velocity *= maxSpeed;
            }
        }

        return velocity;
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

    public void TriggerCameraShake(float duration, float magnitude, int delayMs)
    {
        StartCoroutine(DoCameraShake(duration, magnitude, delayMs));
    }

    private IEnumerator DoCameraShake(float duration, float magnitude, int delayMs)
    {
        Vector3 originalPos = playerCamera.transform.localPosition;
        float elapsed = 0.0f;
        float delaySeconds = delayMs / 1000.0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            playerCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);

            yield return new WaitForSeconds(delaySeconds);
            elapsed += delaySeconds;
        }

        playerCamera.transform.localPosition = originalPos;
    }

    private void OnDestroy()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
            jumpAction.canceled -= OnJumpCanceled;
        }
    }
}