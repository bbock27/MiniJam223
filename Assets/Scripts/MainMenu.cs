using UnityEngine;
using UnityEngine.SceneManagement;

/// Actions for the main menu's buttons, wired up in their OnClick lists.
public class MainMenu : MonoBehaviour
{
    public string gameScene = "Brady";
    public GameObject quitButton;

    void Start()
    {
        // Browsers don't let a page close itself, so Quit would do nothing there
        if (quitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
            quitButton.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Application.Quit does nothing in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
