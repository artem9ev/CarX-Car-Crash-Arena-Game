using UnityEngine;

public class Bot_hp : MonoBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Настройки урона")]
    [SerializeField] private float collisionDamageMultiplier = 0.5f;
    [SerializeField] private float minCollisionDamage = 5f;

    [Header("Отключение при смерти")]
    [SerializeField] private float destroyDelay = 3f;

    private bool isDead = false;
    private Rigidbody rb;

    // События (всё, что нужно другим скриптам)
    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDeath;

    // Свойства
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead || rb == null || LayerMask.LayerToName(gameObject.layer) == "bot" || LayerMask.LayerToName(gameObject.layer) == "Player") return;

        float impactForce = collision.relativeVelocity.magnitude * rb.mass;
        if (impactForce < minCollisionDamage) return;

        float damage = impactForce * collisionDamageMultiplier;
        TakeDamage(damage);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("☠️ Машина уничтожена!");

        // Просто уведомляем всех подписчиков
        OnDeath?.Invoke();

        // Отключаем скрипты управления
        DisableControlScripts();

        // Удаляем машину через задержку
        Destroy(gameObject, destroyDelay);
    }

    private void DisableControlScripts()
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this && script.enabled)
            {
                // Не отключаем скрипты эффектов и ragdoll — они нужны после смерти
                if (script is VehicleHealth ||
                    script is DriverRagdoll ||
                    script is CarDestruction ||
                    script is WheelDustEffect ||
                    script is CarAudio ||
                    script is WheelDustEffect)
                {
                    continue;
                }
                script.enabled = false;
            }
        }

        // Отключаем коллайдеры машины (но не колёс)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void Repair(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetHealth(float health)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        if (currentHealth <= 0) Die();
    }
}