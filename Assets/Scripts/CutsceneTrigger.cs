#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneTrigger : MonoBehaviour
{
    public List<string> sceneNames;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out FirstPersonController controller))
        {
            foreach (string sceneName in sceneNames)
            {
                CutsceneManager.Instance.QueueCutscene(sceneName);
            }

            // only trigger cutscenes once
            Destroy(gameObject);
        }
    }
}
