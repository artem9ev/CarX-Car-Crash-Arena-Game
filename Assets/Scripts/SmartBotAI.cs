using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Простой ИИ бота: патрулирует случайными поворотами, при обнаружении игрока в
/// detectionRange — преследует и таранит (урон наносится через столкновение,
/// см. CarPhysics.OnCollisionEnter). Цель не сбрасывается, если игрок выехал
/// за пределы detectionRange — бот преследует до последнего.
///
/// Объезд препятствий: веер raycast'ов вперёд по obstacleLayer. Если что-то
/// обнаружено — желаемое направление отклоняется в сторону от препятствий,
/// а если препятствие прямо по курсу и близко — бот тормозит, а не просто
/// сбавляет газ (иначе на инерции всё равно въедет).
///
/// ВАЖНО: управление идёт через CarController.OnGas/OnBrake/OnSteer — те же
/// публичные методы, что вызывает обычный игрок через PlayerInputHandler.
/// Это работает, потому что у ботов OwnerClientId == ServerClientId (см.
/// BotIdentity/CarPhysics.GetAttackerClientId), а SmartBotAI выполняется
/// только на сервере — то есть сервер вызывает свой же Owner-RPC на своей же
/// машине, проверка владения проходит, и запрос исполняется как обычный ввод.
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
    [Tooltip("Слой игроков/ботов — по нему бот ищет цель для преследования (OverlapSphere)")]
    [SerializeField] private LayerMask PlayerLayer;

    [Header("Настройки руления")]
    [Tooltip("Угол между направлением машины и желаемым направлением, при котором руль считается вывернутым полностью (OnSteer = ±1)")]
    [SerializeField, Range(1f, 90f)] private float maxSteerAngle = 45f;

    [Header("Объезд препятствий")]
    [Tooltip("Слой стен/статичного окружения, которое нужно объезжать (НЕ должен включать игроков/ботов — их бот обнаруживает отдельно через PlayerLayer)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Длина лучей веера вперёд")]
    [SerializeField] private float rayLength = 5f;
    [Tooltip("Количество лучей веера (нечётное — тогда один луч смотрит точно вперёд)")]
    [SerializeField, Range(3, 9)] private int rayCount = 5;
    [Tooltip("Угловой раствор веера в каждую сторону от forward")]
    [SerializeField, Range(10f, 90f)] private float raySpread = 45f;
    [Tooltip("Насколько сильно препятствие отклоняет желаемое направление")]
    [SerializeField, Min(0f)] private float avoidanceForce = 2f;
    [Tooltip("Углы (от forward), которые считаются \"лобовыми\" — при близком препятствии в этом секторе бот тормозит, а не просто сбавляет газ")]
    [SerializeField, Range(1f, 45f)] private float frontalSectorAngle = 15f;
    [Tooltip("Доля rayLength, начиная с которой лобовое препятствие считается критически близким (0 = впритык, 1 = на пределе дальности луча)")]
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
            // Периодически ищем ближайшего игрока заново — даже во время погони,
            // чтобы бот мог переключиться на более близкую цель. Если сейчас в
            // радиусе никого нет, FindPlayer() ничего не тронет и старая цель
            // останется (см. FindPlayer) — бот преследует до последнего.
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

    private void FindPlayer()
    {
        Collider[] players = Physics.OverlapSphere(transform.position, detectionRange, PlayerLayer);

        if (players.Length > 0)
        {
            float distance = float.MaxValue;

            foreach (var player in players)
            {
                float d = Vector3.Distance(transform.position, player.transform.position);

                if (d < distance)
                {
                    distance = d;
                    targetPlayer = player.GetComponentInParent<MovingCar>();
                }
            }
        }
        // Если players.Length == 0 — намеренно ничего не делаем: цель (если была)
        // сохраняется, бот не прерывает погоню только из-за выхода игрока за
        // пределы detectionRange.
    }

    private void ChaseTarget()
    {
        if (targetPlayer == null) return;

        Vector3 toTarget = targetPlayer.position - transform.position;
        toTarget.y = 0f; // высоту игнорируем — иначе на трамплинах/склонах руление будет "дёргаться"

        float distanceToTarget = toTarget.magnitude;
        Vector3 directionToTarget = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;

        if (distanceToTarget > stopDistance)
        {
            ApplyMovement(directionToTarget, 1f);
        }
        else
        {
            // Доехали до цели — тормозим независимо от объезда препятствий.
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

        ApplyMovement(currentDirection, 0.5f); // умеренная скорость патрулирования
    }

    /// <summary>
    /// Общая точка приложения движения: берёт желаемое направление и базовый газ,
    /// накладывает объезд препятствий (отклонение направления + торможение при
    /// лобовом упоре) и отправляет итоговый ввод в CarController.
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
                // Лобовое препятствие близко — тормозим, а не просто сбавляем газ,
                // иначе на инерции бот всё равно в него въедет.
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
    /// Веер raycast'ов вперёд по obstacleLayer. Возвращает true, если что-то
    /// обнаружено. adjustedDirection — desiredDirection, отклонённое от
    /// препятствий. frontalClearFraction — насколько свободен путь строго по
    /// курсу (1 = ничего в пределах frontalSectorAngle, 0 = препятствие впритык).
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
                float closeness = 1f - Mathf.Clamp01(hit.distance / rayLength); // 0..1, 1 = впритык

                // Отталкиваем желаемое направление от препятствия — просто в
                // сторону, противоположную лучу, который в него попал.
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
    /// переводит его в диапазон [-1; 1] и отправляет как ввод руля — так же,
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