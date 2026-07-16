using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelPresenter : PresenterUI
{
    [Header("Views")]
    [SerializeField] private GameHUDView m_hudView;
    [SerializeField] private RacePauseView m_pauseView;
    [SerializeField] private RaceResultsView m_resultsView;

    //private Race m_model;
    private MovingCar _car;

    private void Awake()
    {
        AddView(m_hudView);
        AddView(m_pauseView);
        AddView(m_resultsView);

        //ClientEventBus.onAwake += Subscribe;
    }

    private void OnDestroy()
    {
        //ClientEventBus.onAwake -= Subscribe;
    }

    public override void Subscribe()
    {
        if (ClientEventBus.Instance != null)
        {
            ClientEventBus.Instance.onCarOwn += OnCarOwn;
        }

        GameStateMachine.Instance.CurrentState.onUpdate += UpdateUI;
        GameStateMachine.Instance.CurrentState.onEnd += EndUI;

        ActivateView(m_hudView);
    }

    public override void Unsubscribe()
    {
        if (ClientEventBus.Instance != null)
        {
            ClientEventBus.Instance.onCarOwn -= OnCarOwn;
        }

        GameStateMachine.Instance.CurrentState.onUpdate -= UpdateUI;
        GameStateMachine.Instance.CurrentState.onEnd -= EndUI;
        DeactivateView(m_hudView);
    }

    private void OnCarOwn(MovingCar car)
    {
        _car = car;
        m_hudView.SetUpRPMmeter(_car.engine.peakRPM);
    }

    private void RacePause()
    {
        m_hudView.Deactivate();
        m_pauseView.Activate();
    }
    private void RaceResume()
    {
        m_hudView.Activate();
        m_pauseView.Deactivate();
    }

    private void OnFinish()
    {
        /*RaceEventBus.instance.onPause -= RacePause;
        RaceEventBus.instance.onResume -= RaceResume;

        m_model.OnTimeChanged -= UpdateTime;
        m_model.OnLapsChanged -= UpdateLaps;
*/
        m_hudView.Deactivate();
        m_pauseView.Deactivate();
        m_resultsView.Activate();

        //DrawPlayersScores();

        m_hudView.ResetTimer();
        m_hudView.Deinit();
    }

    /*private void DrawPlayersScores()
    {
        if (RaceMultiplayerSession.instance == null)
        {
            return;
        }
        RaceMultiplayerSession multiplayerSession = RaceMultiplayerSession.instance;

        if (multiplayerSession.sessions == null || multiplayerSession.sessions.Count == 0 || !YG2.player.auth && !multiplayerSession.isSessionsLoaded)
        {
            m_resultsView.SetTimeResult(Race.instance.CurrentTime);
            return;
        }

        m_resultsView.SetPlayersTimers(multiplayerSession.playersResults);
    }*/

    // hud

    private void UpdateTime()
    {
        //m_hudView.SetTime(m_model.CurrentTime);
    }
    private void UpdateSpeed()
    {
        m_hudView.SetSpeed(_car.projectedVelocityZ.magnitude);
        m_hudView.SetGear(_car.engine.currentGear);
        m_hudView.SetRpm(_car.engine.rpm);
    }

    private void StartUI()
    {

    }

    private void UpdateUI()
    {
        if (_car != null)
        {
            UpdateSpeed();
        }
    }

    private void EndUI()
    {

    }
}