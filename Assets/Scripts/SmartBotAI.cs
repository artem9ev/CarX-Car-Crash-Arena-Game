using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MovingCar))]
public class BotAI : NetworkBehaviour
{
    [Header("Настройки AI")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float stopDistance = 4f;
    [SerializeField] private float reactionTime = 1f;
    [SerializeField] private float updateTargetInterval = 1f;

    [Header("Настройки движения")]
    [SerializeField] private float turnSpeed = 50f;

    [Header("Raycasting")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask PlayerLayer;
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private float avoidanceForce = 2f;

    private MovingCar targetPlayer;

    private MovingCar movingCar;
    private float timer;
    private float targetUpdateTimer;
    private bool hasTarget = false;
    private Vector3 currentDirection;

    private void Start()
    {
        if (!IsServer) return;

        movingCar = GetComponent<MovingCar>();
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
    }

    private void ChaseTarget()
    {
        if (targetPlayer == null) return;

        Vector3 directionToTarget = (targetPlayer.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);

        // Поворот к цели
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime * 0.01f);

        // Управление через MovingCar
        if (distanceToTarget > stopDistance)
        {
            movingCar.OnGas(-0.8f);
        }
        else
        {
            movingCar.OnBrake(1);
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

        movingCar.OnGas(-0.5f); ;  // Медленное движение
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}