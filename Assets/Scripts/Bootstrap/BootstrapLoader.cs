
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadBootstrapAndUI()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name.StartsWith("Level_"))
        {
            if (!SceneManager.GetSceneByName("Bootstrap").isLoaded)
            {
                SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
            }
        }
        else if (currentScene.name == "UI" || currentScene.name == "MainMenu")
        {
            if (!SceneManager.GetSceneByName("Bootstrap").isLoaded)
            {
                SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
            }
        }
    }
}
