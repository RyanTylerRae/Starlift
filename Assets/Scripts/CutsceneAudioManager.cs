using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages audio events for cutscenes using Wwise.
/// Can be referenced from Timeline to play specific sound events at precise frames.
/// </summary>
public class CutsceneAudioManager : MonoBehaviour
{
    /// <summary>
    /// Plays a Wwise sound event on a specific GameObject.
    /// Can be called from Timeline Animation Events, Signal Receivers, or custom Timeline Playables.
    /// </summary>
    /// <param name="wwiseEventId">The Wwise event ID to post</param>
    /// <param name="emitter">The GameObject to emit the sound from. Pass null for global playback.</param>
    public void PlayEvent(string wwiseEventId, GameObject emitter)
    {
        if (string.IsNullOrEmpty(wwiseEventId))
        {
            Debug.LogWarning("CutsceneAudioManager: Wwise event ID is empty");
            return;
        }

        uint playingId = AkUnitySoundEngine.PostEvent(wwiseEventId, emitter ?? gameObject);

        if (playingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            Debug.LogWarning($"CutsceneAudioManager: Failed to post Wwise event '{wwiseEventId}'");
        }
    }

    /// <summary>
    /// Plays a Wwise sound event globally (not attached to any GameObject).
    /// </summary>
    /// <param name="wwiseEventId">The Wwise event ID to post</param>
    public void PlayEventGlobal(string wwiseEventId)
    {
        PlayEvent(wwiseEventId, null);
    }

    // /// <summary>
    // /// Stops all sounds on the specified GameObject.
    // /// </summary>
    // public void StopAllSounds(GameObject emitter)
    // {
    //     AkSoundEngine.StopAll(emitter);
    // }

    // /// <summary>
    // /// Stops a specific Wwise event on a GameObject.
    // /// </summary>
    // /// <param name="wwiseEventId">The Wwise event ID to stop</param>
    // /// <param name="emitter">The GameObject that is emitting the sound. Pass null for global.</param>
    // public void StopEvent(string wwiseEventId, GameObject emitter)
    // {
    //     if (string.IsNullOrEmpty(wwiseEventId))
    //     {
    //         Debug.LogWarning("CutsceneAudioManager: Wwise event ID is empty");
    //         return;
    //     }

    //     AkSoundEngine.ExecuteActionOnEvent(wwiseEventId, AkActionOnEventType.AkActionOnEventType_Stop, emitter);
    // }
}
