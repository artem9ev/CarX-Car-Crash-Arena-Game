using UnityEngine;

public class CarDestruction : MonoBehaviour
{
    [Header("Части машины")]
    [SerializeField] private Rigidbody[] carParts;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 5f;

    [Header("Водитель")]
    [SerializeField] private GameObject driver;
    [SerializeField] private float driverEjectForce = 1000f;
    [SerializeField] private float driverEjectHeight = 5f;
    [SerializeField] private Transform ejectDirection;

    [Header("Настройки ragdoll")]
    [SerializeField] private float ragdollActivationDelay = 0.1f;

    [Header("Ссылки на системы")]
    [SerializeField] private VehicleHealth vehicleHealth;  // ← НОВОЕ ПОЛЕ
    [SerializeField] private DriverRagdoll driverRagdoll;  // ← НОВОЕ ПОЛЕ (опционально)

    private Rigidbody carRootRb;
    private bool isDestroyed = false;

    private void Start()
    {
        carRootRb = GetComponent<Rigidbody>();

        // Ищем VehicleHealth (вместо MovingCar)
        if (vehicleHealth == null)
        {
            vehicleHealth = GetComponent<VehicleHealth>();
        }

        if (vehicleHealth == null)
        {
            Debug.LogError("❌ CarDestruction: VehicleHealth не найден!");
            return;
        }

        // Подписываемся на событие смерти
        vehicleHealth.OnDeath += HandleDeath;

        // Если части не назначены - находим автоматически
        if (carParts == null || carParts.Length == 0)
        {
            FindCarParts();
        }

        // Выключаем физику частей при старте
        DisablePartsPhysics();
    }

    private void OnDestroy()
    {
        if (vehicleHealth != null)
        {
            vehicleHealth.OnDeath -= HandleDeath;
        }
    }

    // ===== АВТОМАТИЧЕСКИЙ ПОИСК ЧАСТЕЙ =====
    private void FindCarParts()
    {
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>();
        carParts = new Rigidbody[allRigidbodies.Length - 1];

        int index = 0;
        foreach (var rb in allRigidbodies)
        {
            if (rb != carRootRb)
            {
                carParts[index] = rb;
                index++;
            }
        }

        Debug.Log($"✅ Найдено {carParts.Length} частей машины");
    }

    // ===== ОТКЛЮЧЕНИЕ ФИЗИКИ ЧАСТЕЙ ПРИ СТАРТЕ =====
    private void DisablePartsPhysics()
    {
        if (carParts == null) return;

        foreach (var part in carParts)
        {
            if (part != null)
            {
                part.isKinematic = true;
                part.useGravity = false;
            }
        }

        if (driver != null)
        {
            Rigidbody[] driverRbs = driver.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in driverRbs)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    // ===== ОБРАБОТКА СМЕРТИ =====
    private void HandleDeath()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log("💥 Машина разрушается!");

        // Включаем физику всех частей
        EnablePartsPhysics();

        // Применяем силу взрыва к частям
        ApplyExplosionForce();

        // Активируем ragdoll водителя (если есть скрипт)
        if (driverRagdoll != null)
        {
            driverRagdoll.ForceActivateRagdoll();
        }
        else if (driver != null)
        {
            // Если скрипта нет - старый способ вылета
            Invoke(nameof(EjectDriver), ragdollActivationDelay);
        }

        // Отключаем управление машиной
        DisableCarControls();
    }

    // ===== ВКЛЮЧЕНИЕ ФИЗИКИ ЧАСТЕЙ =====
    private void EnablePartsPhysics()
    {
        if (carParts == null) return;

        foreach (var part in carParts)
        {
            if (part != null)
            {
                part.isKinematic = false;
                part.useGravity = true;
                part.AddExplosionForce(explosionForce * 0.5f, transform.position, explosionRadius);
            }
        }
    }

    // ===== ПРИМЕНЕНИЕ СИЛЫ ВЗРЫВА =====
    private void ApplyExplosionForce()
    {
        if (carParts == null) return;

        Vector3 explosionCenter = transform.position;

        foreach (var part in carParts)
        {
            if (part != null)
            {
                Vector3 direction = part.position - explosionCenter;
                float distance = direction.magnitude;
                float forceMultiplier = Mathf.Clamp01(1f - distance / explosionRadius);

                part.AddExplosionForce(
                    explosionForce * forceMultiplier,
                    explosionCenter,
                    explosionRadius
                );

                part.AddTorque(
                    Random.insideUnitSphere * 100f * forceMultiplier
                );
            }
        }
    }

    // ===== ВЫЛЕТ ВОДИТЕЛЯ (старый способ, если нет DriverRagdoll) =====
    private void EjectDriver()
    {
        if (driver == null) return;

        Debug.Log("👤 Водитель вылетает!");

        Rigidbody[] driverRbs = driver.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in driverRbs)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Vector3 ejectDir = Vector3.forward;
        if (this.ejectDirection != null)
        {
            ejectDir = this.ejectDirection.forward;
        }
        else if (carRootRb != null)
        {
            ejectDir = carRootRb.linearVelocity.normalized;
            if (ejectDir.magnitude < 0.1f)
            {
                ejectDir = transform.forward;
            }
        }

        foreach (var rb in driverRbs)
        {
            rb.AddForce(ejectDir * driverEjectForce);
            rb.AddForce(Vector3.up * driverEjectHeight * rb.mass);
            rb.AddTorque(Random.insideUnitSphere * 50f);
        }

        driver.transform.SetParent(null);
    }

    // ===== ОТКЛЮЧЕНИЕ УПРАВЛЕНИЯ =====
    private void DisableCarControls()
    {
        // Отключаем все коллайдеры корневого объекта
        Collider[] colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Отключаем MovingCar (если есть)
        MovingCar movingCar = GetComponent<MovingCar>();
        if (movingCar != null)
        {
            movingCar.enabled = false;
        }

        // Отключаем BotAI (если есть)
        /*MonoBehaviour botAI = GetComponent("BotAI");
        if (botAI != null)
        {
            botAI.enabled = false;
        }*/
    }
}