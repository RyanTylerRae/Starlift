#nullable enable

using System;
using System.Collections;
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
    private bool isGrounded = false;
    private Vector3 surfaceNormal = Vector3.up;
    public float groundedDistance;
    public float jumpForce;
    public float jumpCooldown = 0.3f;
    private float lastJumpTime = -1f;
    public float airControlMultiplier = 0.1f;
    public float maxJumpSpeed;
    public float groundFriction = 10f;
    public float groundStoppingFriction = 25f;
    public bool IsSprinting { get; private set; }

    [Header("Magnetized Movement")]
    public float maxMagnetizedWalkSpeed;
    public float magnetizeRadius = 0.5f;
    public float gravityAlignmentSpeed = 5f;
    public float jumpForceTier1;
    public float jumpForceTier2;
    public float jumpForceTier3;
    public float jumpTargetRaycastDistance = 200f;
    public float jumpDirectionalAngleThreshold = 135f;
    public float jumpGravityDisableDuration = 2.0f;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 90f;
    public float controllerLookMultiplier = 2.0f;
    public float zeroGRollSpeed = 45f;

    private float xRotation = 0f;

    // Cached look values for external use (e.g., HUD)
    public float LastLookX { get; private set; }
    public float LastLookY { get; private set; }

    [Header("Zero Gravity")]
    public float stabilizeMultiplier;

    public float flightForce;
    public float maxFlightSpeed;

    // camera angle tracking
    private float cameraAngleFromGravity = 0f;
    public float CameraAngleFromGravity => cameraAngleFromGravity;

    // jump charge
    private float jumpPressStartTime = 0f;
    public float jumpTier1Time = 0f;
    public float jumpTier2Time = 0f;
    public float jumpTier3Time = 0f;

    [Header("Physics Sub-stepping")]
    public float substepDistance = 0.01f;

    [Header("Collision")]
    [Range(0f, 1f)]
    public float airCollisionDampening = 0.85f;
    public float maxDepenetrationVelocity = 2f;

    // Desired movement velocity from input (used in FixedUpdate)
    private Vector3 desiredMovementVelocity = Vector3.zero;
    private Vector3 _preCollisionVelocity = Vector3.zero;

    // Gravity mode: force direction and speed cap stored in Update, applied in FixedUpdate
    private Vector3 desiredGravityForce = Vector3.zero;
    private float gravityModeMaxSpeed = 0f;

    [Header("Player")]
    private CharacterController? characterController;
    private PlayerInput? playerInput;
    private Rigidbody? _rigidbody;
    private GravityController? gravityController;
    private CapsuleCollider? bodyCollider;

    [Header("Camera")]
    public GameObject? cameraArm;
    public Camera? playerCamera;

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

    [Header("Oxygen")]
    public float minOxygenBurnRate = 0.33f;
    public float jumpOxygenCost;
    private float oxygenBurnRate = 0.0f;
    public float OxygenBurnRate => oxygenBurnRate;
    private OxygenSystem? oxygenSystem = null;

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
        oxygenSystem = GetComponent<OxygenSystem>();

        //if (TryGetComponent<CoherenceSync>(out var _sync) && _sync.HasStateAuthority)
        //{
        playerInput = GetComponent<PlayerInput>();
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null)
        {
            _rigidbody.maxDepenetrationVelocity = maxDepenetrationVelocity;
        }
        characterController = GetComponent<CharacterController>();
        gravityController = GetComponent<GravityController>();
        bodyCollider = GetComponentInChildren<CapsuleCollider>();

        if (gravityController != null)
            gravityController.ActiveSourceChanged += OnActiveGravitySourceChanged;

        SetMovementMode(ControllerMovementMode.Gravity);

        if (cameraArm != null)
        {
            Camera mainCamera = cameraArm.AddComponent<Camera>();
            mainCamera.cullingMask &= ~LayerMask.GetMask("3D_HUD");
            mainCamera.depth = -1.0f;
            cameraArm.AddComponent<AkAudioListener>();
            playerCamera = mainCamera;
        }
        //}

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetMovementMode(ControllerMovementMode newMovementMode)
    {
        if (playerInput == null)
        {
            return;
        }

        movementMode = newMovementMode;

        if (MovementMode == ControllerMovementMode.Gravity)
        {
            playerInput.SwitchCurrentActionMap("MovementGravity");

            moveAction = playerInput.currentActionMap.FindAction("Move");
            lookAction = playerInput.currentActionMap.FindAction("Look");
            sprintAction = playerInput.currentActionMap.FindAction("Sprint");
            jumpAction = playerInput.currentActionMap.FindAction("Jump");

            stabilizeAction = null;
            forwardThrustAction = null;
            backwardThrustAction = null;
            leftThrustAction = null;
            rightThrustAction = null;
            upThrustAction = null;
            downThrustAction = null;
            rotateLeftAction = null;
            rotateRightAction = null;

            if (_rigidbody != null)
            {
                _rigidbody.freezeRotation = true;
            }
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

            if (_rigidbody != null)
            {
                _rigidbody.freezeRotation = true;
            }
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

            if (_rigidbody != null)
            {
                _rigidbody.freezeRotation = false;
            }
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
                    //TriggerCameraShake(0.05f, 0.1f, 8);
                }

                modifiers.Set(ModifierType.MagneticCharge, magneticCharge);
            }
        }

        Vector3 gravity = gravityController.GetGravityVector();

        // Calculate camera angle from gravity direction
        if (gravity.sqrMagnitude > 0.01f && playerCamera != null)
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

            //TriggerCameraShake(0.05f, 0.1f, 8);
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
            // we don't burn extra oxygen when walking on a surface
            oxygenBurnRate = 0.0f;
            IsSprinting = false;

            HandleMouseLook();
            HandleGrounded();

            if (MovementMode == ControllerMovementMode.Magnetized)
            {
                HandleMovementSubStepped();
            }
            else if (MovementMode == ControllerMovementMode.Gravity && jumpPressStartTime == 0.0f)
            {
                HandleMovement();
                HandleJump();
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
        if (_rigidbody == null)
        {
            return;
        }

        if (MovementMode == ControllerMovementMode.Gravity)
        {
            if (desiredGravityForce != Vector3.zero)
            {
                _rigidbody.AddForce(desiredGravityForce);
            }

            Vector3 gravity = gravityController?.GetGravityVector() ?? Vector3.zero;
            if (gravity.sqrMagnitude > 0.01f)
            {
                Vector3 linearVelocity = _rigidbody.linearVelocity;
                Vector3 gravityDir = gravity.normalized;
                Vector3 velocityInGravityDir = Vector3.Dot(linearVelocity, gravityDir) * gravityDir;
                Vector3 velocityTangent = linearVelocity - velocityInGravityDir;

                if (velocityTangent.sqrMagnitude > gravityModeMaxSpeed * gravityModeMaxSpeed)
                {
                    velocityTangent = velocityTangent.normalized * gravityModeMaxSpeed;
                }

                if (isGrounded)
                {
                    float friction = desiredGravityForce == Vector3.zero ? groundStoppingFriction : groundFriction;
                    float speed = velocityTangent.magnitude;
                    float decel = friction * Time.fixedDeltaTime;
                    velocityTangent = speed > decel ? velocityTangent.normalized * (speed - decel) : Vector3.zero;
                }

                _rigidbody.linearVelocity = velocityTangent + velocityInGravityDir;
            }
            _preCollisionVelocity = _rigidbody.linearVelocity;
            return;
        }

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

        // calculate relative velocity to the gravity source and the distance it represents
        Vector3 gravitySourceVelocity = Vector3.zero;
        if (gravitySource.TryGetComponent(out Rigidbody rBody))
        {
            // might not be necessary, but this includes angular velocity as well
            gravitySourceVelocity = rBody.GetPointVelocity(_rigidbody.position);
        }

        Vector3 relativeVelocity = _rigidbody.linearVelocity - gravitySourceVelocity;

        // separate into vertical and horizontal components along gravity
        Vector3 position = _rigidbody.position;
        Vector3 gravityVector = gravitySource.GetGravityVector(position);
        Vector3 verticalAxis = (isGrounded && surfaceNormal.sqrMagnitude > 0.01f) ? surfaceNormal : -gravityVector.normalized;
        float verticalSpeed = Vector3.Dot(relativeVelocity, verticalAxis);
        float clampedVerticalSpeed = Mathf.Min(verticalSpeed, 0f);
        Vector3 verticalVelocity = clampedVerticalSpeed * verticalAxis;
        Vector3 velocity = desiredMovementVelocity + verticalVelocity;

        float totalDistance = velocity.magnitude * Time.fixedDeltaTime;

        int substeps = Mathf.Max(1, Mathf.CeilToInt(totalDistance / substepDistance));
        float subDeltaTime = Time.fixedDeltaTime / substeps;

        for (int i = 0; i < substeps; i++)
        {
            // calculate the new velocity at this step
            Vector3 gravityAtPosition = gravitySource.GetGravityVector(position);
            velocity += gravityAtPosition * subDeltaTime;

            // integrate the substep
            position += velocity * subDeltaTime;
        }

        // apply final state to rigidbody, included the new adjusted velocity
        _rigidbody.position = position;
        _rigidbody.linearVelocity = gravitySourceVelocity + velocity;
        _preCollisionVelocity = _rigidbody.linearVelocity;
    }

    void HandleMouseLook()
    {
        if (playerCamera == null)
        {
            return;
        }

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
        transform.Rotate(transform.up, lookX, Space.World);

        // Cache look values for external use
        LastLookX = lookX;
        LastLookY = lookY;
    }

    private void HandleGrounded()
    {
        isGrounded = false;

        if (gravityController == null)
        {
            return;
        }

        Vector3 gravity = gravityController.GetGravityVector();
        if (gravity.sqrMagnitude < 0.01)
        {
            return;
        }

        Vector3 gravityDir = gravity.normalized;
        int groundMask = LayerMask.GetMask("Default");

        float halfRadius = bodyCollider != null ? bodyCollider.radius * bodyCollider.transform.lossyScale.x : 0f;

        // perform the middle raycast, this can give us a hint to determine if we are over an edge or not,
        // and also allows us to early-out walking on magnetized surfaces with a steep angle
        if (Physics.Raycast(new Ray(transform.position, gravityDir), out RaycastHit centerHit, groundedDistance, groundMask))
        {
            isGrounded = true;
            surfaceNormal = centerHit.normal;
            return;
        }

        Vector3 ray = new();
        uint numHits = 0;

        // perform the other eight raycasts to help with ground detection on edges and corners
        // we also know our center is not grounded, so sum the grounded ray positions to create a ray we can use to search for the wall we are walking off of
        for (int i = 0; i <= 2; i++)
        {
            for (int j = 0; j <= 2; j++)
            {
                if (i == 1 && j == 1)
                {
                    continue;
                }

                Vector3 origin = transform.position + transform.right * ((i - 1) * halfRadius) + transform.forward * ((j - 1) * halfRadius);
                if (Physics.Raycast(new Ray(origin, gravityDir), groundedDistance, groundMask))
                {
                    ++numHits;
                    ray += origin - transform.position;
                    isGrounded = true;
                }
            }
        }

        if (isGrounded)
        {
            // this ray is diagonal and should point back towards the wall we are walking off of from the center
            ray /= numHits;
            ray += gravityDir;
            ray.Normalize();

            if (Physics.Raycast(new Ray(transform.position, ray), out RaycastHit edgeHit, groundedDistance * 2f, groundMask))
            {
                surfaceNormal = edgeHit.normal;
            }
            else
            {
                surfaceNormal = -gravityDir;
            }
        }
    }

    private void HandleJump()
    {
        if (jumpAction == null || !jumpAction.WasPressedThisFrame())
        {
            return;
        }

        // Check cooldown to prevent jump spamming
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            return;
        }

        if (!isGrounded)
        {
            return;
        }

        if (gravityController == null)
        {
            return;
        }

        Vector3 gravity = gravityController.GetGravityVector();
        if (gravity.sqrMagnitude < 0.01f)
        {
            return;
        }

        if (_rigidbody == null)
        {
            return;
        }

        // Jump in the opposite direction of gravity
        Vector3 jumpDirection = -gravity.normalized;
        _rigidbody.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
        oxygenSystem?.DepleteOxygen(jumpOxygenCost);

        // Record jump time for cooldown
        lastJumpTime = Time.time;
    }

    private void OnActiveGravitySourceChanged(GravitySourceComponent? newSource)
    {
        if (movementMode == ControllerMovementMode.Magnetized)
        {
            jumpPressStartTime = 0f;
            SetMovementMode(ControllerMovementMode.Gravity);
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressStartTime = Time.time;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        float pressDuration = Time.time - jumpPressStartTime;
        jumpPressStartTime = 0.0f;

        float jumpForce = 0.0f;
        // if (pressDuration > jumpTier3Time + jumpTier2Time + jumpTier1Time)
        // {
        //     jumpForce = jumpForceTier3;
        // }
        // else if (pressDuration > jumpTier2Time + jumpTier1Time)
        // {
        //     jumpForce = jumpForceTier2;
        // }
        // else if (pressDuration > jumpTier1Time)
        // {
        //     jumpForce = jumpForceTier1;
        // }
        if (pressDuration > jumpTier1Time)
        {
            jumpForce = jumpForceTier1;
        }

        if (jumpForce > 0.0f && _rigidbody != null && gravityController != null && playerCamera != null)
        {
            Vector3 jumpDirection;

            Vector3 gravity = gravityController.GetGravityVector();

            // If camera angle exceeds threshold and a surface is in range, jump toward it
            Ray jumpRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (cameraAngleFromGravity > jumpDirectionalAngleThreshold
                && Physics.Raycast(jumpRay, jumpTargetRaycastDistance, LayerMask.GetMask("Default")))
            {
                jumpDirection = playerCamera.transform.forward;
                gravityController.SetNextTransitionTorqueAxis(playerCamera.transform.forward);
            }
            else
            {
                jumpDirection = -1.0f * gravity.normalized;
            }

            _rigidbody.AddForce(jumpDirection * jumpForce);

            // disable active gravity source for 1.0 seconds on tier 1+ jump
            GravitySourceComponent? activeSource = gravityController.GetActiveGravitySource();
            if (activeSource != null)
            {
                // @todo trae - is this really going to be ok?
                activeSource.DisableForSeconds(jumpGravityDisableDuration);
            }
        }
    }

    private void HandleMovementSubStepped()
    {
        Vector2? moveInput = moveAction?.ReadValue<Vector2>();
        if (moveInput == null || jumpPressStartTime > 0f)
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
            desiredGravityForce = Vector3.zero;
            return;
        }

        IsSprinting = sprintIsPressed.Value && moveInput.Value.sqrMagnitude > 0.0f;

        float adjustedMoveForce = moveForce;
        if (!isGrounded)
        {
            adjustedMoveForce *= airControlMultiplier;
        }

        Vector3 direction = (transform.right * moveInput.Value.x + transform.forward * moveInput.Value.y).normalized;
        desiredGravityForce = direction * adjustedMoveForce;

        float maxSpeed = sprintIsPressed.Value ? maxRunSpeed : maxWalkSpeed;
        gravityModeMaxSpeed = isGrounded ? maxSpeed : maxJumpSpeed;
    }

    void HandleZeroGLook()
    {
        if (_rigidbody == null || playerCamera == null)
        {
            return;
        }

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

            // Cache look values for external use
            LastLookX = lookX;
            LastLookY = lookY;
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
        if (_rigidbody == null || playerCamera == null)
        {
            return;
        }

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

        // burn less oxygen the closer the player gets to maximum velocity
        if (isStabilizePressed.Value && velocity.sqrMagnitude > 1.0f)
        {
            oxygenBurnRate = 1.0f;
        }
        else if (thrustVector.sqrMagnitude > 0.0f)
        {
            oxygenBurnRate = Math.Max(1.0f - (_rigidbody.linearVelocity.magnitude / maxFlightSpeed), minOxygenBurnRate);
        }
        else
        {
            oxygenBurnRate = 0.0f;
        }

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
        if (playerCamera == null)
        {
            yield break;
        }

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

    private void OnCollisionEnter(Collision collision)
    {
        if (isGrounded) { return; }
        if (MovementMode != ControllerMovementMode.Gravity) { return; }
        if (collision.rigidbody == null || collision.rigidbody.isKinematic) { return; }
        if (_rigidbody == null) { return; }

        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, _preCollisionVelocity, airCollisionDampening);
    }

    private void OnDestroy()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
            jumpAction.canceled -= OnJumpCanceled;
        }
        if (gravityController != null)
            gravityController.ActiveSourceChanged -= OnActiveGravitySourceChanged;
    }
}