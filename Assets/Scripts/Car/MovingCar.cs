using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CarEffector))]
[RequireComponent(typeof(CarPhysics))]
[RequireComponent(typeof(CarEngine))]
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

    [Header("Torque (для StopCar / аварийной остановки)")]
    [SerializeField] private float brakeTorque = 3000f;

    // Нужны CarEngine для доступа к колёсам
    public CarWheel WheelFR => _wheelFR;
    public CarWheel WheelFL => _wheelFL;
    public CarWheel WheelBR => _wheelBR;
    public CarWheel WheelBL => _wheelBL;



    private Transform _transform;
    private Rigidbody _rb;
    private NetworkTransform _networkTransform;
    private NetworkRigidbody _networkRb;

    private float m_steering = 0f;

    private NetworkVariable<WheelVisualState> m_netWheelState = new NetworkVariable<WheelVisualState>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector3 m_projectedAirForceX;
    private Vector3 m_projectedAirForceY;
    private Vector3 m_projectedAirForceZ;

    private Vector3 m_projectedVelocityZ;

    public event UnityAction<float> OnHealthChanged;
    public event UnityAction OnDeath;

    public bool isGrounded => _wheelFR.IsGrounded || _wheelFL.IsGrounded || _wheelBR.IsGrounded || _wheelBL.IsGrounded;
    public Vector3 position => _rb.position;
    public Vector3 linearVelocity => _rb.linearVelocity;
    public Vector3 projectedVelocityZ => Vector3.Project(_rb.linearVelocity, _transform.forward);

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
        if (IsServer)
        {
            if (_networkTransform != null)
            {
                // Принудительно синхронизируем позицию с текущим трансформом
            }
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            _wheelFR.SteerWheel(m_steering, _turnSpeed, _steeringMaxAngle);
            _wheelFL.SteerWheel(m_steering, _turnSpeed, _steeringMaxAngle);
        }

        WheelVisualState state = IsServer
            ? new WheelVisualState
            {
                frCompression = _wheelFR.GetSuspensionCompression(),
                flCompression = _wheelFL.GetSuspensionCompression(),
                brCompression = _wheelBR.GetSuspensionCompression(),
                blCompression = _wheelBL.GetSuspensionCompression(),

                frAngularVelocity = _wheelFR.angularVelocity,
                flAngularVelocity = _wheelFL.angularVelocity,
                brAngularVelocity = _wheelBR.angularVelocity,
                blAngularVelocity = _wheelBL.angularVelocity,

                steerAngle = _wheelFR.CurrentSteerAngle,
                forwardSpeed = Vector3.Dot(transform.forward, _rb.linearVelocity)
            }
            : m_netWheelState.Value;

        _wheelFR.ApplyVisual(state.frCompression, state.steerAngle, state.frAngularVelocity);
        _wheelFL.ApplyVisual(state.flCompression, state.steerAngle, state.flAngularVelocity);
        _wheelBR.ApplyVisual(state.brCompression, 0f, state.brAngularVelocity);
        _wheelBL.ApplyVisual(state.blCompression, 0f, state.blAngularVelocity);

        if (IsServer)
        {
            m_netWheelState.Value = state;
        }
    }

    private void FixedUpdate()
    {
        // Мотор и тормоза теперь считает CarEngine.
        // Здесь остаётся только руление, аэродинамика и демпфирование в воздухе.
        if (!IsServer) return;

        RotateInAir();
        ApplyDownforce();
        ApplyLinearDamping();
    }

    private void RotateInAir()
    {
        if (isGrounded) return;

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
        if (!isGrounded) return;

        float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        if (forwardSpeed < 0) forwardSpeed = 0;

        float downforce = _downForceCoef * forwardSpeed * forwardSpeed;
        downforce = Mathf.Min(downforce, _downForceCoef * _downForceCoef * _downForceCoef);

        _rb.AddForce(-_transform.up * downforce, ForceMode.Force);
    }

    public void StopCar()
    {
        if (!IsServer) return;

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

    public void OnSteer(float direction) => m_steering = direction;
}

public struct WheelVisualState : INetworkSerializable
{
    public float frCompression;
    public float flCompression;
    public float brCompression;
    public float blCompression;

    public float frAngularVelocity;
    public float flAngularVelocity;
    public float brAngularVelocity;
    public float blAngularVelocity;

    public WheelGroundSurfaceType frGround;
    public WheelGroundSurfaceType flGround;
    public WheelGroundSurfaceType brGround;
    public WheelGroundSurfaceType blGround;

    public float steerAngle;
    public float forwardSpeed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref frCompression);
        serializer.SerializeValue(ref flCompression);
        serializer.SerializeValue(ref brCompression);
        serializer.SerializeValue(ref blCompression);

        serializer.SerializeValue(ref frAngularVelocity);
        serializer.SerializeValue(ref flAngularVelocity);
        serializer.SerializeValue(ref brAngularVelocity);
        serializer.SerializeValue(ref blAngularVelocity);

        serializer.SerializeValue(ref frGround);
        serializer.SerializeValue(ref flGround);
        serializer.SerializeValue(ref brGround);
        serializer.SerializeValue(ref blGround);

        serializer.SerializeValue(ref steerAngle);
        serializer.SerializeValue(ref forwardSpeed);
    }
}

public enum WheelGroundSurfaceType
{
    None,
    Terrain,
    Road,
    Metal
}