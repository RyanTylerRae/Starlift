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

    public bool logVelocity = true;

    private float[] arrowOffsets = System.Array.Empty<float>();
    private bool arrowOffsetsInitialized = false;

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
