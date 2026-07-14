using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Слушает событие смерти VehicleHealth и заменяет видимую модель водителя
/// на сетевой рэгдолл, "выстреливая" его вперёд из открытого кузова машины.
///
/// ВАЖНО: имя класса DriverRagdoll не случайное — VehicleHealth.DisableControlScripts()
/// уже содержит исключение для этого типа, чтобы он не отключался после смерти машины.
/// Если переименуете класс, поправьте и список исключений в VehicleHealth.
/// </summary>
[RequireComponent(typeof(VehicleHealth))]
public class DriverRagdoll : NetworkBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Видимая модель водителя внутри машины (обычный неанимированный/анимированный меш сидящего игрока)")]
    [SerializeField] private GameObject _driverModel;

    [Tooltip("Префаб рэгдолла. Должен иметь NetworkObject и Rigidbody на корне, и должен быть добавлен в NetworkPrefabs list у NetworkManager")]
    [SerializeField] private NetworkObject _ragdollPrefab;

    [Tooltip("Точка вылета водителя — обычно место сиденья, forward смотрит наружу через открытый кузов")]
    [SerializeField] private Transform _ejectPoint;

    [Header("Настройки выстрела")]
    [SerializeField, Min(0f)] private float _ejectForce = 8f;
    [SerializeField, Min(0f)] private float _ejectUpwardForce = 4f;
    [SerializeField, Min(0f)] private float _ejectTorque = 5f;
    [SerializeField] private bool _inheritCarVelocity = true;
    private NetworkObject ragdollInstance;
    // Синхронизируем видимость модели водителя на всех клиентах.
    // Пишет только сервер, читают все — это стандартный паттерн для "флагов состояния".
    private readonly NetworkVariable<bool> _isEjected =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private VehicleHealth _health;
    private Rigidbody _carRb;

    private void Awake()
    {
        _health = GetComponent<VehicleHealth>();
        _carRb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        _health.OnDeath += HandleDeath;
        _isEjected.OnValueChanged += OnEjectedChanged;

        // Применяем текущее состояние сразу — важно для игроков, подключившихся
        // после того, как машина уже "умерла" (late join).
        OnEjectedChanged(false, _isEjected.Value);
    }

    public override void OnNetworkDespawn()
    {
        _health.OnDeath -= HandleDeath;
        _isEjected.OnValueChanged -= OnEjectedChanged;
    }

    private void HandleDeath()
    {
        ForceActivateRagdoll();
    }

    /// <summary>
    /// Публичный вход для катапультирования водителя. Может быть вызван как
    /// изнутри (через подписку на VehicleHealth.OnDeath), так и снаружи —
    /// например, из CarDestruction.HandleDeath(). Безопасен для повторного
    /// вызова: реальный эффект произойдёт только один раз.
    /// </summary>
    public void ForceActivateRagdoll()
    {
        // OnDeath в VehicleHealth вызывается локально на каждом клиенте
        // (это колбэк NetworkVariable.OnValueChanged), но спавнить сетевые
        // объекты и менять NetworkVariable может только сервер.
        if (!IsServer) return;
        if (_isEjected.Value) return; // защита от повторного срабатывания

        EjectDriver();
    }

    private void EjectDriver()
    {
        _isEjected.Value = true; // спрячет модель водителя на всех клиентах через колбэк ниже

        if (_ragdollPrefab == null || _ejectPoint == null)
        {
            Debug.LogWarning("DriverRagdoll: не назначен _ragdollPrefab или _ejectPoint", this);
            return;
        }

        ragdollInstance = Instantiate(_ragdollPrefab, _ejectPoint.position, _ejectPoint.rotation);

        // true = деспавнить вместе с уничтожением объекта, сервер — владелец спавна.
        ragdollInstance.Spawn(true);

        // ВАЖНО: NetworkObject лежит на корне префаба ragdoll'а, а физический
        // Rigidbody (Hips) — на дочернем объекте. GetComponentInChildren идёт
        // в глубину и найдёт Rigidbody на Hips раньше, чем на костях-потомках,
        // потому что Hips — первый предок в цепочке суставов.
        Rigidbody ragdollRb = ragdollInstance.GetComponentInChildren<Rigidbody>();
        if (ragdollRb != null)
        {
            Vector3 direction = (_ejectPoint.forward + Vector3.up * (_ejectUpwardForce / Mathf.Max(_ejectForce, 0.01f))).normalized;
            Vector3 launchVelocity = direction * _ejectForce;

            if (_inheritCarVelocity && _carRb != null)
                launchVelocity += _carRb.linearVelocity;

            ragdollRb.linearVelocity = launchVelocity;
            ragdollRb.angularVelocity = Random.insideUnitSphere * _ejectTorque;
        }
        else
        {
            Debug.LogWarning("DriverRagdoll: на префабе рэгдолла нет Rigidbody на корневом объекте", ragdollInstance);
        }
    }

    private void OnEjectedChanged(bool oldValue, bool newValue)
    {
        if (_driverModel != null)
            _driverModel.SetActive(!newValue);
        Debug.Log(_driverModel!=null);
    }
    public void DespawnRagDoll()
    {
        if(ragdollInstance != null && IsServer)
        {
            ragdollInstance.Despawn(true);
        }
    }
}