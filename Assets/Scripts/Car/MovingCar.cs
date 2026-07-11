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

    [Header("Двигатель")]
    [SerializeField] private float motorForce = 2500f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float brakeTorque = 3000f;

    [Header("Колёса")]
    [SerializeField] private WheelCollider WheelFL;
    [SerializeField] private WheelCollider WheelFR;
    [SerializeField] private WheelCollider WheelBL;
    [SerializeField] private WheelCollider WheelBR;

    [Header("Визуализация колёс")]
    [SerializeField] private Transform frontLeftTransform;
    [SerializeField] private Transform frontRightTransform;
    [SerializeField] private Transform rearLeftTransform;
    [SerializeField] private Transform rearRightTransform;

    private Transform _transform;
    private Rigidbody _rb;
    private NetworkTransform _networkTransform;
    private NetworkRigidbody _networkRb;

    private float m_gas;
    private float m_brake;
    private float m_steering = 0f;


    private Vector3 m_projectedAirForceX;
    private Vector3 m_projectedAirForceY;
    private Vector3 m_projectedAirForceZ;

    // События
    public event UnityAction<float> OnHealthChanged;
    public event UnityAction OnDeath;

    public bool isGrounded => WheelFL.isGrounded || WheelFR.isGrounded || WheelBL.isGrounded || WheelBR.isGrounded;
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

    private void Update()
    {
        if (!IsServer) return;

        SteerWheel(WheelFR, m_steering, _turnSpeed, _steeringMaxAngle);
        SteerWheel(WheelFL, m_steering, _turnSpeed, _steeringMaxAngle);
    }

    private void SteerWheel(WheelCollider wheel, float steering, float turnSpeed, float maxAngle)
    {
        float angleT = wheel.steerAngle / maxAngle;
        float t = Mathf.Lerp(angleT, steering, Time.deltaTime * turnSpeed / maxAngle / Mathf.Abs(steering - angleT));
        wheel.steerAngle = maxAngle * t;
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

        WheelBL.motorTorque = currentMotorForce;
        WheelBR.motorTorque = currentMotorForce;

        WheelFL.brakeTorque = m_brake * brakeTorque;
        WheelFR.brakeTorque = m_brake * brakeTorque;
        WheelBL.brakeTorque = m_brake * brakeTorque;
        WheelBR.brakeTorque = m_brake * brakeTorque;

        UpdateWheelVisuals();

        RotateInAir();

        ApplyDownforce();
        ApplyLinearDamping();
    }

    private void StopCar()
    {
        WheelBL.motorTorque = 0;
        WheelBR.motorTorque = 0;
        WheelBL.brakeTorque = brakeTorque;
        WheelBR.brakeTorque = brakeTorque;
        WheelFL.brakeTorque = brakeTorque;
        WheelFR.brakeTorque = brakeTorque;
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

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheelVisual(WheelFL, frontLeftTransform);
        UpdateSingleWheelVisual(WheelFR, frontRightTransform);
        UpdateSingleWheelVisual(WheelBL, rearLeftTransform);
        UpdateSingleWheelVisual(WheelBR, rearRightTransform);
    }

    private void UpdateSingleWheelVisual(WheelCollider collider, Transform wheelVisual)
    {
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
        wheelVisual.position = position;
        wheelVisual.rotation = rotation;
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