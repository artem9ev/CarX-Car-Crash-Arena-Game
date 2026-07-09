using UnityEngine;

public class SmartBotAI : MonoBehaviour
{
    [Header("Õ‡ÒÚÓÈÍË")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float obstacleAvoidanceRange = 5f;

    [Header("Raycasting")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float rayLength = 5f;

    [Header("—Ò˚ÎÍË")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform target;

    private Vector3 currentDirection;
    private bool isAvoidingObstacle = false;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        currentDirection = transform.forward;
    }

    private void Update()
    {
        FindPlayer();
    }

    private void FixedUpdate()
    {
        if (target != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        AvoidObstacles();
    }

    // ===== œŒ»—  »√–Œ ¿ =====
    private void FindPlayer()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= detectionRange)
                {
                    target = player.transform;
                }
            }
        }
    }

    // ===== œ–≈—À≈ƒŒ¬¿Õ»≈ =====
    private void ChasePlayer()
    {
        if (target == null) return;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        // œÓ‚ÓÓÚ Í Ë„ÓÍÛ
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime * 0.01f);

        // ƒ‚ËÊÂÌËÂ
        if (distance > 3f)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    // ===== œ¿“–”À»–Œ¬¿Õ»≈ =====
    private void Patrol()
    {
        rb.linearVelocity = currentDirection * speed * 0.5f;
    }

    // ===== ” ÀŒÕ≈Õ»≈ Œ“ œ–≈œﬂ“—“¬»… =====
    private void AvoidObstacles()
    {
        Vector3[] rayDirections = new Vector3[]
        {
            transform.forward,
            transform.forward * 0.7f + transform.right * 0.7f,
            transform.forward * 0.7f - transform.right * 0.7f
        };

        foreach (var dir in rayDirections)
        {
            if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, rayLength, obstacleLayer))
            {
                Debug.DrawRay(transform.position, dir.normalized * rayLength, Color.red);

                // ”‚Ó‡˜Ë‚‡ÂÏÒˇ
                currentDirection = Vector3.Reflect(dir, hit.normal).normalized;
                isAvoidingObstacle = true;
                return;
            }
            else
            {
                Debug.DrawRay(transform.position, dir.normalized * rayLength, Color.green);
            }
        }

        isAvoidingObstacle = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}