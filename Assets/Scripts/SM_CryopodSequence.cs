#nullable enable

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class SM_CryopodSequence : StateMachineComponent
{
    public DirectedSequence? introSequence;
    public DirectedSequence? wakeSequence;
    public TextFader? zoomPrompt;
    public float fadeInDuration = 4f;
    public bool restrictLook = true;
    public float lookConeYawHalfAngle = 45f;
    public float lookConePitchHalfAngle = 30f;

    // after the zoom prompt the player scans the drone, which plays droneSequence before everything unlocks
    public Scannable? droneScannable;
    public DirectedSequence? droneSequence;

    // fires once droneSequence has finished - wire the drone's activation here (e.g. DroneController.enabled),
    // so it doesn't start flying while the sequence is still animating it
    public UnityEvent onDroneSequenceFinished = new();

    private FirstPersonController? playerController = null;
    private ScreenFader? screenFader = null;
    // the main HUD and the world-element HUD each have their own fade
    private HUDZoomFade[] hudFades = System.Array.Empty<HUDZoomFade>();
    private Task? fadeTask = null;

    private void TryResolvePlayerReferences()
    {
        playerController = StarliftStatics.FindFirstPersonController();
        hudFades = FindObjectsByType<HUDZoomFade>(FindObjectsSortMode.None);

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

    private void SetHudHidden(bool hidden)
    {
        foreach (HUDZoomFade hudFade in hudFades)
        {
            if (hudFade != null)
            {
                hudFade.SetHudHidden(hidden);
            }
        }
    }

    // the scan markers' HUD is the fade on the same camera as the WorldHUDProjector
    private void SetScanHudHidden(bool hidden)
    {
        foreach (HUDZoomFade hudFade in hudFades)
        {
            if (hudFade != null && hudFade.TryGetComponent(out WorldHUDProjector _))
            {
                hudFade.SetHudHidden(hidden);
            }
        }
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
        stateMachine.RegisterState("Scan");
        stateMachine.RegisterState("DroneSequence");
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

        stateMachine.AddEdge("ZoomPrompt", "Scan", () => playerController != null && playerController.IsZoomHeld);

        stateMachine.AddEdge("Scan", "DroneSequence", () => droneScannable == null || droneScannable.IsScanned);

        stateMachine.AddEdge("DroneSequence", "Complete", () => IsSequenceFinished(droneSequence));

        stateMachine.GetState("Intro").OnEnterState += () =>
        {
            screenFader?.SetOpacity(1f);
            SetHudHidden(true);
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

        // the main HUD stays hidden until Complete, but the scan marker needs to be visible to scan with
        stateMachine.GetState("Scan").OnEnterState += () =>
        {
            zoomPrompt?.Hide();
            SetScanHudHidden(false);
        };

        stateMachine.GetState("DroneSequence").OnEnterState += () =>
        {
            droneSequence?.Play();
        };

        stateMachine.GetState("Complete").OnEnterState += () =>
        {
            onDroneSequenceFinished.Invoke();
            playerController?.ClearLookCone();
            SetHudHidden(false);
            SetAllControlsEnabled(true);
        };
    }
}
