#nullable enable

using System.Threading.Tasks;
using UnityEngine;

public class SM_CryopodSequence : StateMachineComponent
{
    public DirectedSequence? introSequence;
    public DirectedSequence? wakeSequence;
    public TextFader? zoomPrompt;
    public float fadeInDuration = 4f;
    public bool restrictLook = true;
    public float lookConeYawHalfAngle = 45f;
    public float lookConePitchHalfAngle = 30f;

    private FirstPersonController? playerController = null;
    private ScreenFader? screenFader = null;
    private HUDZoomFade? hudFade = null;
    private Task? fadeTask = null;

    private void TryResolvePlayerReferences()
    {
        playerController = StarliftStatics.FindFirstPersonController();
        hudFade = FindFirstObjectByType<HUDZoomFade>();

        GameObject? player = StarliftStatics.FindPlayer();
        if (player != null && player.TryGetComponent<ScreenFader>(out var fader))
        {
            screenFader = fader;
        }
    }

    private void SetAllControlsEnabled(bool enabled)
    {
        if (playerController == null)
        {
            return;
        }

        playerController.SetLookEnabled(enabled);
        playerController.SetZoomEnabled(enabled);
        playerController.SetMoveEnabled(enabled);
        playerController.SetJumpEnabled(enabled);
        playerController.SetStabilizeEnabled(enabled);
        playerController.SetThrustEnabled(enabled);
    }

    private bool IsSequenceFinished(DirectedSequence? sequence)
    {
        return sequence == null || sequence.IsFinished;
    }

    protected override void BuildStateMachine()
    {
        stateMachine.RegisterState("Init", isStartingState: true);
        stateMachine.RegisterState("Intro");
        stateMachine.RegisterState("Wake");
        stateMachine.RegisterState("ZoomPrompt");
        stateMachine.RegisterState("Complete");

        stateMachine.AddEdge("Init", "Intro", () =>
        {
            if (playerController == null || screenFader == null)
            {
                TryResolvePlayerReferences();
            }

            return playerController != null && screenFader != null;
        });

        stateMachine.AddEdge("Intro", "Wake", () => IsSequenceFinished(introSequence));

        stateMachine.AddEdge("Wake", "ZoomPrompt", () =>
        {
            bool fadeFinished = fadeTask == null || fadeTask.IsCompleted;
            return fadeFinished && IsSequenceFinished(wakeSequence);
        });

        stateMachine.AddEdge("ZoomPrompt", "Complete", () => playerController != null && playerController.IsZoomHeld);

        stateMachine.GetState("Intro").OnEnterState += () =>
        {
            screenFader?.SetOpacity(1f);
            hudFade?.SetHudHidden(true);
            SetAllControlsEnabled(false);

            if (restrictLook)
            {
                playerController?.SetLookCone(lookConeYawHalfAngle, lookConePitchHalfAngle);
            }

            introSequence?.Play();
        };

        stateMachine.GetState("Wake").OnEnterState += () =>
        {
            playerController?.SetLookEnabled(true);
            fadeTask = screenFader?.FadeToOpacity(0f, fadeInDuration);
            wakeSequence?.Play();
        };

        stateMachine.GetState("ZoomPrompt").OnEnterState += () =>
        {
            zoomPrompt?.Show();
            playerController?.SetZoomEnabled(true);
        };

        stateMachine.GetState("Complete").OnEnterState += () =>
        {
            zoomPrompt?.Hide();
            playerController?.ClearLookCone();
            hudFade?.SetHudHidden(false);
            SetAllControlsEnabled(true);
        };
    }
}
