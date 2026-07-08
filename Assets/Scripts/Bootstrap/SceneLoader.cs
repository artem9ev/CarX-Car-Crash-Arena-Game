using System.Collections;
using Unity.Services.Multiplayer.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string _uiName;
    [SerializeField] private string _mainName;
    [SerializeField] private string _lobbyName;

    private string _currentSceneName;
    private Coroutine _loadRoutine;

    private static SceneLoader _instance;

    public static SceneLoader Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        LoadUI();
        LoadMainMenu();

        DontDestroyOnLoad(gameObject);
    }

    private void LoadUI()
    {
        if (SceneManager.GetSceneByName(_uiName).isLoaded)
        {
            return;
        }

        SceneManager.LoadScene(_uiName, LoadSceneMode.Additive);
    }

    private IEnumerator LoadSceneAsync(string name)
    {
        float sceneProgress = 0f;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);

        while (!loadOperation.isDone)
        {
            sceneProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);

            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName(name);
        SceneManager.SetActiveScene(loadedScene);

        AsyncOperation unloadOperation = null;
        if (!string.IsNullOrEmpty(_currentSceneName))
        {
            unloadOperation = SceneManager.UnloadSceneAsync(_currentSceneName);
        }

        while (unloadOperation != null && !unloadOperation.isDone)
        {
            if (unloadOperation != null)
            {
                sceneProgress += Mathf.Clamp01(unloadOperation.progress);
                sceneProgress /= 2f;
            }

            yield return null;
        }

        _currentSceneName = name;
        _loadRoutine = null;
    }

    public void LoadLobby()
    {
        if (_loadRoutine != null)
        {
            _loadRoutine = null;
        }

        if (SceneManager.GetSceneByName("Bootstrap").isLoaded)
        {
            //SceneManager.UnloadSceneAsync("Bootstrap");
        }
        if (SceneManager.GetSceneByName(_uiName).isLoaded)
        {
            //SceneManager.UnloadSceneAsync(_uiName);
        }

        //_loadRoutine = StartCoroutine(LoadSceneAsync(_lobbyName));
    }

    public void LoadMainMenu()
    {
        if (SceneManager.GetSceneByName(_mainName).isLoaded)
        {
            return;
        }

        _loadRoutine = StartCoroutine(LoadSceneAsync(_mainName));
    }
}
