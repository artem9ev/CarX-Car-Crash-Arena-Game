using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;

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

    private Coroutine m_gearBoxRoutine;

    private Queue<float> m_rpmsShort = new();
    private Queue<float> m_rpmsLong = new();
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

    private void Update()
    {
        if (!IsServer && !_isControlling)
        {
            return;
        }

        float dot = 0;
        if (_car.linearVelocity.magnitude > 0.001f)
        {
            dot = Vector3.Dot(transform.forward, _car.linearVelocity.normalized);
        }

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
        {
            return;
        }
        AutomaticGearBox();
    }

    private void ApplyGasAndBrake(float positive, float negative)
    {
        if (!IsServer && !_isControlling)
        {
            return;
        }

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
        {
            return;
        }

        if (m_gearBoxRoutine != null)
        {
            return;
        }
        if (_engine.currentGear >= 1 && _engine.rpm < _engine.idleRPM * 2 && m_forwardInput < 0.01f && m_backwardInput >= 0.01f && _car.linearVelocity.magnitude < 0.2f)
        {
            // при тормозе в пол сначала переход на первую передачу
            m_gearBoxRoutine = StartCoroutine(SetGearRoutine(0)); // вызовет небольшую задержку чтобы не произощел переход на реверс
            return;
        }
        if (_engine.currentGear == 0 && _engine.rpm < _engine.idleRPM * 2 && m_forwardInput < 0.01f && m_backwardInput >= 0.01f && _car.linearVelocity.magnitude <= 0.001f)
        {
            // затем переход на реверс
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
        {
            m_rpmShortDifference -= m_rpmsShort.Dequeue();
        }
        if (m_rpmsLong.Count > countLong)
        {
            m_rpmLongDifference -= m_rpmsLong.Dequeue();
        }

        float rpm = _engine.rpm - _engine.idleRPM;
        float peakRPM = _engine.peakRPM - _engine.idleRPM;

        float avgShortRpmChange = m_rpmShortDifference / countShort;
        float avgLongRpmChange = m_rpmLongDifference / countLong;

        bool isAcceleratingHard = avgShortRpmChange > m_rpmMaxDifference; // Быстрый набор оборотов
        bool isRpmStable = Mathf.Abs(avgLongRpmChange) < m_rpmMinDifference; // Обороты стабильны (зависли)
        bool isRpmDropping = avgShortRpmChange < -m_rpmMaxDifference; // Резкое падение оборотов

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
        Debug.Log($"CAR SPAWN!");

        if (!IsOwner)
            return;

        Debug.Log($"CAR OWN!! {OwnerClientId}");
        ClientEventBus.Instance.InvokeCarOwn(_car);

        EnableControlls();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        DisableControlls();
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
        // Подключи к своим инпут-евентам для передач, если они есть:
        // PlayerInputHandler.Instance.onShiftUp += OnShiftUp;
        // PlayerInputHandler.Instance.onShiftDown += OnShiftDown;

        _isControlling = true;
    }

    public void DisableControlls()
    {
        if (!IsOwner || !_isControlling)
            return;

        PlayerInputHandler.Instance.onGas -= OnGas;
        PlayerInputHandler.Instance.onBrake -= OnBrake;
        PlayerInputHandler.Instance.onSteer -= OnSteer;
        // PlayerInputHandler.Instance.onShiftUp -= OnShiftUp;
        // PlayerInputHandler.Instance.onShiftDown -= OnShiftDown;

        _isControlling = false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnGasRpc(float value) 
    {
        m_forwardInput = value;
        _engine.UnPressClutch();
        //_engine.OnGas(value); 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnBrakeRpc(float value)
    {
        m_backwardInput = value;

        //_engine.OnBrake(value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnSteerRpc(float value) 
    { 
        _car.OnSteer(value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ShiftUpRpc()
    { 
        _engine.NextGear(); 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ShiftDownRpc() 
    {
        _engine.PrevGear();
    }
}