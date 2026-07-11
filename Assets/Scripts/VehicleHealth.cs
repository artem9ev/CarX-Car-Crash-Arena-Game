using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class VehicleHealth : NetworkBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Настройки урона")]
    [SerializeField] private float _damageMultiplier = 0.5f;
    [SerializeField] private float _minDamage = 5f;

    private MovingCar _car;

    private NetworkVariable<float> _currentHealth;

    public event UnityAction<float> OnHealthChanged;
    public event UnityAction OnDeath;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth.Value;
    public bool IsDead => _currentHealth.Value <= 0f;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            _currentHealth.Value = _maxHealth;

        _currentHealth.OnValueChanged += OnHealthValueChange;
    }

    private void OnHealthValueChange(float oldValue, float newValue)
    {
        Debug.Log($"[client: {OwnerClientId}] --- CAR IS DESTROYED ", gameObject);
        // Вызываем локальное событие на клиентах
        OnHealthChanged?.Invoke(newValue);
        if (newValue <= 0)
        {
            OnDeath?.Invoke();
            // Дополнительно можно вызвать клиентские эффекты через отдельный RPC,
            // но можно и через событие
            //ClientCarDeathEffectsRpc(transform.position);
        }
    }

    public void TakeDamage(float impulse)
    {
        if (IsDead || !IsServer) 
            return;

        float damage = impulse * _damageMultiplier;

        if (impulse < 0)
            throw new System.ArgumentException($"Try to take damage that have value less 0 (impulse: {impulse})");

        if (damage < _minDamage)
            damage = _minDamage;

        _currentHealth.Value -= damage;
        _currentHealth.Value = Mathf.Max(0, CurrentHealth);

        OnHealthChanged?.Invoke(CurrentHealth);

        if (_currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead || !IsServer) return;

        ClientCarDeathRpc(transform.position);

        DisableControlScripts();
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
        if (IsDead || !IsServer) 
            return;
        _currentHealth.Value += amount;
        _currentHealth.Value = Mathf.Min(CurrentHealth, _maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth);
    }

    public void SetHealth(float health)
    {
        if (IsDead || !IsServer) 
            return;
        _currentHealth.Value = Mathf.Clamp(health, 0, _maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);
        if (CurrentHealth <= 0) Die();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ClientCarDeathRpc(Vector3 position)
    {
        ClientEventBus.Instance.InvokeCarExplosion(position);
    }
}