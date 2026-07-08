using UnityEngine;

public class DriverRagdoll : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private ConfigurableJoint configurableJoint;
    [SerializeField] private Rigidbody[] ragdollParts;

    [Header("Настройки вылета")]
    [SerializeField] private bool ejectOnDeath = true;
    [SerializeField] private float ejectForce = 500f;
    [SerializeField] private float ejectUpForce = 300f;
    [SerializeField] private float randomTorque = 50f;

    [Header("Задержки")]
    [SerializeField] private float activateDelay = 0.05f;

    private MovingCar car;
    private Rigidbody rootRigidbody;
    private bool isRagdollActive = false;
    private ConfigurableJoint[] allJoints;  // Все суставы ragdoll

    private void Start()
    {
        car = GetComponentInParent<MovingCar>();

        if (car == null)
        {
            Debug.LogError("❌ DriverRagdoll: MovingCar не найден!");
            return;
        }

        rootRigidbody = GetComponent<Rigidbody>();
        if (rootRigidbody == null)
        {
            Debug.LogError("❌ DriverRagdoll: Rigidbody не найден!");
            return;
        }

        if (configurableJoint == null)
        {
            configurableJoint = GetComponent<ConfigurableJoint>();
        }

        if (configurableJoint == null)
        {
            Debug.LogError("❌ Configurable Joint не найден!");
            return;
        }

        // Автоматически находим части тела
        if (ragdollParts == null || ragdollParts.Length == 0)
        {
            ragdollParts = GetComponentsInChildren<Rigidbody>();
        }

        // Находим все ConfigurableJoint на частях тела
        allJoints = GetComponentsInChildren<ConfigurableJoint>();
        Debug.Log($"✅ Найдено {allJoints.Length} ConfigurableJoint");

        // Усыпляем ragdoll при старте
        SleepRagdoll();

        car.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (car != null)
        {
            car.OnDeath -= HandleDeath;
        }
    }

    // ===== УСЫПИТЬ RAGDOLL =====
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

    // ===== ВКЛЮЧИТЬ RAGDOLL =====
    private void EnableRagdoll(Vector3 ejectDirection)
    {
        if (isRagdollActive) return;
        isRagdollActive = true;

        Debug.Log("👤 Активация ragdoll...");

        // 1. Ломаем главный Configurable Joint (который крепит к машине)
        if (configurableJoint != null)
        {
            Destroy(configurableJoint);
            Debug.Log("🔓 Главный Joint уничтожен");
        }

        // 2. РАЗБЛОКИРУЕМ все остальные суставы частей тела
        UnlockAllJoints();
        Debug.Log($"🔓 Разблокировано {allJoints.Length} суставов");

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
            if (car != null)
            {
                Rigidbody carRb = car.GetComponent<Rigidbody>();
                if (carRb != null)
                {
                    rb.linearVelocity = carRb.linearVelocity;
                    rb.angularVelocity = carRb.angularVelocity * 0.5f;
                }
            }
        }

        // 5. Применяем силу вылета
        if (ejectOnDeath)
        {
            ApplyEjectForce(ejectDirection);
        }

        Debug.Log("✅ Ragdoll активирован!");
    }

    // ===== РАЗБЛОКИРОВКА ВСЕХ СУСТАВОВ =====
    private void UnlockAllJoints()
    {
        foreach (var joint in allJoints)
        {
            if (joint == null) continue;

            // Разблокируем линейное движение
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            // Разблокируем вращение
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            // Настраиваем лимиты (чтобы не разваливался полностью)
            SoftJointLimit limit = new SoftJointLimit
            {
                limit = 45f  // Угол отклонения (можно настроить)
            };

            joint.lowAngularXLimit = limit;
            joint.highAngularXLimit = limit;
            joint.angularYLimit = limit;
            joint.angularZLimit = limit;

            // Настраиваем линейные лимиты (чтобы части не улетали далеко друг от друга)
            SoftJointLimitSpring spring = new SoftJointLimitSpring
            {
                spring = 500f,
                damper = 50f
            };

            joint.xDrive = new JointDrive
            {
                positionSpring = 500f,
                positionDamper = 50f,
                maximumForce = Mathf.Infinity
            };

            joint.yDrive = new JointDrive
            {
                positionSpring = 500f,
                positionDamper = 50f,
                maximumForce = Mathf.Infinity
            };

            joint.zDrive = new JointDrive
            {
                positionSpring = 500f,
                positionDamper = 50f,
                maximumForce = Mathf.Infinity
            };
        }
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

        if (car != null)
        {
            Rigidbody carRb = car.GetComponent<Rigidbody>();
            if (carRb != null && carRb.linearVelocity.magnitude > 1f)
            {
                ejectDirection = carRb.linearVelocity.normalized;
            }
            else
            {
                ejectDirection = car.transform.forward;
            }
        }

        EnableRagdoll(ejectDirection);
    }

    public void ForceActivateRagdoll()
    {
        ActivateRagdoll();
    }
}