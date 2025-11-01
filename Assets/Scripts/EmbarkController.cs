#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EmbarkController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad = "Lobby";
    public GameObject? textGameObject;

    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            textGameObject?.SetActive(true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            textGameObject?.SetActive(false);
            playerInRange = false;
        }
    }

    // Call this method from your input system when the interact input is pressed
    public void OnEmbark()
    {
        if (playerInRange)
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
