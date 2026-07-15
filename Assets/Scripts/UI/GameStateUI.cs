using UnityEngine;

public class GameStateUI : MonoBehaviour
{
    [SerializeField] private MainMenuPresenter _mainPresenter;
    [SerializeField] private LevelPresenter _levelPresenter;

    private static GameStateUI _instance;
    public static GameStateUI Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameStateMachine.Instance.onStateChange += OnGameStateChange;

        if (GameStateMachine.Instance.CurrentState != null)
        {
            OnGameStateChange(GameStateMachine.Instance.CurrentStateType.Value);
        }
    }

    private void OnDestroy()
    {
        GameStateMachine.Instance.onStateChange -= OnGameStateChange;
    }

    public void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                _mainPresenter.Subscribe();
                _levelPresenter.Unsubscribe();
                break;
            case GameState.Lobby:

                break;
            case GameState.Level:
                _mainPresenter.Unsubscribe();
                _levelPresenter.Subscribe();
                break;
            case GameState.PostCombat:
                
                break;
            default:
                break;
        }
    }
}
