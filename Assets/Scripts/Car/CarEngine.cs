using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour
{
    /*[Header("Engine")]
    [SerializeField] private AnimationCurve m_torqueCurve;
    [SerializeField, Min(0)] private float m_idleRPM = 800f;
    [SerializeField, Min(0)] private float m_peakRPM = 6500f;
    [SerializeField, Min(0)] private float m_maxEngineTorque = 800f;
    [SerializeField, Min(0.01f)] private float m_engineInertia = 1f;
    [SerializeField, Min(0)] private float m_engineFrictionCoeff = 0.3f; // подбирается под ощущения
    [Header("Transmission")]
    [SerializeField, Min(0f)] private float m_mainRatio = 4f;
    [SerializeField] private List<float> m_gears = new List<float>() { 3f, 2f, 1.4f, 1f, 0.8f };
    [SerializeField, Min(0f)] private float m_reverseGear = 3.4f;
    [SerializeField, Range(0.8f, 1f)] private float m_efficiency = 0.93f;

    [Header("Clutch")]
    [SerializeField] private float m_clutchTime = 0.4f;
    [SerializeField] private float m_maxClutchFrictionTorque = 3000f;
    [SerializeField] private float m_clutchSpringRate = 200f;   // коэффициент демпфирования (чем больше, тем быстрее схватывает)
    [SerializeField] private float m_clutchDampingCoeff = 1f;   // коэффициент демпфирования (чем больше, тем быстрее схватывает)

    [Header("Materials")]
    [SerializeField] private int m_backwardLightsIndex;
    [SerializeField, Min(1f)] private float m_brightnessCoef = 1.3f;

    [SerializeField] private MeshRenderer m_renderer;
    private Color m_backwardLightsColor;

    private Moving m_car;
    private Rigidbody m_rb;
    private Coroutine m_clutchRoutine;

    private float m_gas;
    private float m_brake;
    private float m_clutch;

    private float m_rpm;

    private float m_engineAngularVelocity;
    private float m_transAngVel;
    private float m_filteredWheelAngVel = 0f;
    private float m_clutchAngularDeflection = 0f;

    private float m_engineTorque;
    private float m_clutchTorque;
    private float m_wheelTorque;

    private int m_currentGear = 0;

    public List<float> gears => m_gears;
    public float idleRPM => m_idleRPM;
    public float peakRPM => m_peakRPM;
    public float rpm => m_engineAngularVelocity * 30f / Mathf.PI;
    public float transRPM => m_transAngVel * 60f / (2f * Mathf.PI);
    public float maxEngineTorque => m_maxEngineTorque;
    public float maxClutchTorque => m_maxClutchFrictionTorque;
    public float engineTorque => m_engineTorque;
    public float wheelTorque => m_wheelTorque;
    public float clutchTorque => m_clutchTorque;
    public float clutchTime => m_clutchTime;
    public int currentGear => m_currentGear;
    public float gas => m_gas;
    public float clutch => m_clutch;

    public bool isClutchPressed => m_clutch < 0.1f && m_clutchRoutine == null;

    private void Awake()
    {
        m_car = GetComponent<Car>();
        m_rb = GetComponent<Rigidbody>();
        if (m_renderer == null)
            m_renderer = GetComponent<MeshRenderer>();
        m_renderer.materials[m_backwardLightsIndex].EnableKeyword("_EMISSION");
        m_backwardLightsColor = m_renderer.materials[m_backwardLightsIndex].GetColor("_EmissionColor");
    }

    private void Start()
    {
        m_engineAngularVelocity = m_idleRPM * Mathf.PI / 30f;
        m_clutch = 0f;
    }

    private void FixedUpdate()
    {
        if (m_currentGear == -1) 
        {
            m_clutch = 0f;
        }

        m_engineTorque = CalculateEngineTorque();

        // 2. Сцепление: чисто демпферное
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
        float wheelAngVel = GetWheelAngularVelocity(); // Средняя скорость ведущих колес
        m_transAngVel = wheelAngVel * totalRatio;

        float slip = m_engineAngularVelocity - m_transAngVel;

        // Угловое смещение (интеграл от slip)
        m_clutchAngularDeflection += slip * Time.fixedDeltaTime;
        float maxDeflection = m_maxClutchFrictionTorque / Mathf.Max(m_clutchSpringRate, 0.01f);
        m_clutchAngularDeflection = Mathf.Clamp(m_clutchAngularDeflection, -maxDeflection, maxDeflection);

        // Момент через пружину + демпфер
        float springTorque = m_clutchSpringRate * m_clutchAngularDeflection;
        float dampingTorque = m_clutchDampingCoeff * slip; // отдельный коэффициент демпфирования, в 5-10 раз меньше springRate
        float rawClutchTorque = (springTorque + dampingTorque) * m_clutch;
        m_clutchTorque = Mathf.Clamp(rawClutchTorque, -m_maxClutchFrictionTorque * m_clutch, m_maxClutchFrictionTorque * m_clutch);

        float loadTorque = m_clutchTorque;
        if (Mathf.Abs(slip) < 15f)
        {
            foreach (var wheelPair in m_car.wheelPairs)
            {
                if (wheelPair.isMotorPair)
                {
                    //loadTorque += wheelPair.GetResistanceForce();
                }
            }
            loadTorque += m_car.physics.projectedAirForceZ.magnitude * m_car.wheelPairs[0].rightWheel.radius;
        }

        // 3. Интегрирование скорости двигателя
        float frictionTorque = m_engineFrictionCoeff * m_engineAngularVelocity;
        float netEngineTorque = m_engineTorque - loadTorque - frictionTorque;
        m_engineAngularVelocity += netEngineTorque / m_engineInertia * Time.fixedDeltaTime;

        if (m_engineAngularVelocity < m_idleRPM * Mathf.PI / 30f)
            m_engineAngularVelocity = m_idleRPM * Mathf.PI / 30f;

        // 5. Передача момента на колёса (если не нейтраль)
        if (gearRatio != 0f)
            m_wheelTorque = m_clutchTorque * totalRatio * m_efficiency;
        else
            m_wheelTorque = 0f;
        ApplyWheelTorque();
    }

    private float CalculateEngineTorque()
    {
        float torqueFactor = m_torqueCurve.Evaluate(Mathf.InverseLerp(m_idleRPM, m_peakRPM, rpm));
        float baseTorque = torqueFactor * m_gas * m_maxEngineTorque;

        // Ограничитель оборотов (мягкое удушение выше пика)
        float revLimitSoft = m_peakRPM * 0.97f;
        float revLimitHard = m_peakRPM * 1.05f;
        if (rpm > revLimitSoft)
        {
            float limiter = 1f - Mathf.InverseLerp(revLimitSoft, revLimitHard, rpm);
            baseTorque *= limiter;
        }
        if (rpm >= revLimitHard)
        {
            baseTorque = Mathf.Min(baseTorque, 0f);
            baseTorque -= m_engineFrictionCoeff * m_engineAngularVelocity * 2f;
        }

        return baseTorque;
    }

    private void ApplyWheelTorque()
    {
        int motorPairsCount = 0;
        foreach (var pair in m_car.wheelPairs)
            if (pair.isMotorPair) motorPairsCount++;

        foreach (var pair in m_car.wheelPairs)
            if (pair.isMotorPair) pair.SetTorque(m_wheelTorque / Mathf.Max(1, motorPairsCount));
    }

    private float GetWheelAngularVelocity()
    {
        float totalRPM = 0f;
        int count = 0;
        foreach (var wheelPair in m_car.wheelPairs)
            if (wheelPair.isMotorPair)
            {
                totalRPM += wheelPair.rightWheel.rpm;
                totalRPM += wheelPair.leftWheel.rpm;
                count += 2;
            }
        if (count == 0) return 0;

        float avgRPM = totalRPM / count;
        float rawAngVel = avgRPM * 2f * Mathf.PI / 60f;

        // Фильтр
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
        m_clutch = 0;
    }

    public void UnPressClutch()
    {
        if (m_clutchRoutine != null)
        {
            StopCoroutine(m_clutchRoutine);
        }
        if (m_currentGear != -1) 
        {
            m_clutchRoutine = StartCoroutine(ClutchRoutine());
        }
    }

    public void NextGear()
    {
        if (m_currentGear < m_gears.Count - 1)
        {
            UnPressClutch();
            m_currentGear++;
        }
    }

    public void PrevGear()
    {
        if (m_currentGear > -1)
        {
            UnPressClutch();
            m_currentGear--;
        }
    }

    public void SetGear(int gear)
    {
        if (gear >= m_gears.Count || gear < -2)
        {
            return;
        }
        m_currentGear = gear;
        UnPressClutch();
    }

    public void OnGas(float value)
    {
        m_gas = value;
    }

    public void OnBrake(float value)
    {
        foreach (var wheelPair in m_car.wheelPairs)
        {
            wheelPair.Brake(value);
        }

        if (value > 0.5f)
        {
            m_renderer.materials[m_backwardLightsIndex].SetColor("_EmissionColor", m_backwardLightsColor * m_brightnessCoef);
        }
        else
        {
            m_renderer.materials[m_backwardLightsIndex].SetColor("_EmissionColor", m_backwardLightsColor);
        }
    }*/
}