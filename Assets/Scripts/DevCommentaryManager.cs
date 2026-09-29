#nullable enable

using UnityEngine;

public class DevCommentaryManager : MonoBehaviour
{
    public static DevCommentaryManager? Instance;

    private DevCommentaryNode? activeNode;
    private uint activePlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

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

    public bool IsPlaying(DevCommentaryNode node)
    {
        return activeNode == node;
    }

    public void Toggle(DevCommentaryNode node)
    {
        if (activeNode == node)
        {
            Stop();
            return;
        }

        Stop();
        Play(node);
    }

    public void Stop()
    {
        if (activePlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.StopPlayingID(activePlayingId);
        }

        activePlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        activeNode = null;
    }

    private void Play(DevCommentaryNode node)
    {
        if (node.playEvent == null || !node.playEvent.IsValid())
        {
            return;
        }

        uint playingId = node.playEvent.Post(node.gameObject, (uint)AkCallbackType.AK_EndOfEvent, OnEventCallback);
        if (playingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            return;
        }

        activePlayingId = playingId;
        activeNode = node;
    }

    private void OnEventCallback(object cookie, AkCallbackType type, AkCallbackInfo info)
    {
        if (type != AkCallbackType.AK_EndOfEvent)
        {
            return;
        }

        // a log that finished on its own is no longer active, so the next interact restarts it;
        // ignore end-of-event from a log we already stopped or replaced
        var eventInfo = info as AkEventCallbackInfo;
        if (eventInfo != null && eventInfo.playingID == activePlayingId)
        {
            activePlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
            activeNode = null;
        }
    }
}
