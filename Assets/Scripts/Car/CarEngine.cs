using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MovingCar))]
public class CarEngine : NetworkBehaviour
{
    [Header("Engine")]
    [SerializeField] private AnimationCurve m_torqueCurve;
    [SerializeField, Min(0)] private float m_idleRPM = 800f;
    [SerializeField, Min(0)] private float m_peakRPM = 6500f;
    [SerializeField, Min(0)] private float m_maxEngineTorque = 800f;
    [SerializeField, Min(0.01f)] private float m_engineInertia = 1f;
    [SerializeField, Min(0)] private float m_engineFrictionCoeff = 0.3f;

    [Header("Transmission")]
    [SerializeField, Min(0f)] private float m_mainRatio = 4f;
    [SerializeField] private List<float> m_gears = new List<float>() { 3f, 2f, 1.4f, 1f, 0.8f };
    [SerializeField, Min(0f)] private float m_reverseGear = 3.4f;
    [SerializeField, Range(0.8f, 1f)] private float m_efficiency = 0.93f;

    [Header("Clutch")]
    [SerializeField] private float m_clutchTime = 0.4f;
    [SerializeField] private float m_maxClutchFrictionTorque = 3000f;
    [SerializeField] private float m_clutchSpringRate = 200f;
    [SerializeField] private float m_clutchDampingCoeff = 1f;

    [Header("Brakes")]
    [SerializeField, Min(0f)] private float m_maxBrakeTorque = 3000f;

    private Color m_backwardLightsColor;

    private MovingCar _car;
    private Coroutine m_clutchRoutine;

    // Локальные значения, реально участвующие в симуляции — валидны только на сервере
    private float m_gas;
    private float m_brake;
    private float m_clutch;

    private float m_engineAngularVelocity;
    private float m_transAngVel;
    private float m_filteredWheelAngVel = 0f;
    private float m_clutchAngularDeflection = 0f;

    private float m_engineTorque;
    private float m_clutchTorque;
    private float m_wheelTorque;

    private int m_currentGear = 0;

    // Синхронизируемое состояние для UI/звука/света на клиентах.
    // Пишет только сервер, читают все.
    private NetworkVariable<EngineNetworkState> m_netState = new NetworkVariable<EngineNetworkState>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public List<float> gears => m_gears;
    public float idleRPM => m_idleRPM;
    public float peakRPM => m_peakRPM;

    // Публичное API теперь всегда читает синхронизированное состояние —
    // одинаково работает и на сервере (он же его и пишет), и на клиентах.
    public float rpm => m_netState.Value.rpm;
    public int currentGear => m_netState.Value.gear;
    public float clutch => m_netState.Value.clutch;
    public bool isBraking => m_netState.Value.isBraking;

    public float maxEngineTorque => m_maxEngineTorque;
    public float maxClutchTorque => m_maxClutchFrictionTorque;
    public float engineTorque => m_engineTorque;
    public float wheelTorque => m_wheelTorque;
    public float clutchTorque => m_clutchTorque;
    public float clutchTime => m_clutchTime;
    public float gas => m_gas;

    public bool isClutchPressed => m_clutch < 0.1f && m_clutchRoutine == null;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_engineAngularVelocity = m_idleRPM * Mathf.PI / 30f;
            m_clutch = 0f;
            PushNetState();
        }
    }

    private void FixedUpdate()
    {
        // Вся симуляция двигателя/сцепления — авторитетная физика, только сервер.
        if (!IsServer) return;

        if (m_currentGear == -1)
        {
            m_clutch = 0f;
        }

        m_engineTorque = CalculateEngineTorque();

        float gearRatio = 0;
        if (m_currentGear >= 0)
        {
            gearRatio = m_gears[m_currentGear];
        }
        else if (m_currentGear == -2)
        {
            gearRatio = -m_reverseGear;
        }
        float totalRatio = m_mainRatio * gearRatio;
        float wheelAngVel = GetWheelAngularVelocity();
        m_transAngVel = wheelAngVel * totalRatio;

        float slip = m_engineAngularVelocity - m_transAngVel;

        m_clutchAngularDeflection += slip * Time.fixedDeltaTime;
        float maxDeflection = m_maxClutchFrictionTorque / Mathf.Max(m_clutchSpringRate, 0.01f);
        m_clutchAngularDeflection = Mathf.Clamp(m_clutchAngularDeflection, -maxDeflection, maxDeflection);

        float springTorque = m_clutchSpringRate * m_clutchAngularDeflection;
        float dampingTorque = m_clutchDampingCoeff * slip;
        float rawClutchTorque = (springTorque + dampingTorque) * m_clutch;
        m_clutchTorque = Mathf.Clamp(rawClutchTorque, -m_maxClutchFrictionTorque * m_clutch, m_maxClutchFrictionTorque * m_clutch);

        float loadTorque = m_clutchTorque;

        float frictionTorque = m_engineFrictionCoeff * m_engineAngularVelocity;
        float netEngineTorque = m_engineTorque - loadTorque - frictionTorque;
        m_engineAngularVelocity += netEngineTorque / m_engineInertia * Time.fixedDeltaTime;

        if (m_engineAngularVelocity < m_idleRPM * Mathf.PI / 30f)
            m_engineAngularVelocity = m_idleRPM * Mathf.PI / 30f;

        if (gearRatio != 0f)
        {
            m_wheelTorque = m_clutchTorque * totalRatio * m_efficiency;
        }
        else
            m_wheelTorque = 0f;

        ApplyWheelTorque();
        PushNetState();
    }

    private void PushNetState()
    {
        m_netState.Value = new EngineNetworkState
        {
            rpm = m_engineAngularVelocity * 30f / Mathf.PI,
            gear = m_currentGear,
            clutch = m_clutch,
            isBraking = m_brake > 0.5f
        };
    }

    private float CalculateEngineTorque()
    {
        float currentRpm = m_engineAngularVelocity * 30f / Mathf.PI;
        float torqueFactor = m_torqueCurve.Evaluate(Mathf.InverseLerp(m_idleRPM, m_peakRPM, currentRpm));
        float baseTorque = torqueFactor * m_gas * m_maxEngineTorque;

        float revLimitSoft = m_peakRPM * 0.97f;
        float revLimitHard = m_peakRPM * 1.05f;
        if (currentRpm > revLimitSoft)
        {
            float limiter = 1f - Mathf.InverseLerp(revLimitSoft, revLimitHard, currentRpm);
            baseTorque *= limiter;
        }
        if (currentRpm >= revLimitHard)
        {
            baseTorque = Mathf.Min(baseTorque, 0f);
            baseTorque -= m_engineFrictionCoeff * m_engineAngularVelocity * 2f;
        }

        return baseTorque;
    }

    // Мотор-пара = передние колёса (соответствует прежней логике MovingCar).
    // Если привод другой — поменяй список колёс здесь.
    private void ApplyWheelTorque()
    {
        float perWheelTorque = m_wheelTorque / 2f;
        _car.WheelFR.SetTorque(perWheelTorque);
        _car.WheelFL.SetTorque(perWheelTorque);
    }

    private float GetWheelAngularVelocity()
    {
        float avgRPM = (_car.WheelFR.rpm + _car.WheelFL.rpm) / 2f;
        float rawAngVel = avgRPM * 2f * Mathf.PI / 60f;

        m_filteredWheelAngVel = Mathf.Lerp(m_filteredWheelAngVel, rawAngVel, Time.fixedDeltaTime * 4f);
        return m_filteredWheelAngVel;
    }

    private IEnumerator ClutchRoutine()
    {
        m_clutch = 0;
        float timeAnchor = Time.time;
        while (Time.time - timeAnchor < m_clutchTime)
        {
            yield return null;
            m_clutch = Mathf.Lerp(0, 1, (Time.time - timeAnchor) / m_clutchTime);
        }
        m_clutch = 1;
        m_clutchRoutine = null;
    }

    public void PressClutch()
    {
        if (!IsServer) return;
        m_clutch = 0;
    }

    public void UnPressClutch()
    {
        if (!IsServer) return;

        if (m_clutchRoutine != null)
            StopCoroutine(m_clutchRoutine);

        if (m_currentGear != -1)
            m_clutchRoutine = StartCoroutine(ClutchRoutine());
    }

    public void NextGear()
    {
        if (!IsServer) return;

        if (m_currentGear < m_gears.Count - 1)
        {
            UnPressClutch();
            m_currentGear++;
        }
    }

    public void PrevGear()
    {
        if (!IsServer) return;

        if (m_currentGear > -1)
        {
            UnPressClutch();
            m_currentGear--;
        }
    }

    public void SetGear(int gear)
    {
        if (!IsServer) return;
        if (gear >= m_gears.Count || gear < -2) return;

        m_currentGear = gear;
        UnPressClutch();
    }

    // Вызывается сервером (через CarController RPC от владельца)
    public void OnGas(float value)
    {
        if (!IsServer) return;
        m_gas = value;
    }

    // Вызывается сервером; тормозит ВСЕ 4 колеса машины
    public void OnBrake(float value)
    {
        if (!IsServer) return;

        m_brake = value;
        float torque = value * m_maxBrakeTorque;

        _car.WheelFR.SetBrake(torque);
        _car.WheelFL.SetBrake(torque);
        _car.WheelBR.SetBrake(torque);
        _car.WheelBL.SetBrake(torque);
    }
}

public struct EngineNetworkState : INetworkSerializable, System.IEquatable<EngineNetworkState>
{
    public float rpm;
    public int gear;
    public float clutch;
    public bool isBraking;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref rpm);
        serializer.SerializeValue(ref gear);
        serializer.SerializeValue(ref clutch);
        serializer.SerializeValue(ref isBraking);
    }

    public bool Equals(EngineNetworkState other) =>
        rpm.Equals(other.rpm) && gear == other.gear && clutch.Equals(other.clutch) && isBraking == other.isBraking;
}