using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CarEffector))]
[RequireComponent(typeof(CarPhysics))]
public class MovingCar : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float _downForceCoef = 3f;
    [Header("Steering Settings")]
    [SerializeField, Min(0f)] private float _steeringMaxAngle = 40f;
    [SerializeField, Min(0f)] private float _turnSpeed = 300f;
    [SerializeField, Range(0f, 2f)] private float _outerWheelCoef = 0.8f;
    [SerializeField, Range(0f, 10f)] private float _safeSteerCoef = 1f;
    [Header("Air")]
    [SerializeField] private Vector3 m_projectedLinearDamping = Vector3.zero;
    [Header("Air controls")]
    [SerializeField, Min(0f)] private float m_maxAirAngularVelocity = 300f;

    [Header("Wheels")]
    [SerializeField] private CarWheel _wheelFR;
    [SerializeField] private CarWheel _wheelFL;
    [SerializeField] private CarWheel _wheelBR;
    [SerializeField] private CarWheel _wheelBL;

    [Header("Двигатель")]
    [SerializeField] private float motorForce = 2500f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float brakeTorque = 3000f;

    private Transform _transform;
    private Rigidbody _rb;
    private NetworkTransform _networkTransform;
    private NetworkRigidbody _networkRb;

    // Локальные (только сервер) значения инпута
    private float m_gas;
    private float m_brake;
    private float m_steering = 0f;

    // Реплицируемые значения — сервер пишет, все читают для визуала
    private NetworkVariable<WheelVisualState> m_netWheelState = new NetworkVariable<WheelVisualState>(
        new WheelVisualState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Vector3 m_projectedAirForceX;
    private Vector3 m_projectedAirForceY;
    private Vector3 m_projectedAirForceZ;

    public bool isGrounded => _wheelFR.IsGrounded || _wheelFL.IsGrounded || _wheelBR.IsGrounded || _wheelBL.IsGrounded;
    public Vector3 position => _rb.position;
    public Vector3 linearVelocity => _rb.linearVelocity;

    private void OnDrawGizmosSelected()
    {
        var tempRb = GetComponent<Rigidbody>();
        if (tempRb != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.TransformPoint(tempRb.centerOfMass), 0.2f);
        }
    }

    private void Awake()
    {
        _transform = transform;
        _rb = GetComponent<Rigidbody>();

        _networkTransform = GetComponent<NetworkTransform>();
        _networkRb = GetComponent<NetworkRigidbody>();

        StopCar();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) // только сервер задаёт позицию
        {
            if (_networkTransform != null)
            {
                // Принудительно синхронизируем позицию с текущим трансформом
            }
        }
    }

/*    private void Update()
    {
        if (!IsServer) return;

        _wheelFR.SteerWheel(m_steering, _turnSpeed, _steeringMaxAngle);
        _wheelFL.SteerWheel(m_steering, _turnSpeed, _steeringMaxAngle);
    }
*/
    private void Update()
    {
        // Реальная физика руления (влияет на трение шин) — только сервер
        if (IsServer)
        {
            _wheelFR.SteerWheel(m_steering, _turnSpeed, _steeringMaxAngle);
            _wheelFL.SteerWheel(m_steering, _turnSpeed, _steeringMaxAngle);
        }

        // Визуал колёс — у ВСЕХ, но данные берутся из разных источников
        WheelVisualState state = IsServer
            ? new WheelVisualState
            {
                frCompression = _wheelFR.GetSuspensionCompression(),
                flCompression = _wheelFL.GetSuspensionCompression(),
                brCompression = _wheelBR.GetSuspensionCompression(),
                blCompression = _wheelBL.GetSuspensionCompression(),
                steerAngle = _wheelFR.CurrentSteerAngle,
                forwardSpeed = Vector3.Dot(transform.forward, _rb.linearVelocity)
            }
            : m_netWheelState.Value;

        _wheelFR.ApplyVisual(state.frCompression, state.steerAngle, state.forwardSpeed);
        _wheelFL.ApplyVisual(state.flCompression, state.steerAngle, state.forwardSpeed);
        _wheelBR.ApplyVisual(state.brCompression, 0f, state.forwardSpeed);
        _wheelBL.ApplyVisual(state.blCompression, 0f, state.forwardSpeed);

        if (IsServer)
        {
            m_netWheelState.Value = state;
        }
    }


    private void FixedUpdate()
    {
        if (!IsServer) return;

        float localVelocity = Vector3.Dot(transform.forward, _rb.linearVelocity);
        float currentSpeedKmh = Mathf.Abs(localVelocity) * 3.6f;

        float currentMotorForce;
        // ===== УПРАВЛЕНИЕ ДВИГАТЕЛЕМ =====
        if (currentSpeedKmh < maxSpeed || m_gas < 0)
        {
            float reverseMultiplier = m_gas < 0 ? 0.7f : 1f;
            currentMotorForce = motorForce * m_gas * reverseMultiplier;
        }
        else
        {
            currentMotorForce = 0;
        }

        _wheelFR.SetTorque(currentMotorForce);
        _wheelFL.SetTorque(currentMotorForce);

        _wheelFR.SetBrake(m_brake * brakeTorque);
        _wheelFL.SetBrake(m_brake * brakeTorque);
        _wheelBR.SetBrake(m_brake * brakeTorque);
        _wheelBL.SetBrake(m_brake * brakeTorque);

        RotateInAir();

        ApplyDownforce();
        ApplyLinearDamping();
    }

    private void RotateInAir()
    {
        if (isGrounded)
        {
            return;
        }
        Vector3 targetAngularVelocity = new Vector3(0, 0, -m_steering) * m_maxAirAngularVelocity;
        Vector3 angularVelocityIncrease = (_transform.TransformDirection(targetAngularVelocity * Mathf.Deg2Rad) - _rb.angularVelocity) * Time.fixedDeltaTime;
        _rb.angularVelocity += angularVelocityIncrease;
    }

    private void ApplyLinearDamping()
    {
        const float air = 0.6125f;

        Vector3 projVelForward = -Vector3.Project(_rb.linearVelocity, _transform.forward);
        Vector3 projVelUp = -Vector3.Project(_rb.linearVelocity, _transform.up);
        Vector3 projVelRight = -Vector3.Project(_rb.linearVelocity, _transform.right);

        m_projectedAirForceX = m_projectedLinearDamping.x * air * projVelRight * projVelRight.magnitude;
        m_projectedAirForceY = m_projectedLinearDamping.y * air * projVelUp * projVelUp.magnitude;
        m_projectedAirForceZ = m_projectedLinearDamping.z * air * projVelForward * projVelForward.magnitude;

        _rb.AddForce(m_projectedAirForceX);
        _rb.AddForce(m_projectedAirForceY);
        _rb.AddForce(m_projectedAirForceZ);
    }

    private void ApplyDownforce()
    {
        if (!isGrounded)
        {
            return;
        }

        float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        if (forwardSpeed < 0) forwardSpeed = 0;

        float downforce = _downForceCoef * forwardSpeed * forwardSpeed;
        downforce = Mathf.Min(downforce, _downForceCoef * _downForceCoef * _downForceCoef);

        _rb.AddForce(-_transform.up * downforce, ForceMode.Force);
    }

    public void StopCar()
    {
        if (!IsServer)
            return;

        _wheelFR.SetTorque(0);
        _wheelFL.SetTorque(0);

        _wheelFR.SetBrake(brakeTorque);
        _wheelFL.SetBrake(brakeTorque);
        _wheelBR.SetBrake(brakeTorque);
        _wheelBL.SetBrake(brakeTorque);
    }

    public void SetSpawnPosition(Vector3 pos, Quaternion rot)
    {
        _networkTransform.Teleport(pos, rot, _transform.localScale);
        _networkRb.ApplyCurrentTransform();
    }

    // Ввод
    public void OnSteer(float direction)
    {
        m_steering = direction;
    }

    public void OnGas(float value)
    {
        m_gas = value;
    }

    public void OnBrake(float value)
    {
        m_brake = value;
    }
}