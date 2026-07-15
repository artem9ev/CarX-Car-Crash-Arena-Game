using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollower : MonoBehaviour
{
    [SerializeField] private MovingCar m_target;  // ← Изменил с MovingCar на Transform
    [Header("Parameters")]
    [SerializeField] private Vector3 m_acceleration = new Vector3(7, 15, 30);
    [SerializeField] private float m_angle = 15f;
    [SerializeField] private float m_range = 3f;
    [Space]
    [SerializeField] private Vector3 m_targetOffset = new Vector3(0, 1, -1);
    [Space]
    [SerializeField] private LayerMask m_cameraHitMask;
    [Header("Shake")]
    [SerializeField] private float m_amplitude = 0.1f;
    [SerializeField] private float m_minShakeSpeed = 100f;
    [SerializeField] private float m_maxShakeSpeed = 200f;

    private Transform m_transform;
    private Vector3 m_aTargetVel;
    private Vector3 m_bTargetVel;
    private Vector3 m_targetVelocity;
    private float m_lastFixedUpdateTime;
    private Vector3 m_velocity;
    private Vector3 m_pivot;
    private Rigidbody m_targetRigidbody;


    //private Vector3 m_targetVelLerp;

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

    private void Initialize()
    {
        m_transform = transform;

        if (m_target != null)
        {
            m_targetRigidbody = m_target.GetComponent<Rigidbody>();
            m_pivot = m_target.position + m_targetOffset;
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
        //m_targetVelLerp = Vector3.Lerp(m_targetVelLerp, m_target.linearVelocity, Time.deltaTime * m_angleCoef);
        m_targetVelocity = Vector3.Lerp(m_aTargetVel, m_bTargetVel, (Time.time - m_lastFixedUpdateTime) / Time.fixedDeltaTime);
        m_targetVelocity = m_targetRigidbody.linearVelocity;
    }

    private void LateUpdate()
    {
        if (m_target == null) return;

        Move();
        UpdatePosition();
    }

    private void Move()
    {
        Vector3 moveX = m_target.transform.right * (m_targetOffset.x - m_target.transform.InverseTransformPoint(m_pivot).x) * m_acceleration.x;
        Vector3 moveY = m_target.transform.up * (m_targetOffset.y - m_target.transform.InverseTransformPoint(m_pivot).y) * m_acceleration.y;
        Vector3 moveZ = m_target.transform.forward * (m_targetOffset.z - m_target.transform.InverseTransformPoint(m_pivot).z) * m_acceleration.z;
        m_velocity = (moveX + moveY + moveZ) * Time.deltaTime;
        m_pivot += m_velocity;

        /*m_velocity = (m_target.position + m_targetOffset - m_pivot) * m_acceleration * Time.deltaTime;
        m_pivot += m_velocity;*/
    }

    public void UpdatePosition()
    {
        if (m_target == null)
        {
            return;
        }

        Vector3 targetLook = Vector3.zero;
        if (m_target.linearVelocity.magnitude > 0.5)
        {
            Vector3 localVelocity = m_target.transform.InverseTransformDirection(m_targetVelocity);
            targetLook = m_target.transform.TransformDirection(Quaternion.Euler(m_angle * localVelocity.normalized.z, 0, 0) * localVelocity);
        }
        else
        {
            targetLook = Quaternion.Euler(-m_angle, 0, 0) * m_target.transform.forward;
        }

        float t = Time.deltaTime / (0.4f + Mathf.Clamp01(m_target.linearVelocity.magnitude / 40) * 4.6f);

        m_transform.forward = Vector3.Lerp(m_transform.forward, targetLook, t);

        float range = m_range;
        if (Physics.SphereCast(m_pivot, 0.2f, -m_transform.forward, out RaycastHit hit, m_range, m_cameraHitMask))
        {
            range = hit.distance - 0.2f;
        }

        float shakeX = Mathf.PerlinNoise1D(Time.time * 40) - 0.5f;
        float shakeY = Mathf.PerlinNoise1D(Time.time * 20) - 0.5f;
        Vector3 shake = (m_transform.right * shakeX + m_transform.up * shakeY) * m_amplitude * Mathf.InverseLerp(m_minShakeSpeed, m_maxShakeSpeed, m_target.linearVelocity.magnitude * 3.6f);

        m_transform.position = m_pivot + m_targetOffset * m_target.transform.up.y - m_transform.forward * range + shake;
    }

    public void ResetPosition()
    {
        m_pivot = m_target.position;

        m_transform.forward = Quaternion.Euler(-m_angle, 0, 0) * m_target.transform.forward;
        m_transform.position = m_pivot + m_targetOffset * m_target.transform.up.y - m_transform.forward * m_range;
    }

    public void SetTarget(MovingCar target)
    {
        m_target = target;
        Initialize();
        ResetPosition();
    }

    public void SetOffset(Vector3 offset)
    {
        m_targetOffset = offset;
    }
}