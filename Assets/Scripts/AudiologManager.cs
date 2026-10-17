#nullable enable

using UnityEngine;

/// <summary>
/// Plays one AudiologComponent at a time through Wwise and tracks its progress for AudiologHUD.
/// The log's length comes from Wwise's duration callback and its position from the source play
/// position, so progress follows the actual audio. The waveform level is read from a Game Parameter
/// driven by a Wwise Meter effect on the audiolog bus, when one is assigned and being set.
/// </summary>
public class AudiologManager : MonoBehaviour
{
    public static AudiologManager? Instance;

    [Header("Waveform Meter")]
    // Game Parameter written by a Meter effect on the bus the audiologs play through
    public AK.Wwise.RTPC? meterParameter;

    // meter values that map to a flat line and to the top of the waveform box
    public float meterSilentValue = -48.0f;
    public float meterLoudValue = 0.0f;

    private AudiologComponent? activeLog;
    private uint activePlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    private float durationMs = 0.0f;
    private float progress = 0.0f;

    public AudiologComponent? ActiveLog { get { return activeLog; } }

    // 0..1 through the active log; stays at 0 until Wwise reports the log's duration
    public float Progress { get { return progress; } }

    public void Awake()
    {
        Instance = this;
    }

    public void OnDestroy()
    {
        Stop();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsPlaying(AudiologComponent log)
    {
        return activeLog == log;
    }

    public void Play(AudiologComponent log)
    {
        Stop();

        if (log.playEvent == null || !log.playEvent.IsValid())
        {
            return;
        }

        uint callbackFlags = (uint)(AkCallbackType.AK_EndOfEvent | AkCallbackType.AK_Duration);
        uint playingId = log.playEvent.Post(log.gameObject, callbackFlags, OnEventCallback);
        if (playingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            return;
        }

        activePlayingId = playingId;
        activeLog = log;
    }

    public void Stop()
    {
        if (activePlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.StopPlayingID(activePlayingId);
        }

        activePlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        activeLog = null;
        durationMs = 0.0f;
        progress = 0.0f;
    }

    // the meter level mapped to 0..1. Returns false when there's no meter to read - no parameter
    // assigned, nothing playing, or the parameter has never been set (the Meter effect isn't set up)
    public bool TryGetMeterLevel(out float level)
    {
        level = 0.0f;

        if (meterParameter == null || !meterParameter.IsValid() || activeLog == null)
        {
            return false;
        }

        int valueType = (int)AkQueryRTPCValue.RTPCValue_Global;
        AKRESULT result = AkUnitySoundEngine.GetRTPCValue(meterParameter.Id, null, AkUnitySoundEngine.AK_INVALID_PLAYING_ID, out float value, ref valueType);

        // Wwise falls back to the parameter's default when no global value exists
        if (result != AKRESULT.AK_Success || valueType == (int)AkQueryRTPCValue.RTPCValue_Default)
        {
            return false;
        }

        level = Mathf.InverseLerp(meterSilentValue, meterLoudValue, value);
        return true;
    }

    public void Update()
    {
        if (activePlayingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID || durationMs <= 0.0f)
        {
            return;
        }

        // not found until the source actually starts, so progress holds at its last value until then
        if (AkUnitySoundEngine.GetSourcePlayPosition(activePlayingId, out int positionMs) == AKRESULT.AK_Success)
        {
            progress = Mathf.Clamp01(positionMs / durationMs);
        }
    }

    private void OnEventCallback(object cookie, AkCallbackType type, AkCallbackInfo info)
    {
        // ignore callbacks from a log we already stopped or replaced
        var eventInfo = info as AkEventCallbackInfo;
        if (eventInfo == null || eventInfo.playingID != activePlayingId)
        {
            return;
        }

        if (type == AkCallbackType.AK_Duration)
        {
            // one callback per source; the longest one is the log's length
            var durationInfo = info as AkDurationCallbackInfo;
            if (durationInfo != null)
            {
                durationMs = Mathf.Max(durationMs, durationInfo.fEstimatedDuration);
            }
            return;
        }

        if (type == AkCallbackType.AK_EndOfEvent)
        {
            // a log that finished on its own is no longer active, so it can be interacted with again
            activePlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
            activeLog = null;
            durationMs = 0.0f;
            progress = 0.0f;
        }
    }
}
