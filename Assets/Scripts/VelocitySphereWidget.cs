#nullable enable

using UnityEngine;

public class VelocitySphereWidget : MonoBehaviour
{
    private const float MinSpeedForDirection = 0.01f;

    private Rigidbody? playerRigidbody;
    private FirstPersonController? playerController;

    public GameObject? rimSphereVisual;
    public Transform? arrowLine;
    public GameObject[] arrowPool = System.Array.Empty<GameObject>();

    public float arrowLineHalfLength = 1f;
    public float arrowMotionSpeedScale = 0.2f;
    public float minSpeedToShowArrows = 0.1f;

    [Header("Arrow Scale Fade")]
    // distance (in local line units) from either end of the line over which an arrow scales from 0 up
    // to its cached default scale, so wrapping around the ends fades rather than pops
    public float arrowEdgeFadeDistance = 0.2f;
    // how fast (in 0..1 units per second) the whole arrow set scales in/out as velocity crosses
    // minSpeedToShowArrows, so arrows grow in from 0 rather than snapping on/off
    public float arrowSpeedFadeSpeed = 2f;

    [Header("Camera-Facing Roll")]
    // degrees around the direction axis from the wide face's default reference to its actual normal,
    // to compensate for however the arrow mesh was authored/rotated
    public float wideFaceAxisAngle = 90f;

    public bool logVelocity = true;

    private float[] arrowOffsets = System.Array.Empty<float>();
    private bool arrowOffsetsInitialized = false;

    // per-arrow inverse of the mesh-space frame (travel axis, wide face normal), so the arrow can be
    // rotated straight onto the desired line-space frame regardless of how the mesh was authored
    private Quaternion[] arrowMeshFrameInverses = System.Array.Empty<Quaternion>();
    private bool arrowRollStateInitialized = false;

    // each arrow's authored localScale, cached once so we can scale from 0 up to it rather than
    // baking an assumption about what "full size" looks like
    private Vector3[] arrowDefaultScales = System.Array.Empty<Vector3>();
    private bool arrowScaleStateInitialized = false;

    // 0..1: fades in while speed is above minSpeedToShowArrows, fades out while below it, so the
    // whole arrow set grows in/shrinks out instead of popping on/off
    private float arrowVisibilityFade = 0f;

    private void Start()
    {
        SetWidgetVisible(false);
    }

    private void LateUpdate()
    {
        if (playerController == null)
        {
            playerController = StarliftStatics.FindFirstPersonController();
        }

        if (playerController == null)
        {
            return;
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = playerController.GetComponent<Rigidbody>();
        }

        Camera? cam = playerController.playerCamera;

        if (playerRigidbody == null || cam == null || arrowLine == null)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        float speed = velocity.magnitude;

        if (logVelocity)
        {
            Debug.Log($"[VelocitySphereWidget] mode={playerController.MovementMode} velocity={velocity} speed={speed:F2}");
        }

        if (playerController.MovementMode != FirstPersonController.ControllerMovementMode.ZeroG)
        {
            SetWidgetVisible(false);
            return;
        }

        SetWidgetVisible(true);

        Transform camTransform = cam.transform;

        if (speed > MinSpeedForDirection)
        {
            Vector3 camRelative = new Vector3(
                Vector3.Dot(velocity, camTransform.right),
                Vector3.Dot(velocity, camTransform.up),
                Vector3.Dot(velocity, camTransform.forward));
            arrowLine.localRotation = Quaternion.LookRotation(camRelative.normalized);
        }

        bool speedAboveThreshold = speed >= minSpeedToShowArrows;
        float fadeTarget = speedAboveThreshold ? 1f : 0f;
        arrowVisibilityFade = Mathf.MoveTowards(arrowVisibilityFade, fadeTarget, arrowSpeedFadeSpeed * Time.deltaTime);

        if (!speedAboveThreshold && arrowVisibilityFade <= 0f)
        {
            DisableAllArrows();
            return;
        }

        AnimateArrows(speed);
        ApplyArrowScales(arrowVisibilityFade);
        AlignArrowsToCamera(camTransform);
    }

    private void AnimateArrows(float speed)
    {
        EnsureArrowOffsetsInitialized();

        float span = 2f * arrowLineHalfLength;
        float step = speed * arrowMotionSpeedScale * Time.deltaTime;

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null)
            {
                continue;
            }

            float offset = arrowOffsets[i] + step;

            // wrap back to the start of the line once an arrow travels past the far end
            if (span > 0f)
            {
                offset = ((offset + arrowLineHalfLength) % span + span) % span - arrowLineHalfLength;
            }

            arrowOffsets[i] = offset;
            arrow.transform.localPosition = new Vector3(0f, 0f, offset);
            arrow.SetActive(true);
        }
    }

    private void ApplyArrowScales(float visibilityFade)
    {
        EnsureArrowScaleStateInitialized();

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null || !arrow.activeSelf)
            {
                continue;
            }

            float edgeFade = ComputeEdgeFade(arrowOffsets[i]);
            arrow.transform.localScale = arrowDefaultScales[i] * (edgeFade * visibilityFade);
        }
    }

    // 1 in the middle of the line, fading to 0 as an arrow nears either end, so it shrinks away
    // before it wraps around rather than popping to the opposite end at full size
    private float ComputeEdgeFade(float offset)
    {
        if (arrowEdgeFadeDistance <= 0f)
        {
            return 1f;
        }

        float distanceFromNearEdge = arrowLineHalfLength - Mathf.Abs(offset);
        return Mathf.Clamp01(distanceFromNearEdge / arrowEdgeFadeDistance);
    }

    private void EnsureArrowScaleStateInitialized()
    {
        if (arrowScaleStateInitialized && arrowDefaultScales.Length == arrowPool.Length)
        {
            return;
        }

        arrowDefaultScales = new Vector3[arrowPool.Length];

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null)
            {
                continue;
            }

            arrowDefaultScales[i] = arrow.transform.localScale;
        }

        arrowScaleStateInitialized = true;
    }

    private void AlignArrowsToCamera(Transform camTransform)
    {
        if (arrowLine == null)
        {
            return;
        }

        EnsureArrowRollStateInitialized();

        // one shared view vector from the rendering camera to the line's centre, in arrowLine's local
        // space (where the direction of travel is +Z), so every arrow gets the identical orientation
        Vector3 toCamLocal = arrowLine.InverseTransformDirection(camTransform.position - arrowLine.position);
        Vector3 faceNormalLocal = Vector3.ProjectOnPlane(toCamLocal, Vector3.forward);

        // velocity pointing straight at/away from the camera leaves no meaningful roll - keep the last one
        if (faceNormalLocal.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion lineFrame = Quaternion.LookRotation(Vector3.forward, faceNormalLocal.normalized);

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null || !arrow.activeSelf)
            {
                continue;
            }

            arrow.transform.localRotation = lineFrame * arrowMeshFrameInverses[i];
        }
    }

    // an arbitrary reference direction perpendicular to axis, used as the wideFaceAxisAngle=0 baseline
    private static Vector3 PerpendicularReference(Vector3 axis)
    {
        Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
        return Vector3.ProjectOnPlane(reference, axis).normalized;
    }

    private void EnsureArrowRollStateInitialized()
    {
        if (arrowRollStateInitialized && arrowMeshFrameInverses.Length == arrowPool.Length)
        {
            return;
        }

        arrowMeshFrameInverses = new Quaternion[arrowPool.Length];

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null)
            {
                arrowMeshFrameInverses[i] = Quaternion.identity;
                continue;
            }

            // the authored rotation tells us which mesh-space axis is the direction of travel (the one
            // it maps onto the line's +Z); the wide face normal is a perpendicular reference spun by
            // wideFaceAxisAngle around that axis
            Quaternion authoredRotation = arrow.transform.localRotation;
            Vector3 meshTravelAxis = Quaternion.Inverse(authoredRotation) * Vector3.forward;
            Vector3 meshFaceNormal = Quaternion.AngleAxis(wideFaceAxisAngle, meshTravelAxis) * PerpendicularReference(meshTravelAxis);

            arrowMeshFrameInverses[i] = Quaternion.Inverse(Quaternion.LookRotation(meshTravelAxis, meshFaceNormal));
        }

        arrowRollStateInitialized = true;
    }

    private void EnsureArrowOffsetsInitialized()
    {
        if (arrowOffsetsInitialized && arrowOffsets.Length == arrowPool.Length)
        {
            return;
        }

        arrowOffsets = new float[arrowPool.Length];
        float spacing = arrowPool.Length > 0 ? (2f * arrowLineHalfLength) / arrowPool.Length : 0f;

        for (int i = 0; i < arrowPool.Length; i++)
        {
            float t = i - (arrowPool.Length - 1) / 2f;
            arrowOffsets[i] = t * spacing;
        }

        arrowOffsetsInitialized = true;
    }

    private void DisableAllArrows()
    {
        for (int i = 0; i < arrowPool.Length; i++)
        {
            if (arrowPool[i] != null)
            {
                arrowPool[i].SetActive(false);
            }
        }
    }

    private void SetWidgetVisible(bool visible)
    {
        if (rimSphereVisual != null)
        {
            rimSphereVisual.SetActive(visible);
        }

        if (!visible)
        {
            DisableAllArrows();
            arrowVisibilityFade = 0f;
        }
    }
}
