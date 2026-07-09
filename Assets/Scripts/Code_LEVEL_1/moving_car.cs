using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingCar : MonoBehaviour
{
    [Header("Двигатель")]
    [SerializeField] private float motorForce = 2500f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float brakeForce = 3000f;

    [Header("Управление")]
    [SerializeField] private float steerAngle = 30f;
    [SerializeField] private float steerSpeed = 100f;

    [Header("Колёса")]
    [SerializeField] private WheelCollider frontLeftWheel;
    [SerializeField] private WheelCollider frontRightWheel;
    [SerializeField] private WheelCollider rearLeftWheel;
    [SerializeField] private WheelCollider rearRightWheel;

    [Header("Визуализация колёс")]
    [SerializeField] private Transform frontLeftTransform;
    [SerializeField] private Transform frontRightTransform;
    [SerializeField] private Transform rearLeftTransform;
    [SerializeField] private Transform rearRightTransform;

    private float currentSteerAngle;
    private float currentMotorForce;
    private Rigidbody rb;

    // ← Внутренние переменные для ввода (заполняются извне)
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;

    public Rigidbody Rigidbody => rb;

    private void OnDrawGizmosSelected()
    {
        var tempRb = GetComponent<Rigidbody>();
        if (tempRb != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.TransformPoint(tempRb.centerOfMass), 0.2f);
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.angularDamping = 5f;
    }

    // ===== ПУБЛИЧНЫЙ МЕТОД ДЛЯ УПРАВЛЕНИЯ (из PlayerInput или BotAI) =====
    public void SetInputs(float vertical, float horizontal, bool brake = false)
    {
        verticalInput = vertical;
        horizontalInput = horizontal;
        isBraking = brake;
    }

    private void FixedUpdate()
    {
        float localVelocity = Vector3.Dot(transform.forward, rb.linearVelocity);
        float currentSpeedKmh = Mathf.Abs(localVelocity) * 3.6f;

        // УПРАВЛЕНИЕ РУЛЁМ
        float targetSteerAngle = steerAngle * horizontalInput;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steerSpeed);

        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;

        // УПРАВЛЕНИЕ ДВИГАТЕЛЕМ
        if (currentSpeedKmh < maxSpeed || verticalInput < 0)
        {
            float reverseMultiplier = verticalInput < 0 ? 0.7f : 1f;
            currentMotorForce = motorForce * verticalInput * reverseMultiplier;
        }
        else
        {
            currentMotorForce = 0;
        }

        rearLeftWheel.motorTorque = -currentMotorForce;
        rearRightWheel.motorTorque = -currentMotorForce;

        // ТОРМОЖЕНИЕ
        float brake = 0;

        if (isBraking)
        {
            brake = brakeForce;
        }
        else if (Mathf.Abs(localVelocity) > 1f)
        {
            if (localVelocity > 0 && verticalInput > 0)
            {
                brake = brakeForce * 0.2f;
            }
            else if (localVelocity < 0 && verticalInput < 0)
            {
                brake = brakeForce * 0.2f;
            }
        }

        frontLeftWheel.brakeTorque = brake;
        frontRightWheel.brakeTorque = brake;
        rearLeftWheel.brakeTorque = brake;
        rearRightWheel.brakeTorque = brake;

        AddDownforce();
        UpdateWheelVisuals();
    }

    private void AddDownforce()
    {
        if (rb.linearVelocity.magnitude > 1f)
        {
            float downforceForce = 3f * rb.linearVelocity.magnitude;
            rb.AddForce(-transform.up * downforceForce, ForceMode.Force);
        }
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheelVisual(frontLeftWheel, frontLeftTransform, 0);
        UpdateSingleWheelVisual(frontRightWheel, frontRightTransform, -0);
        UpdateSingleWheelVisual(rearLeftWheel, rearLeftTransform, 0);
        UpdateSingleWheelVisual(rearRightWheel, rearRightTransform, -0);
    }

    private void UpdateSingleWheelVisual(WheelCollider collider, Transform wheelVisual, float rot)
    {
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
        wheelVisual.position = position;
        wheelVisual.rotation = rotation * Quaternion.Euler(0, rot, 0);
    }
}