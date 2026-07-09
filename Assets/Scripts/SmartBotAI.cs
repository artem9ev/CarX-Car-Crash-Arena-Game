using UnityEngine;

[RequireComponent(typeof(MovingCar))]
public class BotAI : MonoBehaviour
{
    [Header("Настройки AI")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float stopDistance = 4f;
    [SerializeField] private float reactionTime = 1f;
    [SerializeField] private float updateTargetInterval = 1f;

    [Header("Настройки движения")]
    [SerializeField] private float turnSpeed = 50f;

    [Header("Raycasting")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private float avoidanceForce = 2f;

    private MovingCar movingCar;
    private float timer;
    private float targetUpdateTimer;
    private bool hasTarget = false;
    private Vector3 currentDirection;

    private void Start()
    {
        movingCar = GetComponent<MovingCar>();
        timer = reactionTime;
        targetUpdateTimer = updateTargetInterval;
        currentDirection = transform.forward;
        FindPlayer();
    }

    private void Update()
    {
        targetUpdateTimer -= Time.deltaTime;
        if (targetUpdateTimer <= 0)
        {
            FindPlayer();
            targetUpdateTimer = updateTargetInterval;
        }
        timer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (target != null && hasTarget)
        {
            ChaseTarget();
        }
        else
        {
            Patrol();
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= detectionRange)
            {
                target = player.transform;
                hasTarget = true;
            }
        }
    }

    private void ChaseTarget()
    {
        if (target == null) return;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Поворот к цели
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime * 0.01f);

        // Управление через MovingCar
        if (distanceToTarget > stopDistance)
        {
            movingCar.SetInputs(-0.8f, 0f);  // Газ вперёд
        }
        else
        {
            movingCar.SetInputs(0.3f, 0f);  // Сдать назад
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

        Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime * 0.01f);

        movingCar.SetInputs(0.5f, 0f);  // Медленное движение
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}