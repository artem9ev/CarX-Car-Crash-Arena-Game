
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    private static bool _shouldFastConnect = false;

    public static bool ShouldFastConnect => _shouldFastConnect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadBootstrapAndUI()
    {
        _shouldFastConnect = false;

        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name.StartsWith("Level_"))
        {
            if (!SceneManager.GetSceneByName("Bootstrap").isLoaded)
            {
                SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
            }

            _shouldFastConnect = true;
        }
        else if (currentScene.name == "MainMenu")
        {
            if (!SceneManager.GetSceneByName("Bootstrap").isLoaded)
            {
                SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
            }
        }
    }
}
