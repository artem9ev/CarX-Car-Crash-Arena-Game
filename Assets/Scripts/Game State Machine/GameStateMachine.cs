using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameStateMachine : MonoBehaviour
{
    public enum State
    {
        MainMenu,
        Level
    }

    private Dictionary<State, BaseState> _states = new Dictionary<State, BaseState>();
    private State? _currentState;

    public static GameStateMachine Instance { get; private set; }

    public BaseState CurrentState => _states[_currentState.Value];
    public UnityAction<State> onStateChange;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);

        _states[State.MainMenu] = new MainMenuState();
        _states[State.Level] = new LevelState();
    }

    public void ChangeState(State state)
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
}
