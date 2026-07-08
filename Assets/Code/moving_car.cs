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

    [Header("Настройки урона от столкновений")]
    [SerializeField] private float collisionDamageMultiplier = 0.5f;
    [SerializeField] private float minCollisionDamage = 5f;

    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem smokeEffect;        // Дым при повреждении
    [SerializeField] private ParticleSystem explosionEffect;    // Взрыв при смерти
    [SerializeField] private float smokeThreshold = 0.5f;       // Порог HP для дыма (50%)
    [SerializeField] private float smokeIntensity = 1f;         // Интенсивность дыма

    [Header("Частицы из-под колёс")]
    [SerializeField] private ParticleSystem[] wheelDustEffects; // Массив частиц для каждого колеса (4 шт)
    [SerializeField] private float minSpeedForDust = 2f;        // Мин. скорость для появления пыли (м/с)
    [SerializeField] private float dustIntensityMultiplier = 0.5f; // Множитель интенсивности пыли

    // События
    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDeath;

    private float currentSteerAngle;
    private float currentMotorForce;
    private Rigidbody rb;

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private bool isDead = false;
    private bool isSmoking = false; // Флаг: идёт ли дым

    public float MaxHealth => maxHealth;

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
        rb.angularDamping = 5f;
        currentHealth = maxHealth;

        // Выключаем эффекты при старте
        if (smokeEffect != null) smokeEffect.Stop();
        if (explosionEffect != null) explosionEffect.Stop();

        // Выключаем все частицы колёс
        if (wheelDustEffects != null)
        {
            foreach (var effect in wheelDustEffects)
            {
                if (effect != null) effect.Stop();
            }
        }
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal") * steeringSensitivity;
        verticalInput = Input.GetAxis("Vertical") * accelerationSensitivity;
        isBraking = Input.GetKey(KeyCode.Space);

        // Проверяем, нужно ли включить дым
        UpdateSmokeEffect();
    }

    // ===== УРОН ОТ СТОЛКНОВЕНИЙ =====
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

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

        // Выключаем дым и пыль
        if (smokeEffect != null && smokeEffect.isPlaying) smokeEffect.Stop();

        // Выключаем все частицы колёс
        if (wheelDustEffects != null)
        {
            foreach (var effect in wheelDustEffects)
            {
                if (effect != null && effect.isPlaying) effect.Stop();
            }
        }

        // Включаем взрыв
        if (explosionEffect != null)
        {
            explosionEffect.Play();
            Debug.Log("💥 Взрыв!");
        }
    }

    // ===== УПРАВЛЕНИЕ ДЫМОМ =====
    private void UpdateSmokeEffect()
    {
        if (isDead || smokeEffect == null) return;

        float healthPercent = currentHealth / maxHealth;

        if (healthPercent <= smokeThreshold && !isSmoking)
        {
            smokeEffect.Play();
            isSmoking = true;
            Debug.Log($"💨 Машина начала дымить! HP: {healthPercent * 100:F0}%");
        }

        if (isSmoking)
        {
            var main = smokeEffect.main;
            float intensity = (1f - healthPercent) * smokeIntensity;
            main.startSpeed = intensity * 5f;
            main.startSize = intensity * 2f;
        }

        if (healthPercent > smokeThreshold && isSmoking)
        {
            smokeEffect.Stop();
            isSmoking = false;
        }
    }

    // ===== ЧАСТИЦЫ ИЗ-ПОД КАЖДОГО КОЛЕСА =====
    private void UpdateWheelDust()
    {
        if (isDead || wheelDustEffects == null) return;

        float speed = rb.linearVelocity.magnitude;

        // Проверяем каждое колесо отдельно
        UpdateWheelDustForWheel(wheelDustEffects.Length > 0 ? wheelDustEffects[0] : null, frontLeftWheel, speed);
        UpdateWheelDustForWheel(wheelDustEffects.Length > 1 ? wheelDustEffects[1] : null, frontRightWheel, speed);
        UpdateWheelDustForWheel(wheelDustEffects.Length > 2 ? wheelDustEffects[2] : null, rearLeftWheel, speed);
        UpdateWheelDustForWheel(wheelDustEffects.Length > 3 ? wheelDustEffects[3] : null, rearRightWheel, speed);
    }

    private void UpdateWheelDustForWheel(ParticleSystem dustEffect, WheelCollider wheel, float speed)
    {
        if (dustEffect == null) return;

        // Проверяем, касается ли колесо земли
        bool isGrounded = wheel.isGrounded;

        if (speed > minSpeedForDust && isGrounded)
        {
            // Включаем пыль, если она ещё не играет
            if (!dustEffect.isPlaying)
            {
                dustEffect.Play();
            }

            // Меняем интенсивность в зависимости от скорости
            var main = dustEffect.main;
            float intensity = (speed - minSpeedForDust) * dustIntensityMultiplier;
            intensity = Mathf.Clamp(intensity, 0.1f, 3f); // Ограничиваем макс. интенсивность

            main.startSpeed = intensity * 3f;
            main.startSize = intensity * 0.5f;
            main.startLifetime = 0.5f + intensity * 0.3f;
        }
        else
        {
            // Выключаем пыль, если колесо в воздухе или машина стоит
            if (dustEffect.isPlaying)
            {
                dustEffect.Stop();
            }
        }
    }
    // ================================================

    private void FixedUpdate()
    {
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

        rearLeftWheel.motorTorque = -currentMotorForce;
        rearRightWheel.motorTorque = -currentMotorForce;

        // ===== ТОРМОЖЕНИЕ =====
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

        // ===== ОБНОВЛЯЕМ ПЫЛЬ ИЗ-ПОД ВСЕХ КОЛЁС =====
        UpdateWheelDust();
    }

    private void AddDownforce()
    {
        if (rb.linearVelocity.magnitude > 1f)
        {
            float downforceForce = 10f * rb.linearVelocity.magnitude;
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