using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string _uiName;
    [SerializeField] private string _mainName;
    [SerializeField] private string _lobbyName;
    [SerializeField] private string _levelName;

    private Scene _loadedScene;
    private bool _isNetworkSetUped = false;

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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadUI();
        LoadMainMenu();
    }

    private bool NetworkSceneLoadValidation(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Validation for {sceneName}, mode {loadSceneMode}, index {sceneIndex}");

        if (sceneName == _uiName || sceneName == _mainName || sceneName == "Bootstrap")
            return false;

        if (loadSceneMode == LoadSceneMode.Single)
            return false;

        Debug.Log($"LOADINGGGGGGGGG {sceneName}");
        return true;
    }

    private bool NetworkSceneUnloadValidation(Scene scene)
    {
        // Запрещаем выгрузку Bootstrap и UI
        if (scene.name == "Bootstrap" || scene.name == _uiName)
        {
            return false;
        }

        return true;
    }

    private void SetupNetcodeSceneManager()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[SceneLoader] NetworkManager ещё не инициализирован. Настройка отложена.");
            return;
        }

        var sceneManager = NetworkManager.Singleton.SceneManager;
        sceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

        if (NetworkManager.Singleton.IsServer)
        {
            sceneManager.VerifySceneBeforeLoading = NetworkSceneLoadValidation;
            sceneManager.VerifySceneBeforeUnloading = NetworkSceneUnloadValidation;
        }
    }

    /*public void StartGame()
    {
        SetupNetcodeSceneManager();

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }
        var status = NetworkManager.Singleton.SceneManager.LoadScene(_levelName, LoadSceneMode.Additive);
        CheckStatus(status);
    }*/
    public void StartGame()
    {
        SetupNetcodeSceneManager();

        if (!NetworkManager.Singleton.IsServer)
            return;

        StartCoroutine(LoadLevelAdditiveCoroutine());
    }


    private IEnumerator LoadLevelAdditiveCoroutine()
    {
        // Если сцена уровня уже загружена – выгружаем её
        var levelScene = SceneManager.GetSceneByName(_levelName);
        if (levelScene.isLoaded)
        {
            Debug.Log($"Unloading existing level scene '{_levelName}' before loading as Additive.");
            var unloadOp = SceneManager.UnloadSceneAsync(levelScene);
            yield return unloadOp; // ждём завершения выгрузки
        }

        // Теперь загружаем аддитивно
        var status = NetworkManager.Singleton.SceneManager.LoadScene(_levelName, LoadSceneMode.Additive);
        CheckStatus(status);
    }

    private void CheckStatus(SceneEventProgressStatus status, bool isLoading = true)
    {
        var sceneEventAction = isLoading ? "load" : "unload";
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to {sceneEventAction} {_levelName} with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }
    
    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                if (sceneEvent.SceneName.StartsWith("Level_"))
                {
                    GameStateMachine.Instance.ChangeState(GameState.Level);
                }
                break;
            case SceneEventType.LoadComplete:
                if (sceneEvent.SceneName == _levelName)
                    UnloadMainMenu();

                // We want to handle this for only the server-side
                if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                {
                    // *** IMPORTANT ***
                    // Keep track of the loaded scene, you need this to unload it
                    _loadedScene = sceneEvent.Scene;
                }
                //SceneManager.SetActiveScene(sceneEvent.Scene);

                Debug.Log($"Loaded the {sceneEvent.SceneName} scene on {clientOrServer}-({sceneEvent.ClientId}).");
                break;

            case SceneEventType.UnloadComplete:
                Debug.Log($"Unloaded the {sceneEvent.SceneName} scene on {clientOrServer}-({sceneEvent.ClientId}).");
                break;

            case SceneEventType.LoadEventCompleted:

            case SceneEventType.UnloadEventCompleted:
                var loadUnload = sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ? "Load" : "Unload";
                Debug.Log($"{loadUnload} event completed for the following client identifiers:({sceneEvent.ClientsThatCompleted})");
                if (sceneEvent.ClientsThatTimedOut.Count > 0)
                        Debug.LogWarning($"{loadUnload} event timed out for the following client identifiers:({sceneEvent.ClientsThatTimedOut})");
                break;
        }
    }

    private void LoadUI()
    {
        if (SceneManager.GetSceneByName(_uiName).isLoaded)
            return;

        SceneManager.LoadScene(_uiName, LoadSceneMode.Additive);
    }

    public void LoadMainMenu()
    {
        if (SceneManager.GetSceneByName(_mainName).isLoaded)
            return;

        SceneManager.LoadScene(_mainName, LoadSceneMode.Additive);
    }

    public void UnloadMainMenu()
    {
        if (!SceneManager.GetSceneByName(_mainName).isLoaded)
            return;

        SceneManager.UnloadSceneAsync(_mainName);
    }
}
