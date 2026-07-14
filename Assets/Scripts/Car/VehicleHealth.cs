using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class VehicleHealth : NetworkBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private float _maxHealth = 5000f;

    [Header("Настройки урона")]
    [SerializeField] private float _damageMultiplier = 0.2f;
    [SerializeField] private float _minDamage = 5f;

    private MovingCar _car;

    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>();

    public event UnityAction<float> OnHealthChanged;
    public event UnityAction OnDeath;

    /// <summary>
    /// Вызывается ТОЛЬКО на сервере в момент смерти машины.
    /// Аргумент — ClientId игрока, нанёсшего последний удар (killer).
    /// Если машина погибла не от игрока (например SetHealth от админки/скрипта),
    /// передаётся ulong.MaxValue.
    /// </summary>
    public event UnityAction<ulong> OnServerDeath;

    /// <summary>ClientId игрока, который нанёс последний урон этой машине.</summary>
    private ulong _lastAttackerClientId = ulong.MaxValue;

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

        if (IsClient)
            _currentHealth.OnValueChanged += OnHealthValueChange;
    }

    private void OnHealthValueChange(float oldValue, float newValue)
    {
        // Вызываем локальное событие на клиентах
        OnHealthChanged?.Invoke(newValue);
        if (newValue <= 0)
        {        
            OnDeath?.Invoke();
        }
    }

    /// <param name="impulse">Сила удара — используется для расчёта урона.</param>
    /// <param name="attackerClientId">
    /// ClientId игрока, который нанёс урон (например, владелец машины-тарана).
    /// Передавайте ulong.MaxValue, если источник урона не связан с конкретным игроком
    /// (окружение, самоподрыв и т.п.) — в этом случае предыдущий известный атакующий не перезатирается.
    /// </param>
    public void TakeDamage(float impulse, ulong attackerClientId = ulong.MaxValue, bool isCritical = false)
        if (IsDead || !IsServer)
            return;

        float damage = impulse * _damageMultiplier;

        if (impulse < 0)
            throw new System.ArgumentException($"Try to take damage that have value less 0 (impulse: {impulse})");

        if (damage < _minDamage)
            damage = _minDamage;

        if (isCritical)
        {
            damage *= 3f;
        }

        if (attackerClientId != ulong.MaxValue)
            _lastAttackerClientId = attackerClientId;

        _currentHealth.Value -= damage;
        _currentHealth.Value = Mathf.Max(0, CurrentHealth);

        OnHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        ClientCarDeathRpc(transform.position);

        DisableControlRpc();

        _car.StopCar();

        ulong attackerClientId = _lastAttackerClientId;
        _lastAttackerClientId = ulong.MaxValue; // сброс, чтобы следующая жизнь машины начиналась "с чистого листа"

        OnServerDeath?.Invoke(attackerClientId);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void DisableControlRpc()
    {
        if (!IsClient || !IsOwner || !TryGetComponent(out CarController controller))
            return;

        controller.DisableControlls();
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