
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
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);

            SceneManager.LoadScene("UI", LoadSceneMode.Additive);
        }
        else if (currentScene.name == "UI")
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
        }
    }
}
