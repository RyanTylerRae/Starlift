#nullable enable

using UnityEngine;

/// <summary>
/// An audiolog's settings. Call Play (e.g. from an InteractUseable's InteractEvent) to start it through
/// AudiologManager; AudiologHUD shows it on the HUD (waveform, progress, title, speaker).
/// Playing a log that's already playing does nothing; starting a different log replaces it.
/// </summary>
public class AudiologComponent : MonoBehaviour
{
    public AK.Wwise.Event? playEvent;

    [Header("Localization")]
    public string titleKey = "";
    public string speakerKey = "";

    public void Play()
    {
        AudiologManager? manager = AudiologManager.Instance;
        if (manager == null || playEvent == null || !playEvent.IsValid() || manager.IsPlaying(this))
        {
            return;
        }

        manager.Play(this);
    }
}
