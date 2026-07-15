using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarController : NetworkBehaviour
{
    [SerializeField, Range(0.6f, 1f)] private float m_nextGearRPM = 0.9f;
    [SerializeField, Range(0f, 0.6f)] private float m_prevGearRPM = 0.5f;
    [SerializeField, Range(0.3f, 3f)] private float m_waitTime = 0.4f;
    [SerializeField, Range(0.3f, 3f)] private float m_rpmShortMemoryTime = 0.3f;
    [SerializeField, Range(0.3f, 3f)] private float m_rpmLongMemoryTime = 2f;
    [SerializeField, Min(0)] private float m_rpmMaxDifference = 1500f;
    [SerializeField, Min(0)] private float m_rpmMinDifference = 200f;

    private MovingCar _car;
    private CarEngine _engine;
    private SmartBotAI _botBrain;

    private Coroutine m_gearBoxRoutine;

    private Queue<float> m_rpmsShort = new();
    private Queue<float> m_rpmsLong = new();
    private float m_rpmShortDifference;
    private float m_rpmLongDifference;
    private float m_lastRPM;

    private float m_forwardInput;
    private float m_backwardInput;

    private bool _isControlling;
    private bool _matchEnded;

    // Машина считается управляемой ботом, только если компонент SmartBotAI
    // присутствует И включён. Раньше проверялось только на null, из-за чего
    // выключенный SmartBotAI на префабе игрока всё равно блокировал EnableControlls().
    private bool IsBotControlled => _botBrain != null && _botBrain.enabled;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
        _engine = GetComponent<CarEngine>();
        _botBrain = GetComponent<SmartBotAI>();

        m_lastRPM = _engine.idleRPM;

        if (IsServer)
            _engine.OnBrake(1f);
    }

    private void Update()
    {
        if (!IsServer && !_isControlling)
            return;

        if (_matchEnded)
            return;

        if (_engine.currentGear == -2)
        {
            ApplyGasAndBrake(m_backwardInput, m_forwardInput);
        }
        else
        {
            ApplyGasAndBrake(m_forwardInput, m_backwardInput);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer && !_isControlling)
            return;

        if (_matchEnded)
            return;

        AutomaticGearBox();
    }

    private void ApplyGasAndBrake(float positive, float negative)
    {
        if (!IsServer && !_isControlling)
            return;

        _engine.OnGas(positive);
        _engine.OnBrake(negative);
        if (_engine.isClutchPressed && positive > 0.01f)
        {
            _engine.UnPressClutch();
        }
        else if (!_engine.isClutchPressed && negative > 0.01f && _engine.rpm < _engine.idleRPM * 2f)
        {
            _engine.PressClutch();
        }
    }

    private void AutomaticGearBox()
    {
        if (!IsServer && !_isControlling)
            return;

        if (m_gearBoxRoutine != null)
            return;

        if (_engine.currentGear >= 1 && _engine.rpm < _engine.idleRPM * 2 && m_forwardInput < 0.01f && m_backwardInput >= 0.01f && _car.linearVelocity.magnitude < 0.2f)
        {
            m_gearBoxRoutine = StartCoroutine(SetGearRoutine(0));
            return;
        }
        if (_engine.currentGear == 0 && _engine.rpm < _engine.idleRPM * 2 && m_forwardInput < 0.01f && m_backwardInput >= 0.01f && _car.linearVelocity.magnitude <= 0.001f)
        {
            _engine.SetGear(-2);
            return;
        }
        if (_engine.currentGear == -1 && m_forwardInput > 0.01f)
        {
            m_gearBoxRoutine = StartCoroutine(NextGearRoutine());
            return;
        }
        if (_engine.currentGear == -2)
        {
            if (_engine.rpm < _engine.idleRPM * 2 && m_forwardInput >= 0.01f && m_backwardInput < 0.01f && _car.projectedVelocityZ.z > -0.01f)
            {
                _engine.NextGear();
            }
            return;
        }

        int countShort = Mathf.RoundToInt(m_rpmShortMemoryTime / Time.fixedDeltaTime);
        int countLong = Mathf.RoundToInt(m_rpmLongMemoryTime / Time.fixedDeltaTime);

        float diff = _engine.rpm - m_lastRPM;

        m_rpmsShort.Enqueue(diff);
        m_rpmsLong.Enqueue(diff);
        m_rpmShortDifference += diff;
        m_rpmLongDifference += diff;
        m_lastRPM = _engine.rpm;
        if (m_rpmsShort.Count > countShort)
            m_rpmShortDifference -= m_rpmsShort.Dequeue();
        if (m_rpmsLong.Count > countLong)
            m_rpmLongDifference -= m_rpmsLong.Dequeue();

        float rpm = _engine.rpm - _engine.idleRPM;
        float peakRPM = _engine.peakRPM - _engine.idleRPM;

        float avgShortRpmChange = m_rpmShortDifference / countShort;
        float avgLongRpmChange = m_rpmLongDifference / countLong;

        bool isAcceleratingHard = avgShortRpmChange > m_rpmMaxDifference;
        bool isRpmStable = Mathf.Abs(avgLongRpmChange) < m_rpmMinDifference;
        bool isRpmDropping = avgShortRpmChange < -m_rpmMaxDifference;

        float currentGearMaxRpm = peakRPM * m_nextGearRPM;
        float currentGearMinRpm = peakRPM * m_prevGearRPM;

        bool shouldShiftUp = false;
        if (rpm > currentGearMaxRpm)
            shouldShiftUp = true;
        else if (isAcceleratingHard && rpm > currentGearMaxRpm * 0.85f)
            shouldShiftUp = true;

        bool shouldShiftDown = false;
        if (rpm < currentGearMinRpm)
            shouldShiftDown = true;
        else if (isRpmStable && rpm < currentGearMaxRpm * 0.6f && m_forwardInput > 0.7f)
            shouldShiftDown = true;
        else if (isRpmDropping && rpm < currentGearMaxRpm * 0.7f)
            shouldShiftDown = true;

        if (shouldShiftUp && _engine.currentGear < _engine.gears.Count - 1 && m_forwardInput > 0.01f)
            m_gearBoxRoutine = StartCoroutine(NextGearRoutine());
        else if (shouldShiftDown && _engine.currentGear > 0)
            m_gearBoxRoutine = StartCoroutine(PrevGearRoutine());
    }

    private IEnumerator NextGearRoutine()
    {
        _engine.NextGear();
        yield return new WaitForSeconds(_engine.clutchTime + m_waitTime);
        m_gearBoxRoutine = null;
    }

    private IEnumerator PrevGearRoutine()
    {
        _engine.PrevGear();
        yield return new WaitForSeconds(_engine.clutchTime + m_waitTime);
        m_gearBoxRoutine = null;
    }

    private IEnumerator SetGearRoutine(int gear)
    {
        if (_isControlling)
        {
            _engine.SetGear(gear);
        }
        yield return new WaitForSeconds(_engine.clutchTime + m_waitTime * 4);
        m_gearBoxRoutine = null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SubscribeServerToMatchEndWhenReady());
        }

        if (!IsOwner || IsBotControlled)
            return;

        ClientEventBus.Instance.InvokeCarOwn(_car);

        EnableControlls();

        StartCoroutine(SubscribeToMatchEndWhenReady());
    }

    public override void OnNetworkDespawn()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnPhaseChanged -= HandleMatchPhaseChangedServer;
        }

        if (!IsOwner || IsBotControlled)
            return;

        DisableControlls();

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnPhaseChanged -= HandleMatchPhaseChanged;
        }
    }

    private IEnumerator SubscribeServerToMatchEndWhenReady()
    {
        while (MatchManager.Instance == null)
            yield return null;

        MatchManager.Instance.OnPhaseChanged += HandleMatchPhaseChangedServer;

        HandleMatchPhaseChangedServer(MatchManager.Instance.Phase);
    }

    private void HandleMatchPhaseChangedServer(MatchPhase newPhase)
    {
        if (!IsServer) return;

        _matchEnded = newPhase == MatchPhase.PostCombat;

        if (_matchEnded)
        {
            m_forwardInput = 0f;
            m_backwardInput = 0f;
            _car.StopCar();
        }
    }

    private IEnumerator SubscribeToMatchEndWhenReady()
    {
        while (MatchManager.Instance == null)
            yield return null;

        MatchManager.Instance.OnPhaseChanged += HandleMatchPhaseChanged;

        HandleMatchPhaseChanged(MatchManager.Instance.Phase);
    }

    private void HandleMatchPhaseChanged(MatchPhase newPhase)
    {
        if (!IsOwner) return;

        if (newPhase == MatchPhase.PostCombat)
        {
            DisableControlls();
        }
        else
        {
            EnableControlls();
        }
    }

    public void OnGas(float value) => OnGasRpc(value);
    public void OnBrake(float value) => OnBrakeRpc(value);
    public void OnSteer(float value) => OnSteerRpc(value);
    public void OnShiftUp() => ShiftUpRpc();
    public void OnShiftDown() => ShiftDownRpc();

    public void EnableControlls()
    {
        if (!IsOwner || _isControlling)
            return;

        PlayerInputHandler.Instance.onGas += OnGas;
        PlayerInputHandler.Instance.onBrake += OnBrake;
        PlayerInputHandler.Instance.onSteer += OnSteer;

        _isControlling = true;
    }

    public void DisableControlls()
    {
        if (!IsOwner || !_isControlling)
            return;

        PlayerInputHandler.Instance.onGas -= OnGas;
        PlayerInputHandler.Instance.onBrake -= OnBrake;
        PlayerInputHandler.Instance.onSteer -= OnSteer;

        _isControlling = false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnGasRpc(float value)
    {
        if (_matchEnded) return;

        m_forwardInput = value;
        _engine.UnPressClutch();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnBrakeRpc(float value)
    {
        if (_matchEnded) return;

        m_backwardInput = value;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnSteerRpc(float value)
    {
        if (_matchEnded) return;

        _car.OnSteer(value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ShiftUpRpc()
    {
        if (_matchEnded) return;

        _engine.NextGear();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ShiftDownRpc()
    {
        if (_matchEnded) return;

        _engine.PrevGear();
    }
}