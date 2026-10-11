#nullable enable

using System;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class DirectedSequence : MonoBehaviour
{
    public event Action? OnFinished;

    private PlayableDirector? director = null;
    private bool hasStarted = false;
    private bool hasFinished = false;

    public bool IsPlaying { get { return hasStarted && !hasFinished; } }

    public bool IsFinished { get { return hasFinished; } }

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        if (director == null)
        {
            return;
        }

        if (director.state == PlayState.Playing)
        {
            director.Stop();
        }

        ConfigureDirector(director);
        director.stopped += OnDirectorStopped;
    }

    private void OnDestroy()
    {
        if (director != null)
        {
            director.stopped -= OnDirectorStopped;
        }
    }

    private void OnValidate()
    {
        if (TryGetComponent(out PlayableDirector playableDirector))
        {
            ConfigureDirector(playableDirector);
        }
    }

    public void Play()
    {
        if (director == null)
        {
            return;
        }

        hasStarted = true;
        hasFinished = false;

        if (director.playableAsset == null)
        {
            Debug.LogWarning($"DirectedSequence: '{name}' has no timeline assigned, skipping.");
            Finish();
            return;
        }

        director.time = 0.0;
        director.Play();
    }

    public void Stop()
    {
        if (director == null || !IsPlaying)
        {
            return;
        }

        director.Stop();
    }

    private void ConfigureDirector(PlayableDirector playableDirector)
    {
        playableDirector.playOnAwake = false;
        playableDirector.extrapolationMode = DirectorWrapMode.None;
    }

    private void OnDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (!IsPlaying)
        {
            return;
        }

        Finish();
    }

    private void Finish()
    {
        hasFinished = true;
        OnFinished?.Invoke();
    }
}
