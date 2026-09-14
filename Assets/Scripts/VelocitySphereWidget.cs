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

    [Header("Camera-Facing Roll")]
    // degrees around the direction axis from the wide face's default reference to its actual normal,
    // to compensate for however the arrow mesh was authored/rotated
    public float wideFaceAxisAngle = 0f;
    public float wideFaceRotationSpeed = 180f;

    public bool logVelocity = true;

    private float[] arrowOffsets = System.Array.Empty<float>();
    private bool arrowOffsetsInitialized = false;

    private float[] arrowRollAngles = System.Array.Empty<float>();
    private Quaternion[] arrowBaseRotations = System.Array.Empty<Quaternion>();
    private Vector3[] arrowLocalRollAxes = System.Array.Empty<Vector3>();
    private bool arrowRollStateInitialized = false;

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

        if (speed < minSpeedToShowArrows)
        {
            DisableAllArrows();
            return;
        }

        AnimateArrows(speed);
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

    private void AlignArrowsToCamera(Transform camTransform)
    {
        EnsureArrowRollStateInitialized();

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null || !arrow.activeSelf)
            {
                continue;
            }

            Transform arrowTransform = arrow.transform;
            Quaternion baseRotation = arrowBaseRotations[i];
            Vector3 localRollAxis = arrowLocalRollAxes[i];

            // rebuild the roll fresh every frame as a rotation around the direction axis - expressed in
            // this arrow's own unrotated mesh space (localRollAxis), applied on TOP of its authored base
            // rotation - rather than accumulating world-space Rotate() calls directly on localRotation,
            // which would both drop the baked-in correction and drift off-axis: arrowLine's rotation is
            // reassigned wholesale every frame (it tracks camera-relative velocity direction, which
            // shifts with mouse look alone), so a world-space roll baked into localRotation last frame
            // would get dragged along by that reassignment
            arrowTransform.localRotation = baseRotation * Quaternion.AngleAxis(arrowRollAngles[i], localRollAxis);

            Vector3 axis = arrowTransform.forward;

            Vector3 desiredDir = Vector3.ProjectOnPlane(camTransform.position - arrowTransform.position, axis);
            if (desiredDir.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            desiredDir.Normalize();

            Vector3 wideFaceLocalDir = Quaternion.AngleAxis(wideFaceAxisAngle, localRollAxis) * PerpendicularReference(localRollAxis);
            Vector3 currentWideFaceDir = (arrowTransform.rotation * wideFaceLocalDir).normalized;

            // the wide face is flat, so either side of it counts as "facing the camera" - flip to
            // whichever side is already closer so we never turn more than 90 degrees
            float facingSign = Vector3.Dot(currentWideFaceDir, desiredDir) >= 0f ? 1f : -1f;
            currentWideFaceDir *= facingSign;

            // dot product gives the (unsigned) size of the misalignment - 0 when already facing the
            // camera, growing up to 1 when perpendicular - so bigger angles turn faster
            float angleError = 1f - Vector3.Dot(currentWideFaceDir, desiredDir);
            float turnSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(currentWideFaceDir, desiredDir), axis));

            arrowRollAngles[i] += turnSign * angleError * wideFaceRotationSpeed * Time.deltaTime;
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
        if (arrowRollStateInitialized && arrowRollAngles.Length == arrowPool.Length)
        {
            return;
        }

        arrowRollAngles = new float[arrowPool.Length];
        arrowBaseRotations = new Quaternion[arrowPool.Length];
        arrowLocalRollAxes = new Vector3[arrowPool.Length];

        for (int i = 0; i < arrowPool.Length; i++)
        {
            GameObject? arrow = arrowPool[i];
            if (arrow == null)
            {
                continue;
            }

            // capture the mesh's authored orientation before we ever touch it, so any corrective
            // rotation baked onto the prefab is preserved rather than overwritten
            arrowBaseRotations[i] = arrow.transform.localRotation;

            // the object-space axis that this arrow's base rotation maps onto the direction-of-travel
            // axis - i.e. whichever local axis actually needs to roll, given how the mesh was authored
            arrowLocalRollAxes[i] = Quaternion.Inverse(arrowBaseRotations[i]) * Vector3.forward;
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
        }
    }
}
