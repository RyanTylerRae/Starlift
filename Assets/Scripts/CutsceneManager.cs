#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    public GameObject? HUDObject = null;

    private List<string> cutsceneQueue = new();
    private bool isPlayingCutscenes = false;
    private string currentCutscene = "";

    public void Awake()
    {
        Instance = this;
    }

    public void Update()
    {
        // if cutscenes were playing, there are no more, and the last one has finished
        if (isPlayingCutscenes && cutsceneQueue.Count == 0 && currentCutscene == "")
        {
            TakePlayerControl(false);
            isPlayingCutscenes = false;
        }

        // else if we have cutscenes to play and need to start the next one
        if (cutsceneQueue.Count > 0 && currentCutscene == "")
        {
            currentCutscene = cutsceneQueue.First();
            cutsceneQueue.RemoveAt(0);

            StartCoroutine(PlayCutscene(currentCutscene));

            if (!isPlayingCutscenes)
            {
                isPlayingCutscenes = true;
                TakePlayerControl(true);
            }
        }
    }

    private void TakePlayerControl(bool removeControl)
    {
        var playerObject = StarliftStatics.FindPlayer();
        playerObject?.SetActive(!removeControl);

        HUDObject?.SetActive(!removeControl);
    }

    public void QueueCutscene(string sceneName)
    {
        cutsceneQueue.Add(sceneName);
    }

    private IEnumerator PlayCutscene(string sceneName)
    {
        string originalSceneName = SceneManager.GetActiveScene().name;

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return op;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        var playableDirector = FindFirstObjectByType<PlayableDirector>();

        bool timelineComplete = false;
        playableDirector.stopped += _ => timelineComplete = true;
        while (!timelineComplete)
        {
            yield return null;
        }

        SceneManager.UnloadSceneAsync(sceneName);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(originalSceneName));

        currentCutscene = "";
    }
}
