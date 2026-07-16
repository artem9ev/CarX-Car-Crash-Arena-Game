using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameStateMachine : MonoBehaviour
{
    private Dictionary<GameState, BaseState> _states = new Dictionary<GameState, BaseState>();
    private GameState? _currentState;

    public static GameStateMachine Instance { get; private set; }

    public BaseState CurrentState => _states[_currentState.Value];
    public GameState? CurrentStateType => _currentState;
    public UnityAction<GameState> onStateChange;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(this);

        _states[GameState.MainMenu] = new MainMenuState();
        _states[GameState.Level] = new LevelState();

        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState state)
    {
        if (_currentState == state)
            return;

        if (_currentState != null)
        {
            CurrentState.End();
        }
        _currentState = state;

        onStateChange?.Invoke(state);

        CurrentState.Start();
    }

    public void Update()
    {
        if (_currentState != null) 
        {
            CurrentState.Update();
        }
    }
}

public enum GameState
{
    MainMenu,
    Lobby,
    Level,
    PostCombat
}
