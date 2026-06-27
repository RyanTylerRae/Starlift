#nullable enable

using UnityEngine;

public class ToggleObject : MonoBehaviour
{
    public GameObject? target;
    public string targetName = "";

    public void Toggle()
    {
        GameObject? resolved = target;

        if (resolved == null && !string.IsNullOrEmpty(targetName))
        {
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obj.name == targetName)
                {
                    resolved = obj;
                    break;
                }
            }
        }

        if (resolved == null)
        {
            return;
        }

        resolved.SetActive(!resolved.activeSelf);
    }
}
