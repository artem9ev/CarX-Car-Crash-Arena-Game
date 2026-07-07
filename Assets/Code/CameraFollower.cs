using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollower : MonoBehaviour
{
    [SerializeField] private MovingCar m_target;
    [Header("Parameters")]
    [SerializeField] private float m_acceleration = 3f;
    [SerializeField] private float m_angle = 15f;
    [SerializeField] private float m_range = 5f;
    [Space]
    [SerializeField] private Vector3 m_targetOffset = Vector3.zero;
    [Space]
    [SerializeField] private LayerMask m_cameraHitMask;

    private Transform m_transform;
    private Vector3 m_aTargetVel;
    private Vector3 m_bTargetVel;
    private Vector3 m_targetVelocity;
    private float m_lastFixedUpdateTime;
    private Vector3 m_velocity;
    private Vector3 m_pivot;
    private Rigidbody m_targetRigidbody;

    public Vector3 position => m_transform.position;
    public Vector3 forward => m_transform.forward;

    private void Start()
    {
        Initialize();
    }

    private void OnValidate()
    {
        //Initialize();
    }

    // ===== ��������� ����� ��� ������� ������������� =====
    public void Initialize(MovingCar target, Vector3 offset, float angle, float range, float acceleration, LayerMask hitMask)
    {
        m_target = target;
        m_targetOffset = offset;
        m_angle = angle;
        m_range = range;
        m_acceleration = acceleration;
        m_cameraHitMask = hitMask;

        Initialize(); // �������� ��������� Initialize
    }
    // ================================================

    private void Initialize()
    {
        m_transform = transform;

        if (m_target != null)
        {
            m_targetRigidbody = m_target.GetComponent<Rigidbody>();
            m_pivot = m_target.transform.position + m_targetOffset;
        }

        UpdatePosition();
    }

    private void FixedUpdate()
    {
        if (m_target == null || m_targetRigidbody == null) return;

        m_lastFixedUpdateTime = Time.time;
        m_aTargetVel = m_bTargetVel;
        m_bTargetVel = m_targetRigidbody.linearVelocity;
    }

    private void Update()
    {
        if (m_target == null || m_targetRigidbody == null) return;

        m_targetVelocity = Vector3.Lerp(m_aTargetVel, m_bTargetVel, (Time.time - m_lastFixedUpdateTime) / Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (m_target == null)
        {
            return;
        }
        Move();
        UpdatePosition();
    }

    private void Move()
    {
        m_velocity = (m_target.transform.position + m_targetOffset - m_pivot) * m_acceleration * Time.deltaTime;
        m_pivot += m_velocity;
    }

    public void UpdatePosition()
    {
        if (m_target == null || m_targetRigidbody == null)
        {
            return;
        }

        Vector3 targetLook = Vector3.zero;

        if (m_targetRigidbody.linearVelocity.magnitude > 0.5)
        {
            Vector3 localVelocity = m_target.transform.InverseTransformDirection(m_targetVelocity);
            targetLook = m_target.transform.TransformDirection(Quaternion.Euler(m_angle * localVelocity.normalized.z, 0, 0) * localVelocity);
        }
        else
        {
            targetLook = Quaternion.Euler(m_angle, 0, 0) * m_target.transform.forward;
        }

        float t = Time.deltaTime / (0.4f + Mathf.Clamp01(m_targetRigidbody.linearVelocity.magnitude / 40) * 4.6f);
        m_transform.forward = Vector3.Lerp(m_transform.forward, targetLook, t);

        float range = m_range;
        if (Physics.SphereCast(m_pivot, 0.2f, -m_transform.forward, out RaycastHit hit, m_range, m_cameraHitMask))
        {
            range = hit.distance - 0.2f;
        }

        m_transform.position = m_pivot + m_targetOffset * m_target.transform.up.y - m_transform.forward * range;
    }

    public void SetOffset(Vector3 offset)
    {
        m_targetOffset = offset;
    }
}