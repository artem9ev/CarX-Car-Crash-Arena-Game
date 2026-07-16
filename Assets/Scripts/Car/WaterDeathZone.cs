using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Отслеживает погружение машины в воду.
/// Проверяет только верхнюю часть машины через указанную точку.
/// </summary>
[RequireComponent(typeof(VehicleHealth))]
public class WaterDeathZone : NetworkBehaviour
{
    [Header("Настройки")]
    [Tooltip("Слой, на котором находится вода")]
    [SerializeField] private LayerMask _waterLayer = 1 << 4;

    [Tooltip("Точка проверки на машине (если не назначена, будет использоваться расчётная)")]
    [SerializeField] private Transform _checkPoint;

    [Tooltip("Смещение точки проверки относительно центра машины (вверх)")]
    [SerializeField] private float _verticalOffset = 0.5f;

    [Tooltip("Смещение точки проверки вперёд")]
    [SerializeField] private float _forwardOffset = 0f;

    [Tooltip("Радиус сферы проверки")]
    [SerializeField] private float _checkRadius = 0.5f;

    [Tooltip("Задержка перед смертью")]
    [SerializeField] private float _deathDelay = 0.5f;

    [Header("Отладка")]
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private Color _debugColor = Color.cyan;

    private VehicleHealth _health;
    private Transform _transform;
    private bool _isInWater = false;
    private float _waterEnterTime = 0f;
    private Vector3 _checkWorldPosition;

    private void Awake()
    {
        _health = GetComponent<VehicleHealth>();
        _transform = transform;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (_health == null || _health.IsDead) return;

        // Получаем позицию для проверки
        _checkWorldPosition = GetCheckPosition();

        // Проверяем, находится ли точка в воде
        bool isCurrentlyInWater = Physics.CheckSphere(_checkWorldPosition, _checkRadius, _waterLayer);

        // Логика входа/выхода из воды
        if (isCurrentlyInWater && !_isInWater)
        {
            // Только что вошли в воду
            _isInWater = true;
            _waterEnterTime = Time.time;
            //Debug.Log($"[WaterDeathZone] Машина {OwnerClientId} вошла в воду в точке {_checkWorldPosition}");
        }
        else if (!isCurrentlyInWater && _isInWater)
        {
            // Вышли из воды - сбрасываем таймер
            _isInWater = false;
            _waterEnterTime = 0f;
            //Debug.Log($"[WaterDeathZone] Машина {OwnerClientId} вышла из воды");
        }

        // Если в воде и прошло достаточно времени - убиваем
        if (_isInWater && Time.time - _waterEnterTime >= _deathDelay)
        {
            KillCar();
        }
    }

    /// <summary>
    /// Получает позицию для проверки
    /// </summary>
    private Vector3 GetCheckPosition()
    {
        if (_checkPoint != null)
        {
            // Используем назначенную точку
            return _checkPoint.position;
        }

        // Иначе вычисляем позицию относительно центра машины
        Vector3 localOffset = Vector3.up * _verticalOffset + Vector3.forward * _forwardOffset;
        return _transform.TransformPoint(localOffset);
    }

    /// <summary>
    /// Убивает машину
    /// </summary>
    private void KillCar()
    {
        if (!IsServer) return;
        if (_health == null || _health.IsDead) return;

        // Наносим фатальный урон
        _health.TakeDamage(99999f, ulong.MaxValue, false);

        // Сбрасываем состояние
        _isInWater = false;
        _waterEnterTime = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos) return;

        Vector3 checkPos;
        if (Application.isPlaying)
        {
            checkPos = _checkWorldPosition;
        }
        else
        {
            // В режиме редактора вычисляем позицию
            if (_checkPoint != null)
            {
                checkPos = _checkPoint.position;
            }
            else
            {
                Vector3 localOffset = Vector3.up * _verticalOffset + Vector3.forward * _forwardOffset;
                checkPos = transform.TransformPoint(localOffset);
            }
        }

        // Рисуем сферу проверки
        Gizmos.color = _debugColor;
        Gizmos.DrawWireSphere(checkPos, _checkRadius);

        // Рисуем линию от центра машины к точке проверки
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, checkPos);

        // Если в воде - показываем красным
        if (Application.isPlaying && _isInWater)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(checkPos, _checkRadius * 0.3f);
        }

        // Метка с текстом
#if UNITY_EDITOR
        UnityEditor.Handles.Label(checkPos + Vector3.up * 0.5f,
            $"Water Check\nPos: {checkPos}\nIn Water: {_isInWater}");
#endif
    }

    private void OnValidate()
    {
        // Автоматически настраиваем слой воды
        if (_waterLayer == 0)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer != -1)
            {
                _waterLayer = 1 << waterLayer;
            }
        }
    }

    /// <summary>
    /// Принудительная проверка (для тестов)
    /// </summary>
    [ContextMenu("Force Check Water")]
    public void ForceCheckWater()
    {
        if (!IsServer) return;
        if (_health == null || _health.IsDead) return;

        if (Physics.CheckSphere(GetCheckPosition(), _checkRadius, _waterLayer))
        {
            KillCar();
        }
    }
}