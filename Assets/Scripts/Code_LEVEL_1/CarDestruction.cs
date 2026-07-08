using UnityEngine;

public class CarDestruction : MonoBehaviour
{
    [Header("Части машины")]
    [SerializeField] private Rigidbody[] carParts;  // Все части машины (кузов, колёса)
    [SerializeField] private float explosionForce = 500f;  // Сила разлёта частей
    [SerializeField] private float explosionRadius = 5f;   // Радиус взрыва

    [Header("Водитель")]
    [SerializeField] private GameObject driver;  // Объект водителя
    [SerializeField] private float driverEjectForce = 1000f;  // Сила вылета водителя
    [SerializeField] private float driverEjectHeight = 5f;    // Высота вылета
    [SerializeField] private Transform ejectDirection;  // Направление вылета (обычно вперёд машины)

    [Header("Настройки ragdoll")]
    [SerializeField] private float ragdollActivationDelay = 0.1f;  // Задержка перед включением ragdoll

    private MovingCar car;
    private Rigidbody carRootRb;
    private bool isDestroyed = false;

    private void Start()
    {
        car = GetComponent<MovingCar>();
        carRootRb = GetComponent<Rigidbody>();

        if (car == null)
        {
            Debug.LogError("❌ CarDestruction: MovingCar не найден!");
            return;
        }

        // Подписываемся на событие смерти
        car.OnDeath += HandleDeath;

        // Если части не назначены - пытаемся найти автоматически
        if (carParts == null || carParts.Length == 0)
        {
            FindCarParts();
        }

        // Выключаем физику частей при старте (они должны быть кинематичными или привязанными)
        DisablePartsPhysics();
    }

    private void OnDestroy()
    {
        if (car != null)
        {
            car.OnDeath -= HandleDeath;
        }
    }

    // ===== АВТОМАТИЧЕСКИЙ ПОИСК ЧАСТЕЙ =====
    private void FindCarParts()
    {
        // Ищем все Rigidbody в дочерних объектах
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>();
        carParts = new Rigidbody[allRigidbodies.Length - 1]; // -1 потому что корневой Rigidbody не считаем

        int index = 0;
        foreach (var rb in allRigidbodies)
        {
            if (rb != carRootRb) // Пропускаем корневой Rigidbody
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
                part.isKinematic = true;  // Делаем кинематичными (не подвержены физике)
                part.useGravity = false;
            }
        }

        // Водитель тоже кинематичен при старте
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

        // Вылетаем водителя
        if (driver != null)
        {
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
                part.isKinematic = false;  // Включаем физику
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
                // Направление от центра взрыва к части
                Vector3 direction = part.position - explosionCenter;

                // Сила уменьшается с расстоянием
                float distance = direction.magnitude;
                float forceMultiplier = Mathf.Clamp01(1f - distance / explosionRadius);

                // Применяем силу
                part.AddExplosionForce(
                    explosionForce * forceMultiplier,
                    explosionCenter,
                    explosionRadius
                );

                // Добавляем случайное вращение
                part.AddTorque(
                    Random.insideUnitSphere * 100f * forceMultiplier
                );
            }
        }
    }

    // ===== ВЫЛЕТ ВОДИТЕЛЯ =====
    private void EjectDriver()
    {
        if (driver == null) return;

        Debug.Log(" Водитель вылетает!");

        // Включаем физику водителя (ragdoll)
        Rigidbody[] driverRbs = driver.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in driverRbs)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Определяем направление вылета
        Vector3 ejectDirection = Vector3.forward;
        if (this.ejectDirection != null)
        {
            ejectDirection = this.ejectDirection.forward;
        }
        else if (carRootRb != null)
        {
            // Если направление не назначено - используем направление движения машины
            ejectDirection = carRootRb.linearVelocity.normalized;
            if (ejectDirection.magnitude < 0.1f)
            {
                ejectDirection = transform.forward;
            }
        }

        // Применяем силу к каждой части водителя
        foreach (var rb in driverRbs)
        {
            // Основная сила в направлении вылета
            rb.AddForce(ejectDirection * driverEjectForce);

            // Дополнительная сила вверх
            rb.AddForce(Vector3.up * driverEjectHeight * rb.mass);

            // Случайное вращение
            rb.AddTorque(Random.insideUnitSphere * 50f);
        }

        // Отсоединяем водителя от машины
        driver.transform.SetParent(null);
    }

    // ===== ОТКЛЮЧЕНИЕ УПРАВЛЕНИЯ =====
    private void DisableCarControls()
    {
        // Отключаем все коллайдеры корневого объекта (если есть)
        Collider[] colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Отключаем скрипт управления (если нужно)
        MovingCar movingCar = GetComponent<MovingCar>();
        if (movingCar != null)
        {
            movingCar.enabled = false;
        }
    }
}