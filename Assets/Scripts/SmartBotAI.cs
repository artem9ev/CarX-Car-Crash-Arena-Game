using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Поиск цели: перебирает статический реестр MovingCar.ActiveCars, отбрасывает
/// себя и других ботов (по наличию включённого SmartBotAI), берёт ближайшего
/// в пределах detectionRange. Раньше использовался OverlapSphere по PlayerLayer,
/// но слой у бота и игрока общий и менять его нельзя, так что физический поиск
/// по слою не мог отличить игрока от другого бота.
///
/// Избегание препятствий: как и раньше, через raycast'ы по obstacleLayer.
///
/// Важно: используется тот же CarController.OnGas/OnBrake/OnSteer и те же RPC,
/// что и обычный игрок, т.к. бот тоже владеет своей машиной как Owner на сервере,
/// SmartBotAI вызывает методы напрямую в FixedUpdate/Update без Owner-RPC от клиента,
/// поскольку выполняется только на сервере.
/// </summary>
[RequireComponent(typeof(MovingCar))]
[RequireComponent(typeof(CarController))]
public class SmartBotAI : NetworkBehaviour
{
    [Header("Настройки AI")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float stopDistance = 4f;
    [SerializeField] private float reactionTime = 1f;
    [SerializeField] private float updateTargetInterval = 1f;

    [Header("Настройки поворота")]
    [Tooltip("Угол поворота, при котором steer = ±1")]
    [SerializeField, Range(1f, 90f)] private float maxSteerAngle = 45f;

    [Header("Объезд препятствий")]
    [Tooltip("Слой(и) препятствий, которые нужно объезжать (не включает других машин — их отслеживаем через реестр MovingCar)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Длина лучей объезда")]
    [SerializeField] private float rayLength = 5f;
    [Tooltip("Количество лучей объезда")]
    [SerializeField, Range(3, 9)] private int rayCount = 5;
    [Tooltip("Угловой разброс лучей от forward")]
    [SerializeField, Range(10f, 90f)] private float raySpread = 45f;
    [Tooltip("Сила уклонения при обнаружении препятствия")]
    [SerializeField, Min(0f)] private float avoidanceForce = 2f;
    [Tooltip("Сектор (от forward), внутри которого препятствие считается фронтальным и требует торможения, а не просто объезда")]
    [SerializeField, Range(1f, 45f)] private float frontalSectorAngle = 15f;
    [Tooltip("Доля rayLength, ниже которой фронтальное препятствие считается критичным для торможения (0 = вплотную, 1 = на всей длине луча)")]
    [SerializeField, Range(0.05f, 1f)] private float frontalBrakeThreshold = 0.35f;

    private MovingCar targetPlayer;

    private MovingCar _car;
    private CarController _controller;

    private float timer;
    private float targetUpdateTimer;
    private Vector3 currentDirection;

    private void Start()
    {
        if (!IsServer) return;

        _car = GetComponent<MovingCar>();
        _controller = GetComponent<CarController>();

        timer = reactionTime;
        targetUpdateTimer = updateTargetInterval;
        currentDirection = transform.forward;
        FindPlayer();
    }

    private void Update()
    {
        if (!IsServer) return;

        targetUpdateTimer -= Time.deltaTime;
        if (targetUpdateTimer <= 0)
        {
            // Периодически ищем ближайшую машину заново — вдруг цель уехала
            // слишком далеко или появилась новая. Если ничего в радиусе нет,
            // FindPlayer() сбросит targetPlayer в null и Patrol() продолжит работу.
            FindPlayer();
            targetUpdateTimer = updateTargetInterval;
        }
        timer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (targetPlayer != null)
        {
            ChaseTarget();
        }
        else
        {
            Patrol();
            FindPlayer();
        }
    }

    /// <summary>
    /// Ищет ближайшую машину-игрока в радиусе detectionRange через статический
    /// реестр MovingCar.ActiveCars. Пропускает себя и других ботов.
    /// </summary>
    private void FindPlayer()
    {
        MovingCar best = null;
        float bestDistance = detectionRange;

        foreach (var candidate in MovingCar.ActiveCars)
        {
            if (candidate == null || candidate == _car)
                continue; // пропускаем себя

            if (IsBot(candidate))
                continue; // пропускаем других ботов

            float d = Vector3.Distance(transform.position, candidate.transform.position);
            if (d <= bestDistance)
            {
                bestDistance = d;
                best = candidate;
            }
        }

        targetPlayer = best;
    }

    /// <summary>
    /// Машина считается ботом, если на ней есть включённый SmartBotAI.
    /// Так отличаем ботов от игроков без завязки на физический слой.
    /// </summary>
    private static bool IsBot(MovingCar car)
    {
        var bot = car.GetComponent<SmartBotAI>();
        return bot != null && bot.enabled;
    }

    private void ChaseTarget()
    {
        if (targetPlayer == null) return;

        Vector3 toTarget = targetPlayer.position - transform.position;
        toTarget.y = 0f; // считаем горизонтально, чтобы прыжки/трамплины игрока не сбивали "прицел"

        float distanceToTarget = toTarget.magnitude;
        Vector3 directionToTarget = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;

        if (distanceToTarget > stopDistance)
        {
            ApplyMovement(directionToTarget, 1f);
        }
        else
        {
            // Совсем близко к цели — таранить, доворачивая по направлению.
            Steer(directionToTarget);
            _controller.OnGas(1f);
            _controller.OnBrake(0f);
        }

        currentDirection = directionToTarget;
    }

    private void Patrol()
    {
        if (timer <= 0)
        {
            float randomAngle = Random.Range(-90f, 90f);
            currentDirection = Quaternion.Euler(0, randomAngle, 0) * transform.forward;
            timer = reactionTime + Random.Range(0f, 1f);
        }

        ApplyMovement(currentDirection, 0.5f); // патрульная скорость поменьше
    }

    /// <summary>
    /// Общая точка применения движения: если выбранное направление ведёт в
    /// препятствие, корректирует финальное направление (желаемое направление +
    /// уклонение от препятствий) и передаёт итоговый ввод в CarController.
    /// </summary>
    private void ApplyMovement(Vector3 desiredDirection, float baseThrottle)
    {
        Vector3 steerDirection = desiredDirection;
        float throttle = baseThrottle;
        float brake = 0f;

        if (TryAvoidObstacles(desiredDirection, out Vector3 avoidedDirection, out float frontalClearFraction))
        {
            steerDirection = avoidedDirection;

            if (frontalClearFraction < frontalBrakeThreshold)
            {
                // Фронтальное препятствие вплотную — тормозим, а не просто объезжаем,
                // чтобы не влетать во что-то на полном ходу.
                throttle = 0f;
                brake = 1f - frontalClearFraction / frontalBrakeThreshold;
            }
            else
            {
                throttle *= frontalClearFraction;
            }
        }

        Steer(steerDirection);
        _controller.OnGas(throttle);
        _controller.OnBrake(brake);
    }

    /// <summary>
    /// Веер raycast'ов вокруг obstacleLayer. Возвращает true, если что-то
    /// обнаружено. adjustedDirection — desiredDirection, скорректированное на
    /// уклонение. frontalClearFraction — насколько свободен фронтальный сектор
    /// (1 = полностью свободен, 0 = препятствие вплотную).
    /// </summary>
    private bool TryAvoidObstacles(Vector3 desiredDirection, out Vector3 adjustedDirection, out float frontalClearFraction)
    {
        adjustedDirection = desiredDirection;
        frontalClearFraction = 1f;

        bool hitAny = false;
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount == 1 ? 0.5f : (float)i / (rayCount - 1); // 0..1
            float angle = Mathf.Lerp(-raySpread, raySpread, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, rayLength, obstacleLayer))
            {
                hitAny = true;
                float closeness = 1f - Mathf.Clamp01(hit.distance / rayLength); // 0..1, 1 = вплотную

                // Направление уклонения противоположно направлению луча, взвешено
                // близостью, суммируется по всем лучам.
                avoidance += -dir * closeness * avoidanceForce;

                if (Mathf.Abs(angle) <= frontalSectorAngle)
                {
                    float clear = Mathf.Clamp01(hit.distance / rayLength);
                    frontalClearFraction = Mathf.Min(frontalClearFraction, clear);
                }
            }
        }

        if (hitAny)
        {
            Vector3 combined = desiredDirection + avoidance;
            adjustedDirection = combined.sqrMagnitude > 0.001f ? combined.normalized : desiredDirection;
        }

        return hitAny;
    }

    /// <summary>
    /// Считает угол между текущим направлением машины и желаемым направлением,
    /// нормирует его в диапазон [-1; 1] и передаёт как ввод руля — так же,
    /// как это делает PlayerInputHandler для игрока.
    /// </summary>
    private void Steer(Vector3 desiredDirection)
    {
        float angle = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);
        float steer = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f);
        _controller.OnSteer(steer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount == 1 ? 0.5f : (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-raySpread, raySpread, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Gizmos.DrawRay(transform.position, dir * rayLength);
        }
    }
}