using UnityEngine;
using UnityEngine.Events;

public interface IRaceModel
{
    float CurrentSpeed { get; }
    float CurrentTime { get; }
    float CurrentStartTimer { get; }
    int CurrentLap { get; }
    int LapsCount { get; }

    event UnityAction OnSpeedChanged;
    event UnityAction OnTimeChanged;
    event UnityAction OnStartTimerChanged;
    event UnityAction OnLapsChanged;
}
