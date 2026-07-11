using UnityEngine;

public class DriverRagdoll : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private ConfigurableJoint mainJoint;
    [SerializeField] private CharacterJoint[] characterJoints;
    [SerializeField] private Rigidbody[] ragdollParts;

    [Header("Настройки вылета")]
    [SerializeField] private bool ejectOnDeath = true;
    [SerializeField] private float ejectForce = 500f;
    [SerializeField] private float ejectUpForce = 300f;
    [SerializeField] private float randomTorque = 50f;

    [Header("Задержки")]
    [SerializeField] private float activateDelay = 0.05f;

    [Header("Ссылки на системы")]
    [SerializeField] private VehicleHealth vehicleHealth;  // ← НОВОЕ

    private Rigidbody carRootRigidbody;  // ← Переименовал для ясности
    private Transform carTransform;
    private Rigidbody rootRigidbody;
    private bool isRagdollActive = false;

    private void Start()
    {
        rootRigidbody = GetComponent<Rigidbody>();
        if (rootRigidbody == null)
        {
            //Debug.LogError("❌ DriverRagdoll: Rigidbody не найден!");
            return;
        }

        // Ищем VehicleHealth (вместо MovingCar)
        if (vehicleHealth == null)
        {
            vehicleHealth = GetComponentInParent<VehicleHealth>();
        }

        if (vehicleHealth == null)
        {
            //Debug.LogError("❌ DriverRagdoll: VehicleHealth не найден!");
            return;
        }

        // Находим корневой Rigidbody машины (для скорости и направления)
        carRootRigidbody = vehicleHealth.GetComponent<Rigidbody>();
        carTransform = vehicleHealth.transform;

        if (mainJoint == null)
        {
            mainJoint = GetComponent<ConfigurableJoint>();
        }

        // Автоматически находим части тела
        if (ragdollParts == null || ragdollParts.Length == 0)
        {
            ragdollParts = GetComponentsInChildren<Rigidbody>();
        }

        // Автоматически находим Character Joint
        if (characterJoints == null || characterJoints.Length == 0)
        {
            characterJoints = GetComponentsInChildren<CharacterJoint>();
            //Debug.Log($"✅ Найдено {characterJoints.Length} Character Joint");
        }

        // Усыпляем ragdoll
        SleepRagdoll();

        // Подписываемся на событие смерти
        vehicleHealth.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (vehicleHealth != null)
        {
            vehicleHealth.OnDeath -= HandleDeath;
        }
    }

    private void SleepRagdoll()
    {
        foreach (var rb in ragdollParts)
        {
            if (rb == null) continue;
            rb.Sleep();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        isRagdollActive = false;
    }

    private void EnableRagdoll(Vector3 ejectDirection)
    {
        if (isRagdollActive) return;
        isRagdollActive = true;

        Debug.Log("👤 Активация ragdoll...");

        // 1. Ломаем главный Configurable Joint
        if (mainJoint != null)
        {
            Destroy(mainJoint);
        }

        // 2. Включаем все Character Joint
        foreach (var joint in characterJoints)
        {
            if (joint != null)
            {
                joint.enableProjection = true;
                joint.projectionDistance = 0.1f;
                joint.projectionAngle = 10f;
            }
        }

        // 3. Отсоединяем от машины
        transform.SetParent(null);

        // 4. "Будим" все части тела
        foreach (var rb in ragdollParts)
        {
            if (rb == null) continue;

            rb.WakeUp();
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Наследуем скорость машины
            if (carRootRigidbody != null)
            {
                rb.linearVelocity = carRootRigidbody.linearVelocity;
                rb.angularVelocity = carRootRigidbody.angularVelocity * 0.5f;
            }
        }

        // 5. Применяем силу вылета
        if (ejectOnDeath)
        {
            ApplyEjectForce(ejectDirection);
        }

        Debug.Log("✅ Ragdoll активирован!");
    }

    private void ApplyEjectForce(Vector3 direction)
    {
        if (rootRigidbody != null)
        {
            rootRigidbody.AddForce(direction * ejectForce, ForceMode.Impulse);
            rootRigidbody.AddForce(Vector3.up * ejectUpForce, ForceMode.Impulse);
            rootRigidbody.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
        }

        foreach (var rb in ragdollParts)
        {
            if (rb == null || rb == rootRigidbody) continue;

            Vector3 partDirection = direction + Random.insideUnitSphere * 0.3f;
            rb.AddForce(partDirection * ejectForce * 0.3f, ForceMode.Impulse);
            rb.AddForce(Vector3.up * ejectUpForce * 0.5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomTorque * 0.5f, ForceMode.Impulse);
        }
    }

    private void HandleDeath()
    {
        Invoke(nameof(ActivateRagdoll), activateDelay);
    }

    private void ActivateRagdoll()
    {
        Vector3 ejectDirection = Vector3.forward;

        if (carRootRigidbody != null && carRootRigidbody.linearVelocity.magnitude > 1f)
        {
            ejectDirection = carRootRigidbody.linearVelocity.normalized;
        }
        else if (carTransform != null)
        {
            ejectDirection = carTransform.forward;
        }

        EnableRagdoll(ejectDirection);
    }

    public void ForceActivateRagdoll()
    {
        ActivateRagdoll();
    }
}