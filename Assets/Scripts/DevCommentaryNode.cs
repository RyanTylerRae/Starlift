#nullable enable

using UnityEngine;

public class DevCommentaryNode : Interactable
{
    public override ETooltipActionType ActionType
    {
        get { return ETooltipActionType.Use; }
    }

    public AK.Wwise.Event? playEvent;

    [Header("Bubble")]
    public Transform? bubble;
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 1.0f;
    public float spinSpeed = 90.0f;
    public float returnSpeed = 8.0f;

    private Camera? playerCamera;
    private Vector3 bubbleRestLocalPosition = Vector3.zero;
    private float playTime = 0f;

    public void Start()
    {
        if (bubble != null)
        {
            bubbleRestLocalPosition = bubble.localPosition;
        }
    }

    public void LateUpdate()
    {
        if (bubble == null)
        {
            return;
        }

        // bob and spin around the node's own up so nodes work on any gravity surface
        Vector3 up = transform.up;
        bool isPlaying = DevCommentaryManager.Instance != null && DevCommentaryManager.Instance.IsPlaying(this);

        if (isPlaying)
        {
            playTime += Time.deltaTime;

            float bob = Mathf.Sin(playTime * 2.0f * Mathf.PI * bobFrequency) * bobAmplitude;
            bubble.localPosition = bubbleRestLocalPosition + Vector3.up * bob;
            bubble.Rotate(up, spinSpeed * Time.deltaTime, Space.World);
            return;
        }

        playTime = 0f;

        float t = 1.0f - Mathf.Exp(-returnSpeed * Time.deltaTime);
        bubble.localPosition = Vector3.Lerp(bubble.localPosition, bubbleRestLocalPosition, t);

        if (playerCamera == null)
        {
            playerCamera = StarliftStatics.FindFirstPersonController()?.playerCamera;
        }

        if (playerCamera == null)
        {
            return;
        }

        // yaw-only billboard: face the camera projected onto the plane perpendicular to up
        Vector3 toCamera = Vector3.ProjectOnPlane(playerCamera.transform.position - bubble.position, up);
        if (toCamera.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // sprites face -Z, so point forward away from the camera to show the front
        Quaternion target = Quaternion.LookRotation(-toCamera, up);
        // close half the remaining angle each frame
        bubble.rotation = Quaternion.Slerp(bubble.rotation, target, 0.5f);
    }

    protected override bool HandleCanInteract()
    {
        return DevCommentaryManager.Instance != null && playEvent != null && playEvent.IsValid();
    }

    protected override void HandleInteract()
    {
        DevCommentaryManager.Instance?.Toggle(this);
    }
}
