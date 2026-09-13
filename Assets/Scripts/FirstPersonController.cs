#nullable enable

using System;
using System.Collections;
using System.Linq;
using Steamworks;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
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
    // true from the moment a magnetized jump launches until HandleGrounded() confirms a real
    // landing while Magnetized - see the comment where it's set for why this can't just be isJumping
    private bool magnetizedJumpInProgress = false;
    // the gravity source we're currently stuck to while Magnetized - lets HandleGrounded() tell a
    // genuine new landing apart from a re-collision blip against the surface we're already attached
    // to (e.g. from the body reorienting toward the new "up" and briefly clipping the geometry),
    // which was re-triggering landing momentum/rotation-reset for no real landing
    private GravitySourceComponent? attachedMagnetizedSource = null;
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
    public float magnetizedAcceleration = 6f;
    // rate used instead of magnetizedAcceleration whenever the target speed is lower than the
    // current speed (releasing input, or a corner/landing setting a lower desired speed) - see
    // HandleMovementSubStepped
    public float magnetizedDeceleration = 3f;
    // the boosted landing speed at (or above) boostedMaxFlightSpeed - incoming landing speed maps
    // linearly from 0 up to this ceiling, not a multiplier on the raw speed
    public float magnetizedLandingBoostMaxSpeed = 6f;
    [Range(0f, 1f)]
    public float magnetizedLandingMomentumCameraAlignment = 0.6f;
    // surface-normal angle change (degrees) per FixedUpdate beyond which we treat it as a discrete
    // corner/edge rather than gradual curvature or a slowly spinning platform
    public float magnetizedCornerAngleThreshold = 15f;
    // tangential speed above which hitting a sharp corner detaches the player from the surface
    // instead of trying to carry their momentum around it - kept well above ordinary walking
    // speed so normal traversal always rotates smoothly around corners instead of detaching
    public float magnetizedCornerDetachSpeed = 20f;
    // fixed speed used while actively crossing a discrete corner/edge (input direction, not
    // whatever momentum was carried into it) - consistent regardless of how fast the player
    // happened to be moving beforehand, rather than a momentum carry that read as sluggish
    public float magnetizedCornerSpeed = 5f;
    public float magnetizeRadius = 0.5f;
    public float magnetizedSoundRange = 5f;
    private bool isMagnetizedSoundPlaying = false;
    private GravitySourceComponent[] magnetizableSources = System.Array.Empty<GravitySourceComponent>();
    public float gravityAlignmentSpeed = 5f;
    public float maxJumpForce = 0f;
    public float jumpTargetRaycastDistance = 200f;
    public float jumpDirectionalAngleThreshold = 135f;
    public float approachDuration = 0.5f;
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

    [Header("Jump Thrust")]
    public float minJumpThrust = 0f;
    public float maxJumpThrust = 0f;
    public float minJumpForwardThrust = 0f;
    public float maxJumpForwardThrust = 0f;
    private float jumpCounterThrust = 0f;
    public float jumpThrustPhaseOneDuration = 0f;
    public float jumpThrustImpulseDuration = 0f;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 90f;
    public float controllerLookMultiplier = 2.0f;
    public float rollSpeed = 45f;
    public float rollDamping = 0.5f;
    public float autoRollRaycastDistance = 20f;
    public float autoRollSettleTime = 0.4f;
    public float autoRollAngleThreshold = 45f;
    public float autoRollMinSpeed = 0f;
    public float autoRollLookAngleThreshold = 90f;

    private float xRotation = 0f;

    // the actual rendered Camera lives on this child of cameraArm and chases cameraArm's world
    // pose every LateUpdate instead of being locked to it - see the comment where it's created
    // in Start() for why
    private Transform? cameraFollowTransform;
    public float cameraFollowPositionSpeed = 20f;
    public float cameraFollowRotationSpeed = 20f;

    // Cached look values for external use (e.g., HUD)
    public float LastLookX { get; private set; }
    public float LastLookY { get; private set; }

    [Header("Zero Gravity")]
    public float stabilizeMultiplier;

    public float flightForce;
    public float maxFlightSpeed;
    public float boostedMaxFlightSpeed;
    public float speedEaseBackRate = 6f;
    public float zeroGIdleDamping = 0.5f;
    private bool hasPlayedStabilizedSound = false;
    private bool isRotationThrustPlaying = false;
    private bool isThrustForwardPlaying = false;
    private bool isJumpThrustSoundPlaying = false;
    private float jumpThrustOxygenContribution = 0.0f;
    private const float stabilizeSoundVelocityThreshold = 0.3f;
    private const float stabilizeSoundAngularThreshold = 0.3f;

    // camera angle tracking
    private float cameraAngleFromGravity = 0f;
    public float CameraAngleFromGravity { get { return cameraAngleFromGravity; } }

    // jump charge
    private float jumpPressStartTime = 0f;
    public float jumpChargeTime = 0f;

    [Header("Physics Sub-stepping")]
    public float substepDistance = 0.01f;

    [Header("Collision")]
    [Range(0f, 1f)]
    public float airCollisionDampening = 0.85f;
    public float maxDepenetrationVelocity = 2f;

    // Desired movement velocity from input (used in FixedUpdate)
    private Vector3 desiredMovementVelocity = Vector3.zero;
    private Vector3 _preCollisionVelocity = Vector3.zero;

    // Magnetized mode's own authoritative velocity (relative to the active gravity source),
    // seeded once on entering Magnetized mode and otherwise never read back from
    // _rigidbody.linearVelocity. The character's collider is in real contact with the surface, and
    // PhysX's own contact/friction response resets components of the rigidbody's velocity between
    // FixedUpdate calls - reading any part of it back as truth (tangential OR vertical) made that
    // part collapse to zero every frame, so gravity/acceleration could never accumulate.
    private Vector3 magnetizedVelocity = Vector3.zero;

    // the verticalAxis used the last time magnetizedVelocity was decomposed while grounded - lets us
    // detect the surface reorienting (a spinning platform, or just walking over curved/uneven
    // terrain) and rotate the stored velocity to match, instead of it bleeding into "into surface"
    // and getting dropped every frame the normal moves. Vector3.zero means "not yet tracked".
    private Vector3 lastMagnetizedVerticalAxis = Vector3.zero;

    // the gravity normal auto correction (body rotation snap + depenetration + camera lag, see
    // Update()) should only run once when something actually changes the surface we're on - a
    // genuine landing, or a corner crossing - not every single frame regardless of whether the
    // surface changed at all. Running it unconditionally every frame was still a no-op in the
    // common case (already aligned), but repeatedly re-deriving and reapplying the same
    // correction (including a real, if tiny, depenetration raycast + push every tick) is
    // needless work and a needless source of drift; this flag makes it fire only when needed.
    private bool pendingGravityAlignment = true;

    // Gravity mode: force direction and speed cap stored in Update, applied in FixedUpdate
    private Vector3 desiredGravityForce = Vector3.zero;
    private float gravityModeMaxSpeed = 0f;

    // ZeroG roll torque computed in Update, applied in FixedUpdate
    private Vector3 _pendingRollTorque = Vector3.zero;
    private Vector3 _pendingAutoRollAcceleration = Vector3.zero;

    [Header("Player")]
    private CharacterController? characterController;
    private PlayerInput? playerInput;
    private Rigidbody? _rigidbody;
    private GravityController? gravityController;
    private CapsuleCollider? bodyCollider;

    [Header("Camera")]
    public GameObject? cameraArm;
    private Vector3 cameraArmRestLocalPos = Vector3.zero;
    private float cameraArmBobOffset = 0.0f;
    private float cameraArmDipOffset = 0.0f;
    private Coroutine? headBobDipCoroutine = null;
    private Coroutine? jumpThrustCoroutine = null;
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

    private bool canLook = true;
    private bool canStabilize = true;
    private bool canThrust = true;

    public void SetLookEnabled(bool enabled)
    {
        canLook = enabled;
    }

    public void SetStabilizeEnabled(bool enabled)
    {
        canStabilize = enabled;
    }

    public void SetThrustEnabled(bool enabled)
    {
        canThrust = enabled;
    }

    public bool IsUsingGamepad { get { return playerInput != null && playerInput.currentControlScheme == "Gamepad"; } }

    public bool ShouldDisplayJumpTarget { get { return (isGroundedOnEdge || CameraAngleFromGravity > jumpDirectionalAngleThreshold) && MovementMode == ControllerMovementMode.Magnetized; } }

    public bool IsGrounded { get { return isGrounded; } }
    public bool IsGroundedOnEdge { get { return isGroundedOnEdge; } }

    [Header("Oxygen")]
    public float minOxygenBurnRate = 0.33f;
    public float jumpOxygenCost;
    public float magnetizedJumpOxygenCost;
    public float rotationOxygenBurnRate = 0.5f;
    private float oxygenBurnRate = 0.0f;
    public float OxygenBurnRate { get { return oxygenBurnRate; } }
    private float rotationOxygenContribution = 0.0f;
    private OxygenSystem? oxygenSystem = null;
    private Vector3 _previousZeroGVelocity = Vector3.zero;

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

        magnetizableSources = FindObjectsByType<GravitySourceComponent>(FindObjectsSortMode.None)
            .Where(source => source.isMagnetized)
            .ToArray();

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

            // cameraArm is the authoritative pivot - all gameplay logic (movement direction,
            // thrust, aiming, ZeroG rotation control) reads from cameraArm.transform directly and
            // must stay perfectly instant with input. The actual rendered Camera instead lives on
            // this separate, deliberately UNPARENTED object, which chases cameraArm's world pose
            // every LateUpdate - that's what makes the capsule's instant rotation/position snaps
            // (landing, corners) smooth on screen without adding any lag to controls. It must not
            // be parented under cameraArm (or anything else that moves): a parented child's world
            // position/rotation is re-derived from its fixed local offset against the PARENT'S
            // current transform on every read, so it would still jump instantly the moment
            // cameraArm snaps, no matter what we Lerp/Slerp it towards here.
            GameObject cameraFollowObject = new GameObject("CameraFollow");
            cameraFollowObject.transform.SetPositionAndRotation(cameraArm.transform.position, cameraArm.transform.rotation);
            cameraFollowTransform = cameraFollowObject.transform;

            Camera mainCamera = cameraFollowObject.AddComponent<Camera>();
            mainCamera.cullingMask &= ~LayerMask.GetMask("3D_HUD");
            mainCamera.depth = -1.0f;

            var cameraData = cameraFollowObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            mainCamera.farClipPlane = 1500.0f;

            FogCamera fogCamera = mainCamera.AddComponent<FogCamera>();
            fogCamera.fogStartDistance = 75.0f;
            fogCamera.fogColor = Color.black;
            fogCamera.fogPower = 0.4f;

            PixelateCamera pxCamera = mainCamera.AddComponent<PixelateCamera>();
            pxCamera.pixelsPerScreenHeight = 256;

            cameraFollowObject.AddComponent<AkAudioListener>();
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

        Vector3 localPos = cameraArmRestLocalPos;
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

            // seed our own authoritative velocity once, from whatever real momentum the rigidbody
            // currently has (e.g. carried in from ZeroG flight or a Gravity-mode jump) - after this,
            // FixedUpdate never reads _rigidbody.linearVelocity back as truth again (see field comment)
            magnetizedVelocity = _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
        }

        if (newMovementMode != ControllerMovementMode.ZeroG)
        {
            StopRotationThrustSound();
            StopThrustForwardSound();
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
            if (_rigidbody != null)
            {
                _previousZeroGVelocity = _rigidbody.linearVelocity;
            }

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
                transform.rotation = cameraArm.transform.rotation;
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

            float jumpNorm = Math.Clamp(pressDuration / jumpChargeTime, 0.0f, 1.0f);
            modifiers.Set(ModifierType.JumpCharge, jumpNorm);
        }

        Vector3 gravity = gravityController.GetGravityVector();

        // Calculate camera angle from gravity direction
        if (gravity.sqrMagnitude > 0.01f && cameraArm != null)
        {
            cameraAngleFromGravity = Vector3.Angle(cameraArm.transform.forward, gravity);
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
            // we don't burn extra oxygen when walking on a surface, but a jump-thrust launched while
            // still magnetized should keep burning until the coroutine finishes
            oxygenBurnRate = jumpThrustOxygenContribution;
            rotationOxygenContribution = 0.0f;
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

            // gravity normal auto correction while attached. Two distinct cases share this block:
            // gradual curvature (a curved or slightly uneven surface) needs the capsule to keep
            // tracking the normal every frame, just eased in smoothly so it's never a big enough
            // single-frame jump to need depenetration or a camera-lag hide; a genuine landing or
            // corner crossing (see pendingGravityAlignment) is a large, discrete jump instead, so
            // it gets snapped instantly for physical correctness plus the depenetration/camera-lag
            // treatment. Previously the instant snap was the ONLY path, gated on
            // pendingGravityAlignment alone - which meant the capsule never re-aligned at all
            // between events, so walking any distance across a surface with gradual curvature left
            // the camera pointing further and further from the true normal ("stuck" at a stale angle).
            Vector3 upVector = -gravity.normalized;
            if (upVector.sqrMagnitude > 0.01f && !pendingGravityAlignment)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * gravityAlignmentSpeed);
            }

            if (upVector.sqrMagnitude > 0.01f && pendingGravityAlignment)
            {
                pendingGravityAlignment = false;
                transform.rotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;

                // an instant rotation isn't swept, so a large single-frame angle change (e.g.
                // snapping onto a new face at a corner) can leave the capsule's lowest point
                // embedded past the surface it just rotated onto. Find the exact penetration
                // depth along the new normal (via a single raycast from the capsule's lowest
                // point) and push straight back out by that amount - a direct, exact correction
                // rather than letting PhysX's own depenetration resolve it, which can shove the
                // capsule out through the far side of thin geometry instead of back the way it came.
                if (bodyCollider != null)
                {
                    float scaledHeight = bodyCollider.height * bodyCollider.transform.lossyScale.y;
                    Vector3 worldCenter = transform.position + transform.TransformVector(bodyCollider.center);
                    Vector3 lowestPoint = worldCenter - upVector * (scaledHeight / 2f);

                    // cast from above the lowest point so we still find the surface even if the
                    // rotation already pushed that point below it
                    float castLift = groundedDistance;
                    int groundMask = LayerMask.GetMask("Default");
                    if (Physics.Raycast(lowestPoint + upVector * castLift, -upVector, out RaycastHit penetrationHit, castLift * 2f, groundMask))
                    {
                        float penetrationDepth = Vector3.Dot(penetrationHit.point - lowestPoint, upVector);
                        if (penetrationDepth > 0f)
                        {
                            Debug.Log($"TRAE rotation depenetration at t={Time.time:F3}, upVector={upVector}, lowestPoint={lowestPoint}, hit.point={penetrationHit.point}, hit.collider={penetrationHit.collider.name}, penetrationDepth={penetrationDepth:F3}, positionBefore={transform.position}");

                            // push out a bit further than the exact measured depth - landing
                            // exactly on the boundary leaves us one float-precision nudge away
                            // from re-penetrating next frame - and cancel the velocity that
                            // drove us in, otherwise the same speed just carries us straight
                            // back through on the very next tick
                            transform.position += upVector * (penetrationDepth * 1.1f);

                            Debug.Log($"TRAE rotation depenetration result at t={Time.time:F3}, positionAfter={transform.position}");

                            float intoSurfaceSpeed = Vector3.Dot(magnetizedVelocity, upVector);
                            if (intoSurfaceSpeed < 0f)
                            {
                                magnetizedVelocity -= intoSurfaceSpeed * upVector;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"TRAE rotation depenetration raycast MISSED at t={Time.time:F3}, upVector={upVector}, lowestPoint={lowestPoint}, castLift={castLift:F3}");
                    }
                }
            }

            if (cameraArm != null)
            {
                cameraArm.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                ApplyCameraArmLocalPos();
            }
        }
        else
        {
            HandleZeroGLook();
        }

        HandleMagnetizedSound();
    }

    // runs after every Update() (and any FixedUpdate ticks) this frame, once cameraArm's pose is
    // fully finalized, so the rendered camera always chases the latest, correct target
    void LateUpdate()
    {
        if (cameraArm == null || cameraFollowTransform == null)
        {
            return;
        }

        cameraFollowTransform.position = Vector3.Lerp(cameraFollowTransform.position, cameraArm.transform.position, Time.deltaTime * cameraFollowPositionSpeed);
        cameraFollowTransform.rotation = Quaternion.Slerp(cameraFollowTransform.rotation, cameraArm.transform.rotation, Time.deltaTime * cameraFollowRotationSpeed);
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
            _rigidbody.AddTorque(_pendingAutoRollAcceleration, ForceMode.Acceleration);
            _rigidbody.AddTorque(-_rigidbody.angularVelocity * rollDamping, ForceMode.Acceleration);
            HandleZeroGMovement();
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

        // separate into vertical and horizontal components along gravity. Uses our own persistent
        // magnetizedVelocity, never _rigidbody.linearVelocity - see field comment.
        Vector3 position = _rigidbody.position;
        Vector3 gravityVector = gravitySource.GetGravityVector(position);
        Vector3 verticalAxis = (isGrounded && surfaceNormal.sqrMagnitude > 0.01f) ? surfaceNormal : -gravityVector.normalized;

        if (isGrounded && lastMagnetizedVerticalAxis.sqrMagnitude > 0.01f)
        {
            float normalAngleDelta = Vector3.Angle(lastMagnetizedVerticalAxis, verticalAxis);
            bool isCorner = normalAngleDelta > magnetizedCornerAngleThreshold;

            if (isCorner)
            {
                pendingGravityAlignment = true;
                Debug.Log($"TRAE corner detected at t={Time.time:F3}, angleDelta={normalAngleDelta:F2}, magnetizedVelocity={magnetizedVelocity} (magnitude={magnetizedVelocity.magnitude:F2}), desiredMovementVelocity={desiredMovementVelocity} (magnitude={desiredMovementVelocity.magnitude:F2}), lastAxis={lastMagnetizedVerticalAxis}, newAxis={verticalAxis}");
            }

            // a discrete corner/edge taken fast enough that rigidly carrying momentum around it
            // would be physically wrong - and previously produced compounding oscillation with
            // the obstacle-sliding logic - so let go of the surface instead of wrenching momentum
            // around an impossible turn, continuing on the existing trajectory like flying off an
            // edge. magnetizedCornerDetachSpeed is set well above ordinary walking speed, so this
            // is reserved for genuinely fast corner-cutting, not normal traversal.
            if (isCorner && magnetizedVelocity.magnitude > magnetizedCornerDetachSpeed)
            {
                Debug.Log($"TRAE corner detach at t={Time.time:F3}, speed={magnetizedVelocity.magnitude:F2} > detachSpeed={magnetizedCornerDetachSpeed:F2}");
                DetachFromMagnetizedSurface(gravitySource, gravityVector, gravitySourceVelocity + magnetizedVelocity);
                return;
            }

            // "move the capsule to the other wall": at a discrete corner the position that was
            // flush against the old face isn't flush against the new one - rather than letting
            // that gap/overlap play out through normal collision response, snap position to rest
            // exactly against the new wall instantly (identical technique to the rotation
            // depenetration correction above: find the capsule's lowest point along the new
            // normal, raycast once to find the real surface, translate by the exact gap). The
            // camera has no independent position of its own - it's always just cameraArm's rest
            // offset plus bob/dip (see ApplyCameraArmLocalPos) - so it moves with the capsule
            // automatically here with no compensation or lag needed.
            if (isCorner && bodyCollider != null)
            {
                float scaledHeight = bodyCollider.height * bodyCollider.transform.lossyScale.y;
                Vector3 worldCenter = position + transform.TransformVector(bodyCollider.center);
                Vector3 lowestPoint = worldCenter - verticalAxis * (scaledHeight / 2f);

                float castLift = groundedDistance;
                int groundMask = LayerMask.GetMask("Default");
                if (Physics.Raycast(lowestPoint + verticalAxis * castLift, -verticalAxis, out RaycastHit wallHit, castLift * 2f, groundMask))
                {
                    Vector3 positionDelta = verticalAxis * Vector3.Dot(wallHit.point - lowestPoint, verticalAxis);
                    position += positionDelta;

                    Debug.Log($"TRAE corner position-snap at t={Time.time:F3}, verticalAxis={verticalAxis}, lowestPoint={lowestPoint}, wallHit.point={wallHit.point}, wallHit.collider={wallHit.collider.name}, positionDelta={positionDelta} (magnitude={positionDelta.magnitude:F3})");
                }
                else
                {
                    Debug.Log($"TRAE corner position-snap raycast MISSED at t={Time.time:F3}, verticalAxis={verticalAxis}, lowestPoint={lowestPoint}, castLift={castLift:F3}");
                }
            }

            // otherwise - gradual curvature, a slowly spinning platform, or a sharp corner taken
            // at normal walking speed - rotate our persistent velocity to match the surface's new
            // local orientation, so momentum doesn't bleed away by getting misread as "into
            // surface" and dropped every time the normal moves
            Quaternion surfaceRotationDelta = Quaternion.FromToRotation(lastMagnetizedVerticalAxis, verticalAxis);
            magnetizedVelocity = surfaceRotationDelta * magnetizedVelocity;

            // crossing a discrete corner/edge while actively holding movement input snaps
            // straight to a fixed traversal speed in the input direction instead of carrying
            // over whatever momentum was present beforehand - that momentum carry is what read
            // as sluggish/inconsistent. This re-triggers every physics tick the corner condition
            // holds, so it stays at this speed for as long as the corner is actually being
            // crossed and input is held, then falls back to normal acceleration afterward.
            if (isCorner && desiredMovementVelocity.sqrMagnitude > 0.0001f)
            {
                magnetizedVelocity = desiredMovementVelocity.normalized * magnetizedCornerSpeed;
                Debug.Log($"TRAE corner speed applied at t={Time.time:F3}, result magnetizedVelocity={magnetizedVelocity} (magnitude={magnetizedVelocity.magnitude:F2})");
            }
            else if (isCorner)
            {
                Debug.Log($"TRAE corner speed NOT applied (no input) at t={Time.time:F3}, magnetizedVelocity after rotation={magnetizedVelocity} (magnitude={magnetizedVelocity.magnitude:F2})");
            }
        }
        lastMagnetizedVerticalAxis = verticalAxis;

        float verticalSpeed = Vector3.Dot(magnetizedVelocity, verticalAxis);
        Vector3 tangentialVelocity = magnetizedVelocity - verticalSpeed * verticalAxis;
        Vector3 verticalVelocity;

        if (isGrounded)
        {
            // ease our own tracked tangential velocity towards the input-driven target instead of
            // snapping to it instantly - this also naturally slows to a stop when
            // desiredMovementVelocity is zero. A real surface would exert a normal force that
            // exactly cancels gravity's pull into it, so the vertical component is dropped entirely
            // rather than left to accumulate unbounded every frame with nothing to counter it.
            // Speeding up and slowing down use separate rates so releasing input (or a corner/
            // landing lowering the target) doesn't have to feel as snappy as accelerating does.
            float easeRate = desiredMovementVelocity.magnitude > tangentialVelocity.magnitude ? magnetizedAcceleration : magnetizedDeceleration;
            tangentialVelocity = Vector3.MoveTowards(tangentialVelocity, desiredMovementVelocity, easeRate * Time.fixedDeltaTime);
            verticalVelocity = Vector3.zero;
        }
        else
        {
            // airborne (e.g. still approaching a magnetized surface after leaving ZeroG, or having
            // just jumped off one): pass real momentum through untouched instead of easing towards
            // input, only clamping away any drift away from the surface (matching a real object in
            // flight not being slowed by moving away from a surface it hasn't reached yet)
            verticalVelocity = Mathf.Min(verticalSpeed, 0f) * verticalAxis;
        }

        Vector3 velocity = tangentialVelocity + verticalVelocity;

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

        // persist our own authoritative velocity for next frame - never read back from
        // _rigidbody.linearVelocity (see field comment)
        magnetizedVelocity = velocity;

        // apply final state to rigidbody, included the new adjusted velocity
        _rigidbody.position = position;
        _rigidbody.linearVelocity = gravitySourceVelocity + velocity;
        _preCollisionVelocity = _rigidbody.linearVelocity;

        // footsteps are a walking-on-a-surface cue - drive them from grounded travel only, not
        // raw speed, otherwise falling/flying past a surface at high speed plays footstep thuds
        // the whole way down even though the player isn't touching anything
        if (isGrounded)
        {
            // track distance for head bobbing. because it is a function of sine, we can just repeat the period over and over again
            distanceTraveled += totalDistance * headBobSpeed;
            while (distanceTraveled > 2.0f * Math.PI)
            {
                distanceTraveled -= 2.0f * (float)Math.PI;
            }

            DoMagnetizedHeadBob(totalDistance);
        }
    }

    void HandleMouseLook()
    {
        if (playerCamera == null || cameraArm == null)
        {
            return;
        }

        if (!canLook)
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

        cameraArm.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        ApplyCameraArmLocalPos();
        transform.Rotate(transform.up, lookX, Space.World);

        // Cache look values for external use
        LastLookX = lookX;
        LastLookY = lookY;
    }

    private void HandleGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;
        isGroundedOnEdge = false;

        if (gravityController == null)
        {
            return;
        }

        // used below to tell a genuine new landing apart from a re-collision blip against a surface
        // we're already attached to
        GravitySourceComponent? activeMagnetizedSource = MovementMode == ControllerMovementMode.Magnetized
            ? gravityController.GetActiveGravitySource()
            : null;

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
            magnetizedJumpInProgress = false;
            surfaceNormal = centerHit.normal;

            // only a genuine new landing - not still attached to the same surface we already were -
            // should reset landing-tracking state or reapply landing momentum. Without this, the
            // body reorienting toward the new "up" can briefly clip the geometry it's already stuck
            // to, and that re-collision was being treated as a brand new landing.
            bool isGenuineLanding = !wasGrounded && activeMagnetizedSource != attachedMagnetizedSource;
            attachedMagnetizedSource = activeMagnetizedSource;

            if (isGenuineLanding)
            {
                // don't compare against whatever axis was tracked while airborne (an approximation
                // based on gravity direction, not the actual surface) - that mismatch alone can look
                // like a sharp corner on a steeply angled surface and wrongly trigger a detach right
                // at the moment of landing
                Debug.Log($"TRAE landing (CENTER raycast) resetting lastMagnetizedVerticalAxis at t={Time.time:F3}, surfaceNormal={surfaceNormal}");
                lastMagnetizedVerticalAxis = Vector3.zero;
                pendingGravityAlignment = true;
                ApplyLandingMomentum();
            }

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
            magnetizedJumpInProgress = false;

            // see comment at the other landing branch above
            bool isGenuineLanding = !wasGrounded && activeMagnetizedSource != attachedMagnetizedSource;
            attachedMagnetizedSource = activeMagnetizedSource;

            if (isGenuineLanding)
            {
                Debug.Log($"TRAE landing (CORNER raycasts) resetting lastMagnetizedVerticalAxis at t={Time.time:F3}, surfaceNormal={surfaceNormal}");
                lastMagnetizedVerticalAxis = Vector3.zero;
                pendingGravityAlignment = true;
            }

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

            if (isGenuineLanding)
            {
                ApplyLandingMomentum();
            }
        }

        // Magnetized attachment isn't "resting on top of" a surface the way Gravity mode's ground
        // check assumes - the raycasts above can momentarily miss (seams, curvature, substep
        // integration noise) without the player having actually left the surface. The only
        // deliberate way to leave a magnetized surface is jumping, so treat every other case as
        // still grounded, keeping whatever surfaceNormal was last detected.
        // Requires wasGrounded: this only protects an EXISTING attachment from raycast noise. It
        // must not fabricate a first attachment out of nothing - MovementMode flips to Magnetized
        // as soon as the gravity field is entered, often well before the player physically reaches
        // the surface, and forcing isGrounded true during that approach fed a bogus/stale
        // surfaceNormal into the rest of the grounded logic (corner-detection included), which was
        // triggering spurious detaches before real contact and causing genuine PhysX bounces instead.
        if (!isGrounded && wasGrounded && MovementMode == ControllerMovementMode.Magnetized && !magnetizedJumpInProgress)
        {
            isGrounded = true;
        }
    }

    private void ApplyLandingMomentum()
    {
        // magnetizedVelocity is kept accurate through the airborne approach (see the Magnetized
        // FixedUpdate block), so it already holds the real incoming speed here - boost only the
        // tangential (along-surface) part so the landing reads well, leaving the into-surface part
        // alone (it gets dropped entirely next frame now that we're grounded, see FixedUpdate)
        if (MovementMode != ControllerMovementMode.Magnetized)
        {
            return;
        }

        Vector3 verticalAxis = surfaceNormal.sqrMagnitude > 0.01f ? surfaceNormal.normalized : magnetizedVelocity.normalized;
        Vector3 tangential = Vector3.ProjectOnPlane(magnetizedVelocity, verticalAxis);
        Vector3 vertical = magnetizedVelocity - tangential;

        // the literal incoming direction can feel arbitrary/wrong on landing (e.g. you were drifting
        // sideways while looking straight ahead) - bias it towards where the camera is actually
        // looking (projected onto the surface) so the boost reads as "launched where you're aiming"
        // rather than a strict physics carry-over. This is resolved before the speed curve below
        // and applied regardless of how much that curve ends up scaling the result, so even a
        // near-standstill landing still nudges towards camera-forward instead of being left in
        // whatever raw incoming direction it had (or skipped entirely once its magnitude is tiny).
        Vector3 tangentialDirection = tangential.sqrMagnitude > 0.0001f ? tangential.normalized : Vector3.zero;
        Vector3 boostedDirection = tangentialDirection;
        if (cameraArm != null)
        {
            Vector3 cameraForwardOnSurface = Vector3.ProjectOnPlane(cameraArm.transform.forward, verticalAxis);
            if (cameraForwardOnSurface.sqrMagnitude > 0.0001f)
            {
                Vector3 cameraDirection = cameraForwardOnSurface.normalized;
                boostedDirection = tangentialDirection.sqrMagnitude > 0.0001f
                    ? Vector3.Slerp(tangentialDirection, cameraDirection, magnetizedLandingMomentumCameraAlignment).normalized
                    : cameraDirection;
            }
        }

        // scale the boost linearly with landing speed - 0 at a standstill, 1 at/above
        // boostedMaxFlightSpeed (the actual top speed a landing can arrive at) - then remapped
        // from that 0-1 range onto a separate, much smaller ceiling (magnetizedLandingBoostMaxSpeed)
        // rather than the raw flight speed itself.
        float speedRatio = Mathf.Clamp01(tangential.magnitude / boostedMaxFlightSpeed);
        Vector3 boostedTangential = boostedDirection * (speedRatio * magnetizedLandingBoostMaxSpeed);

        magnetizedVelocity = boostedTangential + vertical;
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

        float jumpNorm = Math.Clamp(pressDuration / jumpChargeTime, 0.0f, 1.0f);
        float jumpNormPow = (float)Math.Pow(jumpNorm, 3.0);

        float jumpForce = 0.0f;
        float thrustForce = 0.0f;

        if (pressDuration > 0.0f)
        {
            jumpForce = minJumpThrust + ((maxJumpThrust - minJumpThrust) * jumpNormPow);
            thrustForce = minJumpForwardThrust + ((maxJumpForwardThrust - minJumpForwardThrust) * jumpNormPow);
        }

        if (thrustForce > 0.0f && _rigidbody != null && gravityController != null && cameraArm != null)
        {
            Vector3 gravity = gravityController.GetGravityVector();
            GravitySourceComponent? activeSource = gravityController.GetActiveGravitySource();
            Vector3 releasePosition = transform.position;

            // Direction is captured entirely at release: raycast along the camera's forward; if
            // it hits any surface, aim at that point, otherwise use camera-forward directly.
            Vector3 launchDirection;
            Ray jumpRay = new Ray(cameraArm.transform.position, cameraArm.transform.forward);
            if (Physics.Raycast(jumpRay, out RaycastHit jumpHit, jumpTargetRaycastDistance, LayerMask.GetMask("Default")))
            {
                Vector3 toTarget = jumpHit.point - releasePosition;
                launchDirection = toTarget.AlmostZero() ? cameraArm.transform.forward : toTarget.normalized;
            }
            else
            {
                launchDirection = cameraArm.transform.forward;
            }

            Vector3 upDirection = -1.0f * gravity.normalized;

            if (launchDirection.AlmostZero() || upDirection.AlmostZero())
            {
                return;
            }

            gravityController.SetNextTransitionTorqueAxis(launchDirection);

            if (activeSource != null)
            {
                ignoredGravitySource = activeSource;
                ignoredGravityDirection = gravity.normalized;
                ignoredSourceSetTime = Time.time;
                activeSource.isGravityEnabled = false;
            }

            if (jumpThrustCoroutine != null)
            {
                StopCoroutine(jumpThrustCoroutine);
                StopJumpThrustSound();
                jumpThrustOxygenContribution = 0.0f;
            }

            // mark the jump so HandleGrounded() knows this is the one deliberate way to leave a
            // magnetized surface, rather than a ground-detection glitch. Deliberately a separate
            // flag from isJumping: SetMovementMode() unconditionally resets isJumping on every
            // transition (it only means "arm the Gravity-mode landing sound"), and a magnetized
            // jump typically bounces through ZeroG while its gravity source is briefly disabled -
            // isJumping would get wiped out mid-flight, well before the player actually lands.
            magnetizedJumpInProgress = true;
            // we're deliberately leaving - a later landing (even back on this same surface) should
            // be treated as genuinely new, not a re-collision against a surface we never left
            attachedMagnetizedSource = null;

            jumpThrustCoroutine = StartCoroutine(JumpThrust(upDirection, jumpForce, launchDirection, thrustForce, magnetizedJumpOxygenCost * jumpNorm));
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

    void StartRotationThrustSound()
    {
        if (isRotationThrustPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent("play_thrust_loop", gameObject);
        isRotationThrustPlaying = true;
    }

    void StopRotationThrustSound()
    {
        if (!isRotationThrustPlaying)
        {
            return;
        }

        AkUnitySoundEngine.ExecuteActionOnEvent("play_thrust_loop", AkActionOnEventType.AkActionOnEventType_Stop, gameObject);
        AkUnitySoundEngine.PostEvent("play_thrust_stop", gameObject);
        isRotationThrustPlaying = false;
    }

    void StartJumpThrustSound()
    {
        if (isJumpThrustSoundPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent("play_thrust_forward_loop", gameObject);
        isJumpThrustSoundPlaying = true;
    }

    void StopJumpThrustSound()
    {
        if (!isJumpThrustSoundPlaying)
        {
            return;
        }

        AkUnitySoundEngine.ExecuteActionOnEvent("play_thrust_forward_loop", AkActionOnEventType.AkActionOnEventType_Stop, gameObject);
        AkUnitySoundEngine.PostEvent("stop_thrust_forward_loop", gameObject);
        isJumpThrustSoundPlaying = false;
    }

    void StartThrustForwardSound()
    {
        if (isThrustForwardPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent("play_thrust_forward_loop", gameObject);
        isThrustForwardPlaying = true;
    }

    void StopThrustForwardSound()
    {
        if (!isThrustForwardPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent("stop_thrust_forward_loop", gameObject);
        isThrustForwardPlaying = false;
    }

    void StartMagnetizedSound()
    {
        if (isMagnetizedSoundPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent("play_magnetized", gameObject);
        isMagnetizedSoundPlaying = true;
    }

    void StopMagnetizedSound()
    {
        if (!isMagnetizedSoundPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent("stop_magnetized", gameObject);
        isMagnetizedSoundPlaying = false;
    }

    // owns the magnetized-sound distance check generically across every magnetizable source in
    // the scene (mesh, plane, point, ...), each of which just exposes a raw geometric distance
    // via GetDistanceToSurface. Finds the closest one, applies magnetizeRadius, and drives the
    // play/stop loop plus the DistanceToSurface RTPC (0 = at the surface, 100 = at magnetizedSoundRange)
    void HandleMagnetizedSound()
    {
        // this is a pre-magnetization approach cue only - once the player is actually caught by
        // a surface (MovementMode.Magnetized) or standing on one, it must stay silent
        if (isGrounded || MovementMode == ControllerMovementMode.Magnetized)
        {
            StopMagnetizedSound();
            return;
        }

        Vector3 position = transform.position;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < magnetizableSources.Length; i++)
        {
            GravitySourceComponent? source = magnetizableSources[i];
            if (source == null || !source.isGravityEnabled)
            {
                continue;
            }

            float distance = source.GetDistanceToSurface(position) - magnetizeRadius;
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        if (closestDistance <= magnetizedSoundRange)
        {
            StartMagnetizedSound();

            float distanceRatio = Mathf.Clamp01(closestDistance / magnetizedSoundRange);
            AkUnitySoundEngine.SetRTPCValue("DistanceToSurface", distanceRatio * 100f, gameObject);
        }
        else
        {
            StopMagnetizedSound();
        }
    }

    void HandleZeroGLook()
    {
        if (_rigidbody == null || playerCamera == null || cameraArm == null)
        {
            return;
        }

        if (!canLook)
        {
            _pendingRollTorque = Vector3.zero;
            _pendingAutoRollAcceleration = Vector3.zero;
            rotationOxygenContribution = 0.0f;
            StopRotationThrustSound();
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

            // Apply pitch and yaw rotation to the rigidbody using the arm's forward as reference -
            // cameraArm is the instant, authoritative pivot; playerCamera is a visually-smoothed
            // follower and must never be read for control/gameplay purposes (see its declaration)
            _rigidbody.transform.Rotate(cameraArm.transform.up, lookX, Space.World);
            _rigidbody.transform.Rotate(cameraArm.transform.right, -lookY, Space.World);

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
            _pendingAutoRollAcceleration = Vector3.zero;
            rotationOxygenContribution = rotationOxygenBurnRate;
            StartRotationThrustSound();
        }
        else
        {
            _pendingRollTorque = Vector3.zero;

            bool stabilizeActive = canStabilize && (stabilizeAction?.IsPressed() ?? false);
            bool isStabilizingAngularVelocity = stabilizeActive && _rigidbody.angularVelocity.magnitude >= stabilizeSoundAngularThreshold;
            if (isStabilizingAngularVelocity)
            {
                rotationOxygenContribution = rotationOxygenBurnRate;
                StartRotationThrustSound();
            }
            else
            {
                rotationOxygenContribution = 0.0f;
                StopRotationThrustSound();
            }

            // orient player to align with gravity, but only if we're looking at a surface we can magnetize to
            bool isLookingAtMagnetizableSurface = false;
            Ray forwardRay = new Ray(cameraArm.transform.position, cameraArm.transform.forward);
            if (Physics.Raycast(forwardRay, out RaycastHit lookHit, jumpTargetRaycastDistance, LayerMask.GetMask("Default")))
            {
                GravitySourceComponent? lookedAtGravitySource = lookHit.collider.GetComponentInParent<GravitySourceComponent>();
                isLookingAtMagnetizableSurface = lookedAtGravitySource != null && lookedAtGravitySource.isMagnetized;
            }

            if (_rigidbody.linearVelocity.magnitude >= autoRollMinSpeed
                && Physics.Raycast(forwardRay, out RaycastHit surfaceHit, autoRollRaycastDistance)
                // 0 = looking straight along the surface (grazing), 90 = looking straight into it (head-on)
                && 90f - Vector3.Angle(transform.forward, -surfaceHit.normal) < autoRollAngleThreshold
                && Vector3.Angle(transform.forward, forwardRay.direction) < autoRollLookAngleThreshold
                && isLookingAtMagnetizableSurface)
            {
                Vector3 normalOnPlane = Vector3.ProjectOnPlane(surfaceHit.normal, transform.forward);
                if (normalOnPlane.sqrMagnitude > 0.001f)
                {
                    float t = surfaceHit.distance / autoRollRaycastDistance;
                    float easing = Mathf.Log(1f + (1f - t) * (Mathf.Exp(1f) - 1f));
                    float angle = Vector3.SignedAngle(transform.up, normalOnPlane.normalized, transform.forward);
                    float rollAngularSpeedDeg = Vector3.Dot(_rigidbody.angularVelocity, transform.forward) * Mathf.Rad2Deg;

                    // Critically damped (zeta = 1): closes `angle` to 0 in ~autoRollSettleTime with no overshoot,
                    // regardless of the player's mass/inertia (applied via ForceMode.Acceleration in FixedUpdate).
                    float omega = 2f / Mathf.Max(autoRollSettleTime, 0.01f);
                    float accelerationDeg = (angle * omega * omega - rollAngularSpeedDeg * 2f * omega) * easing;
                    _pendingAutoRollAcceleration = transform.forward * accelerationDeg * Mathf.Deg2Rad;
                }
                else
                {
                    _pendingAutoRollAcceleration = Vector3.zero;
                }
            }
            else
            {
                _pendingAutoRollAcceleration = Vector3.zero;
            }
        }
    }

    void HandleZeroGMovement()
    {
        if (_rigidbody == null || playerCamera == null || cameraArm == null)
        {
            return;
        }

        bool? isStabilizePressed = stabilizeAction?.IsPressed();

        if (isStabilizePressed == null)
        {
            return;
        }

        bool stabilizeActive = canStabilize && isStabilizePressed.Value;

        float forwardThrustInput = canThrust ? (forwardThrustAction?.ReadValue<float>() ?? 0f) : 0f;
        float backwardThrustInput = canThrust ? (backwardThrustAction?.ReadValue<float>() ?? 0f) : 0f;
        float leftThrustInput = canThrust ? (leftThrustAction?.ReadValue<float>() ?? 0f) : 0f;
        float rightThrustInput = canThrust ? (rightThrustAction?.ReadValue<float>() ?? 0f) : 0f;
        float upThrustInput = canThrust ? (upThrustAction?.ReadValue<float>() ?? 0f) : 0f;
        float downThrustInput = canThrust ? (downThrustAction?.ReadValue<float>() ?? 0f) : 0f;

        Vector3 velocity = _rigidbody.linearVelocity;

        // velocity as of last FixedUpdate already reflects last step's applied forces (AddForce isn't
        // integrated until the physics engine steps after FixedUpdate returns, so we compare against
        // the previous step's result rather than trying to read this step's change back immediately)
        float velocityChangeLastStep = (velocity - _previousZeroGVelocity).magnitude;
        _previousZeroGVelocity = velocity;

        bool isManualThrusting = forwardThrustInput > 0f || backwardThrustInput > 0f
            || leftThrustInput > 0f || rightThrustInput > 0f
            || upThrustInput > 0f || downThrustInput > 0f;
        bool isStabilizingLinearVelocity = stabilizeActive && velocity.magnitude >= stabilizeSoundVelocityThreshold;
        if (isManualThrusting || isStabilizingLinearVelocity)
        {
            StartThrustForwardSound();
        }
        else
        {
            StopThrustForwardSound();
        }

        Vector3 stabilizationAccelForce = Vector3.zero;
        if (stabilizeActive)
        {
            stabilizationAccelForce = -velocity * (1.0f - stabilizeMultiplier);

            Vector3 angularVelocity = _rigidbody.angularVelocity;
            Vector3 stabilizationTorque = -angularVelocity * (1.0f - stabilizeMultiplier);
            _rigidbody.AddTorque(stabilizationTorque, ForceMode.Acceleration);
        }

        Vector3 thrustVector = Vector3.zero;
        thrustVector += cameraArm.transform.forward * forwardThrustInput;
        thrustVector += -cameraArm.transform.forward * backwardThrustInput;
        thrustVector += -cameraArm.transform.right * leftThrustInput;
        thrustVector += cameraArm.transform.right * rightThrustInput;
        thrustVector += cameraArm.transform.up * upThrustInput;
        thrustVector += -cameraArm.transform.up * downThrustInput;

        // burn oxygen in proportion to the velocity change the thrust actually produced last step,
        // so it drops to zero once the speed clamp fully absorbs further thrust in the same direction,
        // and rises naturally when redirecting a fast-moving body (a bigger delta-v to turn a bigger vector)
        float movementOxygenBurnRate;
        if (stabilizeActive && velocity.sqrMagnitude > 1.0f)
        {
            movementOxygenBurnRate = 1.0f;
        }
        else if (thrustVector.sqrMagnitude > 0.0f)
        {
            // floor at minOxygenBurnRate while actively holding thrust, even once the speed clamp
            // has absorbed all further acceleration - a real thruster still burns fuel while firing,
            // it shouldn't go free just because you've reached max speed
            float maxPossibleVelocityChange = (flightForce / _rigidbody.mass) * Time.fixedDeltaTime;
            movementOxygenBurnRate = maxPossibleVelocityChange > 0f
                ? Mathf.Max(minOxygenBurnRate, Mathf.Clamp01(velocityChangeLastStep / maxPossibleVelocityChange))
                : minOxygenBurnRate;
        }
        else
        {
            movementOxygenBurnRate = 0.0f;
        }

        oxygenBurnRate = Math.Max(Math.Max(movementOxygenBurnRate, rotationOxygenContribution), jumpThrustOxygenContribution);

        _rigidbody.AddForce(thrustVector.normalized * flightForce);

        Vector3 idleDampingAccelForce = Vector3.zero;
        if (!stabilizeActive && thrustVector.sqrMagnitude == 0f)
        {
            idleDampingAccelForce = -_rigidbody.linearVelocity * zeroGIdleDamping;
        }

        _rigidbody.AddForce(stabilizationAccelForce + idleDampingAccelForce, ForceMode.Acceleration);

        velocity = _rigidbody.linearVelocity;
        if (isManualThrusting)
        {
            // allow briefly exceeding maxFlightSpeed while actively thrusting, up to an overdrive cap
            if (velocity.sqrMagnitude > boostedMaxFlightSpeed * boostedMaxFlightSpeed)
            {
                _rigidbody.linearVelocity = velocity.normalized * boostedMaxFlightSpeed;
            }
        }
        else if (velocity.magnitude > maxFlightSpeed)
        {
            // ease back down to maxFlightSpeed instead of clamping instantly
            float easedSpeed = Mathf.MoveTowards(velocity.magnitude, maxFlightSpeed, speedEaseBackRate * Time.deltaTime);
            _rigidbody.linearVelocity = velocity.normalized * easedSpeed;
        }

        if (stabilizeActive && _rigidbody.linearVelocity.magnitude < stabilizeSoundVelocityThreshold && _rigidbody.angularVelocity.magnitude < stabilizeSoundAngularThreshold)
        {
            if (!hasPlayedStabilizedSound)
            {
                AkUnitySoundEngine.PostEvent("play_stabilized", gameObject);
                hasPlayedStabilizedSound = true;
            }
        }
        else
        {
            hasPlayedStabilizedSound = false;
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

            // the surface we are actively standing on is expected to overlap us and must be
            // ignored so walking across it isn't treated as hitting a wall - but that exemption
            // must not apply while still airborne/approaching it, or a fast fall/bounce has zero
            // collision protection against the very surface it's heading for (HandleGrounded's
            // raycasts are periodic and discrete, and can simply be outrun at speed, tunnelling
            // straight through). Only skip it once we're actually attached.
            // gravity sources are nested under their own surface's collider (not a shared scene-graph root),
            // so walk up from the gravity source instead of comparing transform.root
            if (isGrounded && gravitySource.transform.IsChildOf(hit.collider.transform))
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

    private void ClearIgnoredGravitySource()
    {
        if (ignoredGravitySource != null)
        {
            ignoredGravitySource.isGravityEnabled = true;
            ignoredGravitySource = null;
        }
        approachTimer = 0f;
    }

    // Lets go of the current magnetized surface entirely, carrying the player onward on their
    // existing trajectory (same "ignored gravity source" grace period the magnetized jump uses) -
    // used when a sharp corner is taken too fast to plausibly walk around
    private void DetachFromMagnetizedSurface(GravitySourceComponent gravitySource, Vector3 gravityVector, Vector3 worldVelocity)
    {
        ignoredGravitySource = gravitySource;
        ignoredGravityDirection = gravityVector.normalized;
        ignoredSourceSetTime = Time.time;
        gravitySource.isGravityEnabled = false;
        // we're deliberately leaving - a later landing (even back on this same surface) should be
        // treated as genuinely new, not a re-collision against a surface we never left
        attachedMagnetizedSource = null;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = worldVelocity;
        }
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

    private IEnumerator JumpThrust(Vector3 upDirection, float jumpForce, Vector3 thrustDirection, float thrustForce, float magnetizedJumpOxygenCostAmount)
    {
        if (_rigidbody != null && playerCamera != null)
        {
            StartJumpThrustSound();
            jumpThrustOxygenContribution = 1.0f;

            // wait a frame before charging the flat jump cost so the HUD's laggy oxygen bar
            // freezes at the pre-cost baseline first; otherwise the rising-edge snap reads the
            // already-depleted value and the flat cost never shows as a visible drop
            yield return null;
            oxygenSystem?.DepleteOxygen(magnetizedJumpOxygenCostAmount);

            float elapsed = 0f;
            while (elapsed < jumpThrustPhaseOneDuration)
            {
                float ratio = (float)Math.Clamp(elapsed / jumpThrustPhaseOneDuration, 0.0, 1.0);

                _rigidbody.AddForce(upDirection * jumpForce * (1.0f - ratio), ForceMode.Force);
                _rigidbody.AddForce(-upDirection * jumpForce * ratio, ForceMode.Force);

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            StopJumpThrustSound();

            _rigidbody.AddForce(cameraArm.transform.forward * thrustForce, ForceMode.Impulse);
            AkUnitySoundEngine.PostEvent("play_thrust_impulse", gameObject);

            float impulseElapsed = 0f;
            while (impulseElapsed < jumpThrustImpulseDuration)
            {
                yield return new WaitForFixedUpdate();
                impulseElapsed += Time.fixedDeltaTime;
            }

            jumpThrustOxygenContribution = 0.0f;
        }

        jumpThrustCoroutine = null;
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
        // deliberately unparented (see where it's created in Start()) so it doesn't get destroyed
        // automatically along with the player hierarchy
        if (cameraFollowTransform != null)
        {
            Destroy(cameraFollowTransform.gameObject);
        }

        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
            jumpAction.canceled -= OnJumpCanceled;
        }
        if (jumpThrustCoroutine != null)
        {
            StopCoroutine(jumpThrustCoroutine);
            jumpThrustCoroutine = null;
            StopJumpThrustSound();
            jumpThrustOxygenContribution = 0.0f;
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