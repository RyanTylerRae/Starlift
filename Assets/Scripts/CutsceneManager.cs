using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    public void Awake()
    {
        Instance = this;
    }

    public void StartCutscene(string sceneName)
    {
        StartCoroutine(PlayCutscene(sceneName));
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
    }
}
