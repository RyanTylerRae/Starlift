#nullable enable

using System;
using System.Collections;
using System.Linq;
using Steamworks;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class FirstPersonController : MonoBehaviour
{
    [Header("Gravity-Based Movement")]
    public float moveForce;
    public float maxWalkSpeed;
    public float maxRunSpeed;
    private bool isGrounded = false;
    private bool isGroundedOnEdge = false;
    private bool isJumping = false;
    private Vector3 surfaceNormal = Vector3.up;
    public float groundedDistance;
    public float edgeRaycastMultiplier = 2f;
    public float jumpForce;
    public float jumpCooldown = 0.3f;
    private float lastJumpTime = -1f;
    public float airControlMultiplier = 0.1f;
    public float maxJumpSpeed;
    public float groundFriction = 10f;
    public float groundStoppingFriction = 25f;
    public bool IsSprinting { get; private set; }
    public bool IsMagnetizedWalking { get; private set; }
    public float footstepSpeed = 1.0f;
    public float footstepImpactPhaseOffset = 0.3f;
    private float gravityFootstepPhase = 0.0f;
    private float gravityFootstepPreviousPhase = 0.0f;
    private const float impactDoubleSoundDelay = 0.1f;
    private const float jumpLandingVelocityThreshold = 0.1f;

    [Header("Magnetized Movement")]
    public float maxMagnetizedWalkSpeed;
    public float magnetizeRadius = 0.5f;
    public float gravityAlignmentSpeed = 5f;
    public float jumpForceTier1;
    public float jumpForceTier2;
    public float jumpForceTier3;
    public float jumpTargetRaycastDistance = 200f;
    public float jumpDirectionalAngleThreshold = 135f;
    public float approachDuration = 0.5f;
    public float edgeJumpSearchStep = 1f;
    public float edgeJumpMaxClearDistance = 5f;
    public float edgeJumpLerpDuration = 0.15f;
    public float ignoredSourceCollisionGrace = 0.3f;
    private GravitySourceComponent? ignoredGravitySource;
    private Vector3 ignoredGravityDirection;
    private float approachTimer = 0f;
    private float ignoredSourceSetTime = 0f;
    private float distanceTraveled = 0.0f;
    private float previousBobPhase = 0.0f;
    public float headBobSpeed = 1.0f;
    public float headBobHeight = 0.2f;
    public float headBobImpactPhaseOffset = 0.3f;
    public float headBobDipDepth = 0.05f;
    public float headBobDipDuration = 0.08f;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 90f;
    public float controllerLookMultiplier = 2.0f;
    public float rollSpeed = 45f;
    public float rollDamping = 0.5f;
    public float autoRollRaycastDistance = 20f;
    public float autoRollSpeed = 10f;
    public float autoRollAngleThreshold = 45f;
    public float autoRollMinSpeed = 0f;
    public float autoRollLookAngleThreshold = 90f;

    private float xRotation = 0f;

    // Cached look values for external use (e.g., HUD)
    public float LastLookX { get; private set; }
    public float LastLookY { get; private set; }

    [Header("Zero Gravity")]
    public float stabilizeMultiplier;

    public float flightForce;
    public float maxFlightSpeed;
    public float zeroGIdleDamping = 0.5f;

    // camera angle tracking
    private float cameraAngleFromGravity = 0f;
    public float CameraAngleFromGravity { get { return cameraAngleFromGravity; } }

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

    // ZeroG roll torque computed in Update, applied in FixedUpdate
    private Vector3 _pendingRollTorque = Vector3.zero;

    [Header("Player")]
    private CharacterController? characterController;
    private PlayerInput? playerInput;
    private Rigidbody? _rigidbody;
    private GravityController? gravityController;
    private CapsuleCollider? bodyCollider;

    [Header("Camera")]
    public GameObject? cameraArm;
    private Vector3 cameraArmRestLocalPos = Vector3.zero;
    private Vector3 cameraArmLagOffset = Vector3.zero;
    private float cameraArmBobOffset = 0.0f;
    private float cameraArmDipOffset = 0.0f;
    private Coroutine? headBobDipCoroutine = null;
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
    private InputAction? interactAction;
    private InteractSystem? interactSystem = null;

    private Modifiers? modifiers = null;
    private Entity? entity = null;

    public bool IsUsingGamepad { get { return playerInput != null && playerInput.currentControlScheme == "Gamepad"; } }

    public bool ShouldDisplayJumpTarget { get { return (isGroundedOnEdge || CameraAngleFromGravity > jumpDirectionalAngleThreshold) && MovementMode == ControllerMovementMode.Magnetized; } }

    public bool IsGrounded { get { return isGrounded; } }

    [Header("Oxygen")]
    public float minOxygenBurnRate = 0.33f;
    public float jumpOxygenCost;
    private float oxygenBurnRate = 0.0f;
    public float OxygenBurnRate { get { return oxygenBurnRate; } }
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
        get { return movementMode; }
    }

    void Start()
    {
        // @trae todo - remove this
        AkUnitySoundEngine.PostEvent("play_proto_worldonfire", gameObject);

        modifiers = GetComponent<Modifiers>();
        oxygenSystem = GetComponent<OxygenSystem>();
        entity = GetComponent<Entity>();

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
        {
            gravityController.ActiveSourceChanged += OnActiveGravitySourceChanged;
        }

        SetMovementMode(ControllerMovementMode.Gravity);

        interactSystem = GetComponent<InteractSystem>();

        var diageticUI = playerInput?.actions.FindActionMap("DiageticUI");
        diageticUI?.Enable();
        interactAction = diageticUI?.FindAction("Interact");
        if (interactAction != null)
        {
            interactAction.performed += OnInteractPerformed;
        }

        if (cameraArm != null)
        {
            cameraArmRestLocalPos = cameraArm.transform.localPosition;

            Camera mainCamera = cameraArm.AddComponent<Camera>();
            mainCamera.cullingMask &= ~LayerMask.GetMask("3D_HUD");
            mainCamera.depth = -1.0f;
            var cameraData = cameraArm.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraArm.AddComponent<AkAudioListener>();
            playerCamera = mainCamera;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // composes the arm's rest pose with the current lag/bob offsets so the two effects can be driven independently
    private void ApplyCameraArmLocalPos()
    {
        if (cameraArm == null)
        {
            return;
        }

        Vector3 localPos = cameraArmRestLocalPos + cameraArmLagOffset;
        localPos.y += cameraArmBobOffset + cameraArmDipOffset;
        cameraArm.transform.localPosition = localPos;
    }

    public void SetMovementMode(ControllerMovementMode newMovementMode)
    {
        if (playerInput == null)
        {
            return;
        }

        if (newMovementMode == ControllerMovementMode.Magnetized && movementMode != ControllerMovementMode.Magnetized)
        {
            TriggerMagnetizeSound();
        }

        // entering a gravity zone from ZeroG means we're falling in from open space, so arm the
        // landing sound the same way a jump does; any other transition (e.g. leaving a magnetized
        // surface while already standing on it) shouldn't retrigger a landing
        isJumping = newMovementMode == ControllerMovementMode.Gravity && movementMode == ControllerMovementMode.ZeroG;
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
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (cameraArm != null && playerCamera != null)
            {
                transform.rotation = playerCamera.transform.rotation;
                cameraArm.transform.localRotation = Quaternion.identity;
                xRotation = 0f;
            }
        }
    }

    void Update()
    {
        if (entity == null || !entity.IsAlive)
        {
            return;
        }

        if (gravityController == null)
        {
            Debug.LogWarning("FirstPersonController does not have a sibling GravityController!");
            return;
        }

        if (ignoredGravitySource != null)
        {
            if (!gravityController.GetGravitySources().Contains(ignoredGravitySource))
            {
                ClearIgnoredGravitySource();
            }
            // if we are approaching the gravity source we left, we want to re-enable it
            else if (Time.time > ignoredSourceSetTime + ignoredSourceCollisionGrace
                && _rigidbody != null && Vector3.Dot(_rigidbody.linearVelocity, ignoredGravityDirection) > 0.01f)
            {
                approachTimer += Time.deltaTime;
                if (approachTimer >= approachDuration)
                {
                    ClearIgnoredGravitySource();
                }
            }
            else
            {
                approachTimer = 0f;
            }
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
            IsMagnetizedWalking = false;

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
        if (entity == null || !entity.IsAlive)
        {
            return;
        }

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

                if (isGrounded)
                {
                    DoGravityFootsteps(velocityTangent.magnitude * Time.fixedDeltaTime);
                }
                else
                {
                    gravityFootstepPhase = 0.0f;
                    gravityFootstepPreviousPhase = 0.0f;
                }
            }
            _preCollisionVelocity = _rigidbody.linearVelocity;
            return;
        }

        if (MovementMode == ControllerMovementMode.ZeroG)
        {
            _rigidbody.AddTorque(_pendingRollTorque, ForceMode.Force);
            _rigidbody.AddTorque(-_rigidbody.angularVelocity * rollDamping, ForceMode.Acceleration);
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

        // sweep from the starting position to the projected position; if we would hit static geometry
        // other than the surface we are walking on, clamp movement to just before the point of contact
        // and slide along the obstacle's surface with whatever movement remains, instead of stopping dead
        Vector3 startPosition = _rigidbody.position;
        Vector3 movementDelta = position - startPosition;
        if (TryGetObstacleHit(startPosition, movementDelta, gravitySource, out float obstacleDistance, out Vector3 obstacleNormal))
        {
            const float skinWidth = 0.01f;
            float clampedDistance = Mathf.Max(0f, obstacleDistance - skinWidth);
            Vector3 blockedPosition = startPosition + movementDelta.normalized * clampedDistance;
            Vector3 remainingDelta = movementDelta - movementDelta.normalized * clampedDistance;
            Vector3 tangentDelta = Vector3.ProjectOnPlane(remainingDelta, obstacleNormal);

            position = blockedPosition + tangentDelta;
            velocity = Vector3.ProjectOnPlane(velocity, obstacleNormal);
        }

        // track distance for head bobbing. because it is a function of sine, we can just repeat the period over and over again
        distanceTraveled += totalDistance * headBobSpeed;
        while (distanceTraveled > 2.0f * Math.PI)
        {
            distanceTraveled -= 2.0f * (float)Math.PI;
        }

        // apply final state to rigidbody, included the new adjusted velocity
        _rigidbody.position = position;
        _rigidbody.linearVelocity = gravitySourceVelocity + velocity;
        _preCollisionVelocity = _rigidbody.linearVelocity;

        DoMagnetizedHeadBob(totalDistance);
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
        isGroundedOnEdge = false;

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

        // while jumping, ignore the generous ground-proximity raycast until velocity is confirmed
        // moving back down towards the surface. requiring a small positive threshold (rather than
        // just "not ascending") also covers the frame right after the jump impulse is applied but
        // before physics has integrated it yet, where velocity still reads as ~0
        if (isJumping)
        {
            float velocityAlongGravity = _rigidbody != null ? Vector3.Dot(_rigidbody.linearVelocity, gravityDir) : 0f;
            if (velocityAlongGravity <= jumpLandingVelocityThreshold)
            {
                return;
            }
        }

        int groundMask = LayerMask.GetMask("Default");

        float halfRadius = bodyCollider != null ? bodyCollider.radius * bodyCollider.transform.lossyScale.x : 0f;

        // cast from the base of the capsule rather than the object's own transform, since the
        // collider's center/height offset means transform.position isn't at the character's feet
        Vector3 footPosition = bodyCollider != null
            ? transform.TransformPoint(bodyCollider.center) - transform.up * (bodyCollider.height * bodyCollider.transform.lossyScale.y / 2f)
            : transform.position;

        // perform the middle raycast, this can give us a hint to determine if we are over an edge or not,
        // and also allows us to early-out walking on magnetized surfaces with a steep angle
        if (Physics.Raycast(new Ray(footPosition, gravityDir), out RaycastHit centerHit, groundedDistance, groundMask))
        {
            isGrounded = true;
            surfaceNormal = centerHit.normal;

            if (isJumping)
            {
                TriggerLandingSound();
                isJumping = false;
            }

            // check to see if we are near an edge
            Vector3 centerEdgeOrigin = footPosition + transform.forward * (halfRadius * edgeRaycastMultiplier);
            if (!Physics.Raycast(new Ray(centerEdgeOrigin, gravityDir), groundedDistance, groundMask))
            {
                isGroundedOnEdge = true;
            }

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

                Vector3 origin = footPosition + transform.right * ((i - 1) * halfRadius) + transform.forward * ((j - 1) * halfRadius);
                if (Physics.Raycast(new Ray(origin, gravityDir), groundedDistance, groundMask))
                {
                    ++numHits;
                    ray += origin - footPosition;
                    isGrounded = true;
                }
            }
        }

        if (isGrounded)
        {
            if (isJumping)
            {
                TriggerLandingSound();
                isJumping = false;
            }

            // this ray is diagonal and should point back towards the wall we are walking off of from the center
            ray /= numHits;
            ray += gravityDir;
            ray.Normalize();

            if (Physics.Raycast(new Ray(footPosition, ray), out RaycastHit edgeHit, groundedDistance * 2f, groundMask))
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
        isJumping = true;
    }

    private void OnActiveGravitySourceChanged(GravitySourceComponent? newSource)
    {
        if (movementMode == ControllerMovementMode.Magnetized)
        {
            jumpPressStartTime = 0f;
            SetMovementMode(ControllerMovementMode.Gravity);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        interactSystem?.TryInteractFirst();
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
            Vector3 jumpDirection = new();

            Vector3 gravity = gravityController.GetGravityVector();
            GravitySourceComponent? activeSource = gravityController.GetActiveGravitySource();

            // If camera angle exceeds threshold and a surface is in range, jump toward it —
            // unless the hit surface is the one we're already standing on
            Ray jumpRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (isGroundedOnEdge || cameraAngleFromGravity > jumpDirectionalAngleThreshold)
            {
                if (!Physics.Raycast(jumpRay, out RaycastHit jumpHit, jumpTargetRaycastDistance, LayerMask.GetMask("Default"))
                    || jumpHit.collider.GetComponentInParent<GravitySourceComponent>() != activeSource)
                {
                    jumpDirection = playerCamera.transform.forward;
                    gravityController.SetNextTransitionTorqueAxis(playerCamera.transform.forward);
                }
            }
            // jump straight upwards
            // else
            // {
            //     jumpDirection = -1.0f * gravity.normalized;
            // }

            if (jumpDirection.AlmostZero())
            {
                return;
            }

            if (isGroundedOnEdge && bodyCollider != null && _rigidbody != null)
            {
                var clearPosition = TryFindEdgeJumpClearPosition(jumpDirection);
                if (clearPosition.HasValue)
                {
                    Vector3 visualOffset = transform.position - clearPosition.Value;
                    bodyCollider.enabled = false;
                    transform.position = clearPosition.Value;
                    _rigidbody.position = clearPosition.Value;
                    bodyCollider.enabled = true;

                    if (cameraArm != null)
                    {
                        StartCoroutine(LagCameraFromEdgeJump(visualOffset, edgeJumpLerpDuration));
                    }
                }
            }

            _rigidbody?.AddForce(jumpDirection * jumpForce);

            if (activeSource != null)
            {
                ignoredGravitySource = activeSource;
                ignoredGravityDirection = gravity.normalized;
                ignoredSourceSetTime = Time.time;
                activeSource.isGravityEnabled = false;
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

        IsMagnetizedWalking = moveInput.Value.sqrMagnitude > 0f;

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

        float rollInput = rotateLeftInput - rotateRightInput;
        if (Mathf.Abs(rollInput) > 0.01f)
        {
            _pendingRollTorque = transform.forward * rollInput * rollSpeed;
        }
        else
        {
            Ray forwardRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (_rigidbody.linearVelocity.magnitude >= autoRollMinSpeed
                && Physics.Raycast(forwardRay, out RaycastHit surfaceHit, autoRollRaycastDistance)
                && Vector3.Angle(transform.forward, -surfaceHit.normal) > autoRollAngleThreshold
                && Vector3.Angle(_rigidbody.linearVelocity, forwardRay.direction) < autoRollLookAngleThreshold)
            {
                Vector3 normalOnPlane = Vector3.ProjectOnPlane(surfaceHit.normal, transform.forward);
                if (normalOnPlane.sqrMagnitude > 0.001f)
                {
                    float t = surfaceHit.distance / autoRollRaycastDistance;
                    float easing = Mathf.Log(1f + (1f - t) * (Mathf.Exp(1f) - 1f));
                    float angle = Vector3.SignedAngle(transform.up, normalOnPlane.normalized, transform.forward);
                    _pendingRollTorque = transform.forward * angle * autoRollSpeed * easing;
                }
                else
                {
                    _pendingRollTorque = Vector3.zero;
                }
            }
            else
            {
                _pendingRollTorque = Vector3.zero;
            }
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

        if (!isStabilizePressed.Value && thrustVector.sqrMagnitude == 0f)
        {
            _rigidbody.AddForce(-_rigidbody.linearVelocity * zeroGIdleDamping, ForceMode.Acceleration);
        }

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

    private Vector3? TryFindEdgeJumpClearPosition(Vector3 jumpDirection)
    {
        if (bodyCollider == null) { return null; }

        float scaledHeight = bodyCollider.height * bodyCollider.transform.lossyScale.y;
        float scaledRadius = bodyCollider.radius * bodyCollider.transform.lossyScale.x;
        Vector3 worldCenter = transform.TransformPoint(bodyCollider.center);
        Vector3 p1Base = worldCenter + transform.up * (scaledHeight / 2f - scaledRadius);
        Vector3 p2Base = worldCenter - transform.up * (scaledHeight / 2f - scaledRadius);
        int groundMask = LayerMask.GetMask("Default");

        // Step forward until finding a position clear of ground geometry
        float low = 0f;
        float high = -1f;
        for (float t = edgeJumpSearchStep; t <= edgeJumpMaxClearDistance; t += edgeJumpSearchStep)
        {
            if (!HasCapsuleOverlap(p1Base + jumpDirection * t, p2Base + jumpDirection * t, scaledRadius, groundMask))
            {
                high = t;
                break;
            }
            low = t;
        }

        if (high < 0f)
        {
            return null;
        }

        // Binary search to narrow to the minimum clear distance
        for (int i = 0; i < 5; i++)
        {
            float mid = (low + high) / 2f;
            if (HasCapsuleOverlap(p1Base + jumpDirection * mid, p2Base + jumpDirection * mid, scaledRadius, groundMask))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        // Check if the clear position introduces any other collisions
        Vector3 clearP1 = p1Base + jumpDirection * high;
        Vector3 clearP2 = p2Base + jumpDirection * high;
        if (HasCapsuleOverlap(clearP1, clearP2, scaledRadius, ~0))
        {
            return null;
        }

        return transform.position + jumpDirection * high;
    }

    private bool HasCapsuleOverlap(Vector3 p1, Vector3 p2, float radius, int layerMask)
    {
        Collider[] overlaps = Physics.OverlapCapsule(p1, p2, radius, layerMask, QueryTriggerInteraction.Ignore);
        foreach (Collider c in overlaps)
        {
            if (c != bodyCollider) { return true; }
        }
        return false;
    }

    private bool TryGetObstacleHit(Vector3 startPosition, Vector3 movementDelta, GravitySourceComponent gravitySource, out float hitDistance, out Vector3 hitNormal)
    {
        hitDistance = 0f;
        hitNormal = Vector3.zero;

        if (bodyCollider == null)
        {
            return false;
        }

        float distance = movementDelta.magnitude;
        if (distance < 0.0001f)
        {
            return false;
        }

        float scaledHeight = bodyCollider.height * bodyCollider.transform.lossyScale.y;
        float scaledRadius = bodyCollider.radius * bodyCollider.transform.lossyScale.x;
        Vector3 worldCenter = startPosition + transform.TransformVector(bodyCollider.center);
        Vector3 p1 = worldCenter + transform.up * (scaledHeight / 2f - scaledRadius);
        Vector3 p2 = worldCenter - transform.up * (scaledHeight / 2f - scaledRadius);
        int groundMask = LayerMask.GetMask("Default");

        RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, scaledRadius, movementDelta / distance, distance, groundMask, QueryTriggerInteraction.Ignore);

        bool foundHit = false;
        float closestDistance = float.MaxValue;
        Vector3 closestNormal = Vector3.zero;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == bodyCollider)
            {
                continue;
            }

            // the surface we are actively walking on is expected to overlap us; only halt for other geometry.
            // gravity sources are nested under their own surface's collider (not a shared scene-graph root),
            // so walk up from the gravity source instead of comparing transform.root
            if (gravitySource.transform.IsChildOf(hit.collider.transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestNormal = hit.normal;
                foundHit = true;
            }
        }

        if (foundHit)
        {
            hitDistance = closestDistance;
            hitNormal = closestNormal;
        }

        return foundHit;
    }

    private IEnumerator LagCameraFromEdgeJump(Vector3 worldOffset, float duration)
    {
        if (cameraArm == null)
        {
            yield break;
        }

        Quaternion futureBodyRot = playerCamera != null ? playerCamera.transform.rotation : transform.rotation;
        cameraArmLagOffset = Quaternion.Inverse(futureBodyRot) * worldOffset;
        ApplyCameraArmLocalPos();

        yield return null;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float tEased = t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
            Vector3 localOffset = transform.InverseTransformVector(worldOffset);
            cameraArmLagOffset = Vector3.Lerp(localOffset, Vector3.zero, tEased);
            ApplyCameraArmLocalPos();
            yield return null;
        }

        cameraArmLagOffset = Vector3.zero;
        ApplyCameraArmLocalPos();
    }

    private void ClearIgnoredGravitySource()
    {
        if (ignoredGravitySource != null)
        {
            ignoredGravitySource.isGravityEnabled = true;
            ignoredGravitySource = null;
        }
        approachTimer = 0f;
    }

    private void DoMagnetizedHeadBob(float distanceThisFrame)
    {
        if (Mathf.Abs(distanceThisFrame) < 0.01f)
        {
            // @todo trae - ease this to 0 instead
            distanceTraveled = 0.0f;
        }

        float impactPhase = Mathf.PI - headBobImpactPhaseOffset;
        if (previousBobPhase < impactPhase && distanceTraveled >= impactPhase)
        {
            TriggerHeadBobDip();

            AkUnitySoundEngine.PostEvent("play_footstep_thud", gameObject);
        }
        previousBobPhase = distanceTraveled;

        // head bob
        float cosVal = Mathf.Cos(distanceTraveled - 1.0f);
        cameraArmBobOffset = 0.5f * headBobHeight * cosVal;

        ApplyCameraArmLocalPos();
    }

    private void TriggerLandingSound()
    {
        StartCoroutine(DoDelayedDoubleSound("play_footstep_soft"));
    }

    private void TriggerMagnetizeSound()
    {
        StartCoroutine(DoDelayedDoubleSound("play_footstep_thud"));
    }

    private IEnumerator DoDelayedDoubleSound(string eventName)
    {
        AkUnitySoundEngine.PostEvent(eventName, gameObject);
        yield return new WaitForSeconds(impactDoubleSoundDelay);
        AkUnitySoundEngine.PostEvent(eventName, gameObject);
    }

    // fires a footstep sound while walking on the ground in normal Gravity mode, no head bob involved
    private void DoGravityFootsteps(float distanceThisFrame)
    {
        if (Mathf.Abs(distanceThisFrame) < 0.01f)
        {
            gravityFootstepPhase = 0.0f;
            gravityFootstepPreviousPhase = 0.0f;
            return;
        }

        gravityFootstepPhase += distanceThisFrame * footstepSpeed;
        while (gravityFootstepPhase > 2.0f * Math.PI)
        {
            gravityFootstepPhase -= 2.0f * (float)Math.PI;
        }

        float impactPhase = Mathf.PI - footstepImpactPhaseOffset;
        if (gravityFootstepPreviousPhase < impactPhase && gravityFootstepPhase >= impactPhase)
        {
            AkUnitySoundEngine.PostEvent("play_footstep_soft", gameObject);
        }
        gravityFootstepPreviousPhase = gravityFootstepPhase;
    }

    private void TriggerHeadBobDip()
    {
        if (headBobDipCoroutine != null)
        {
            StopCoroutine(headBobDipCoroutine);
        }
        headBobDipCoroutine = StartCoroutine(DoHeadBobDip());
    }

    private IEnumerator DoHeadBobDip()
    {
        float elapsed = 0.0f;
        while (elapsed < headBobDipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / headBobDipDuration);
            cameraArmDipOffset = -headBobDipDepth * Mathf.Sin(t * Mathf.PI);
            ApplyCameraArmLocalPos();
            yield return null;
        }

        cameraArmDipOffset = 0.0f;
        headBobDipCoroutine = null;
        ApplyCameraArmLocalPos();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ignoredGravitySource != null && Time.time > ignoredSourceSetTime + ignoredSourceCollisionGrace)
        {
            var source = collision.collider.GetComponentInParent<GravitySourceComponent>();
            if (source == ignoredGravitySource)
            {
                ClearIgnoredGravitySource();
            }
        }

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
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
        }
        if (gravityController != null)
        {
            gravityController.ActiveSourceChanged -= OnActiveGravitySourceChanged;
        }
    }
}