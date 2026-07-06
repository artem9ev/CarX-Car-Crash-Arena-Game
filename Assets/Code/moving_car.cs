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

    [Header("Настройки управления")]
    [SerializeField] private float accelerationSensitivity = 1f;
    [SerializeField] private float steeringSensitivity = 1f;

    [Header("Здоровье")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    public float MaxHealth => maxHealth;
    [Header("Настройки урона от столкновений")]
    [SerializeField] private float collisionDamageMultiplier = 0.05f;
    [SerializeField] private float minCollisionDamage = 5f;

    // События для UI и других систем
    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDeath;

    private float currentSteerAngle;
    private float currentMotorForce;
    private Rigidbody rb;

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private bool isDead = false;

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
        rb.centerOfMass = new Vector3(0, -1.5f, 0);
        rb.angularDamping = 5f; // Исправлено: angularDamping → angularDrag

        currentHealth = maxHealth;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal") * steeringSensitivity;
        verticalInput = Input.GetAxis("Vertical") * accelerationSensitivity;
        isBraking = Input.GetKey(KeyCode.Space);
    }

    // ===== НОВАЯ МЕХАНИКА: УРОН ОТ СТОЛКНОВЕНИЙ =====
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        // Сила удара = относительная скорость * масса
        float impactForce = collision.relativeVelocity.magnitude * rb.mass;

        if (impactForce < minCollisionDamage) return;

        float damage = impactForce * collisionDamageMultiplier;
        TakeDamage(damage);
    }

    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"💥 Урон: {damage:F1} | HP: {currentHealth:F1}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("☠️ Машина уничтожена!");
        OnDeath?.Invoke();

        // Отключаем мотор и тормоза
        rearLeftWheel.motorTorque = 0;
        rearRightWheel.motorTorque = 0;
        rearLeftWheel.brakeTorque = brakeForce;
        rearRightWheel.brakeTorque = brakeForce;
        frontLeftWheel.brakeTorque = brakeForce;
        frontRightWheel.brakeTorque = brakeForce;
    }
    // ================================================

    private void FixedUpdate()
    {
        // Если машина мертва — не управляем ей
        if (isDead) return;

        float localVelocity = Vector3.Dot(transform.forward, rb.linearVelocity);
        float currentSpeedKmh = Mathf.Abs(localVelocity) * 3.6f;

        // ===== УПРАВЛЕНИЕ РУЛЁМ =====
        float targetSteerAngle = steerAngle * horizontalInput;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steerSpeed);

        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;

        // ===== УПРАВЛЕНИЕ ДВИГАТЕЛЕМ =====
        if (currentSpeedKmh < maxSpeed || verticalInput < 0)
        {
            float reverseMultiplier = verticalInput < 0 ? 0.7f : 1f;
            currentMotorForce = motorForce * verticalInput * reverseMultiplier;
        }
        else
        {
            currentMotorForce = 0;
        }

        // Твои минусы сохранены!
        rearLeftWheel.motorTorque = -currentMotorForce;
        rearRightWheel.motorTorque = -currentMotorForce;

        // ===== ТОРМОЖЕНИЕ =====
        float brake = 0;

        if (isBraking)
        {
            brake = brakeForce;
        }
        // Твоя инвертированная логика сохранена!
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
            float downforceForce = 10f * rb.linearVelocity.magnitude;
            rb.AddForce(-transform.up * downforceForce, ForceMode.Force);
        }
    }

    // ===== НЕ ТРОГАТЬ ЭТУ ЧАСТЬ КОДА! =====
    private void UpdateWheelVisuals()
    {
        UpdateSingleWheelVisual(frontLeftWheel, frontLeftTransform, 90);
        UpdateSingleWheelVisual(frontRightWheel, frontRightTransform, -90);
        UpdateSingleWheelVisual(rearLeftWheel, rearLeftTransform, 90);
        UpdateSingleWheelVisual(rearRightWheel, rearRightTransform, -90);
    }

    private void UpdateSingleWheelVisual(WheelCollider collider, Transform wheelVisual, float rot)
    {
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
        wheelVisual.position = position;
        wheelVisual.rotation = rotation * Quaternion.Euler(0, rot, 0);
    }
    // ===== КОНЕЦ НЕТРОГАЕМОЙ ЧАСТИ =====
}