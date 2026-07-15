using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarController : NetworkBehaviour
{
    [Header("Automatic gearbox — shift points")]
    [SerializeField, Range(0.6f, 1f)] private float m_nextGearRPM = 0.9f;
    [SerializeField, Range(0f, 0.6f)] private float m_prevGearRPM = 0.5f;
    [SerializeField, Range(0.3f, 3f)] private float m_waitTime = 0.4f;
    [SerializeField, Range(0.3f, 3f)] private float m_rpmShortMemoryTime = 0.3f;
    [SerializeField, Range(0.3f, 3f)] private float m_rpmLongMemoryTime = 2f;
    [SerializeField, Min(0)] private float m_rpmMaxDifference = 1500f;
    [SerializeField, Min(0)] private float m_rpmMinDifference = 200f;

    [Header("Reverse engagement")]
    [SerializeField, Min(0f)] private float m_standstillSpeed = 0.2f;
    [Tooltip("Сколько нужно держать тормоз на 1й передаче в стоп-состоянии, прежде чем реально включится реверс.")]
    [SerializeField, Min(0f)] private float m_reverseEngageDelay = 0.25f;

    private MovingCar _car;
    private CarEngine _engine;

    // Единственная блокировка на переключение — гарантирует, что в моменте
    // выполняется не больше одного изменения передачи.
    private Coroutine m_shiftRoutine;
    private bool ShiftInProgress => m_shiftRoutine != null;

    // Таймер намерения въехать в реверс (антидребезг короткого тапа по тормозу).
    private float m_reverseIntentTimer;

    private readonly Queue<float> m_rpmsShort = new();
    private readonly Queue<float> m_rpmsLong = new();
    private float m_rpmShortDifference;
    private float m_rpmLongDifference;
    private float m_lastRPM;

    private float m_forwardInput;
    private float m_backwardInput;

    private bool _isControlling;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
        _engine = GetComponent<CarEngine>();

        m_lastRPM = _engine.idleRPM;

        if (IsServer)
            _engine.OnBrake(1f);
    }

    private bool _matchEnded;

    private void Update()
    {
        if (!IsServer && !_isControlling)
            return;

        if (_matchEnded)
        {
            return;
        }

        float dot = 0;
        if (_car.linearVelocity.magnitude > 0.001f)
        {
            dot = Vector3.Dot(transform.forward, _car.linearVelocity.normalized);
        }

        // На реверсе педали физически инвертированы.
        if (_engine.currentGear == -2)
            ApplyGasAndBrake(m_backwardInput, m_forwardInput);
        else
            ApplyGasAndBrake(m_forwardInput, m_backwardInput);
    }

    private void FixedUpdate()
    {
        if (!IsServer && !_isControlling)
            return;

        if (ShiftInProgress)
            return;

        int gear = _engine.currentGear;

        if (gear == -2)
            TickReverse();
        else if (gear == -1)
            TickNeutral();
        else
            TickDrive(gear);

        if (_matchEnded)
        {
            return;
        }
        AutomaticGearBox();
    }

    // ---------------- Reverse (-2) ----------------

    private void TickReverse()
    {
        m_reverseIntentTimer = 0f; // сброс на случай следующего входа в reverse-намерение

        bool wantsForward = m_forwardInput >= 0.01f && m_backwardInput < 0.01f;
        bool notRollingBack = _car.projectedVelocityZ.z > -0.01f;

        if (_engine.rpm < _engine.idleRPM * 2f && wantsForward && notRollingBack)
            RequestGearChange(() => _engine.NextGear()); // -2 -> -1 (neutral)
    }

    // ---------------- Neutral (-1) ----------------

    private void TickNeutral()
    {
        if (m_forwardInput > 0.01f)
        {
            RequestGearChange(() => _engine.NextGear()); // -1 -> 0 (1st gear)
            return;
        }

        // Из нейтрали тоже можно уйти напрямую в реверс, если стоим и держим тормоз.
        if (WantsReverseFromStandstill())
        {
            m_reverseIntentTimer += Time.fixedDeltaTime;
            if (m_reverseIntentTimer >= m_reverseEngageDelay)
                RequestGearChange(() => _engine.SetGear(-2));
        }
        else
        {
            m_reverseIntentTimer = 0f;
        }
    }

    // ---------------- Drive (gear >= 0) ----------------

    private bool WantsReverseFromStandstill() =>
        m_forwardInput < 0.01f && m_backwardInput >= 0.01f &&
        _car.linearVelocity.magnitude < m_standstillSpeed &&
        _engine.rpm < _engine.idleRPM * 2f;

    private void TickDrive(int gear)
    {
        if (gear >= 1)
        {
            // Сначала спускаемся до 1й передачи, прежде чем разрешить реверс.
            if (WantsReverseFromStandstill())
            {
                RequestGearChange(() => _engine.SetGear(0));
                m_reverseIntentTimer = 0f;
                return;
            }
        }
        else // gear == 0
        {
            if (WantsReverseFromStandstill())
            {
                m_reverseIntentTimer += Time.fixedDeltaTime;
                if (m_reverseIntentTimer >= m_reverseEngageDelay)
                {
                    RequestGearChange(() => _engine.SetGear(-2));
                    return;
                }
            }
            else
            {
                m_reverseIntentTimer = 0f;
            }
        }

        UpdateAutomaticShift(gear);
    }

    /// <summary>Единая точка входа для любой смены передачи — не даёт запустить второе переключение поверх текущего.</summary>
    private void RequestGearChange(System.Action changeGear)
    {
        if (ShiftInProgress) return;
        m_shiftRoutine = StartCoroutine(GearChangeRoutine(changeGear));
    }

    private IEnumerator GearChangeRoutine(System.Action changeGear)
    {
        int gearBefore = _engine.currentGear;
        changeGear();
        yield return new WaitForSeconds(_engine.clutchTime + m_waitTime);
        m_shiftRoutine = null;

        // Если только что вышли из зоны нормального автомата передач (gear>=0) —
        // не тащим накопленную статистику RPM в реверс/нейтраль и обратно.
        bool wasDriveNormalShift = gearBefore >= 0 && _engine.currentGear >= 0;
        if (!wasDriveNormalShift)
            ResetRpmTracking();
    }

    private void ResetRpmTracking()
    {
        m_rpmsShort.Clear();
        m_rpmsLong.Clear();
        m_rpmShortDifference = 0f;
        m_rpmLongDifference = 0f;
        m_lastRPM = _engine.rpm;
    }

    private void UpdateAutomaticShift(int gear)
    {
        int countShort = Mathf.Max(1, Mathf.RoundToInt(m_rpmShortMemoryTime / Time.fixedDeltaTime));
        int countLong = Mathf.Max(1, Mathf.RoundToInt(m_rpmLongMemoryTime / Time.fixedDeltaTime));

        float diff = _engine.rpm - m_lastRPM;
        m_lastRPM = _engine.rpm;

        m_rpmsShort.Enqueue(diff);
        m_rpmShortDifference += diff;
        if (m_rpmsShort.Count > countShort)
            m_rpmShortDifference -= m_rpmsShort.Dequeue();

        m_rpmsLong.Enqueue(diff);
        m_rpmLongDifference += diff;
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

        bool shouldShiftUp = rpm > currentGearMaxRpm
            || (isAcceleratingHard && rpm > currentGearMaxRpm * 0.85f);

        bool shouldShiftDown = rpm < currentGearMinRpm
            || (isRpmStable && rpm < currentGearMaxRpm * 0.6f && m_forwardInput > 0.7f)
            || (isRpmDropping && rpm < currentGearMaxRpm * 0.7f);

        if (shouldShiftUp && gear < _engine.gears.Count - 1 && m_forwardInput > 0.01f)
            RequestGearChange(() => _engine.NextGear());
        else if (shouldShiftDown && gear > 0)
            RequestGearChange(() => _engine.PrevGear());
    }

    private void ApplyGasAndBrake(float positive, float negative)
    {
        _engine.OnGas(positive);
        _engine.OnBrake(negative);

        if (_engine.isClutchPressed && positive > 0.01f)
            _engine.UnPressClutch();
        else if (!_engine.isClutchPressed && negative > 0.01f && _engine.rpm < _engine.idleRPM * 2f)
            _engine.PressClutch();
    }

    // ---------------- Networking / controls (без изменений) ----------------

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SubscribeServerToMatchEndWhenReady());
        }

        if (!IsOwner)
            return;

        ClientEventBus.Instance.InvokeCarOwn(_car);
        EnableControlls();

        // Как только матч заканчивается (MatchManager переходит в PostCombat),
        // управление машиной должно отключиться — так же, как при смерти.
        StartCoroutine(SubscribeToMatchEndWhenReady());
    }

    public override void OnNetworkDespawn()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnPhaseChanged -= HandleMatchPhaseChangedServer;
        }

        if (!IsOwner)
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
        {
            yield return null;
        }

        MatchManager.Instance.OnPhaseChanged += HandleMatchPhaseChangedServer;

        // На случай, если машина заспавнилась уже после конца матча (маловероятно, но на всякий случай).
        HandleMatchPhaseChangedServer(MatchManager.Instance.Phase);
    }

    /// <summary>
    /// Серверная блокировка: гасит текущий инпут и обнуляет флаги газа/тормоза,
    /// чтобы машина реально остановилась, а не продолжала ехать по инерции
    /// последнего полученного от клиента значения газа.
    /// </summary>
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
        {
            yield return null;
        }

        MatchManager.Instance.OnPhaseChanged += HandleMatchPhaseChanged;

        // На случай, если игрок заспавнился, когда матч уже в PostCombat (late join) —
        // сразу применяем текущую фазу, а не ждём следующего OnPhaseChanged.
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
        if (!IsOwner || _isControlling) return;

        PlayerInputHandler.Instance.onGas += OnGas;
        PlayerInputHandler.Instance.onBrake += OnBrake;
        PlayerInputHandler.Instance.onSteer += OnSteer;

        _isControlling = true;
    }

    public void DisableControlls()
    {
        if (!IsOwner || !_isControlling) return;

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