using Unity.Netcode;
using UnityEngine;

public class CarWheel : MonoBehaviour
{
    [Header("Колёса")]
    [SerializeField] private WheelCollider _wheelCollider;

    [Header("Визуализация колёс")]
    [SerializeField] private Transform _wheelTransform;

    [SerializeField] private ParticleSystem _dust;
    [SerializeField] private int maxRate = 20;

    private float _spinAngle = 0f;

    public bool IsGrounded => _wheelCollider.isGrounded;
    public float CurrentSteerAngle => _wheelCollider.steerAngle;

    // Нужно движку для расчёта угловой скорости трансмиссии.
    // Валидно только на сервере — на клиенте WheelCollider не симулируется физически.
    public float rpm => _wheelCollider.rpm;
    public float angularVelocity => _wheelCollider.rpm / 60f;
    public float radius => _wheelCollider.radius;

    /// <summary>
    /// Вызывается ТОЛЬКО на сервере — читает реальную физику подвески.
    /// Возвращает, насколько колесо сейчас "провалилось" относительно точки крепления
    /// вдоль локальной оси up коллайдера (0 = полностью разжато).
    /// </summary>
    public float GetSuspensionCompression()
    {
        _wheelCollider.GetWorldPose(out Vector3 worldPos, out _);

        return Vector3.Dot(_wheelCollider.transform.position - worldPos, _wheelCollider.transform.up);
    }

    public SurfaceType GetSurfaceType()
    {
        return GetSurfaceType(out _);
    }

    public SurfaceType GetSurfaceType(out SurfaceDefinition definition)
    {
        definition = null;
        if (!_wheelCollider.GetGroundHit(out WheelHit hit))
            return SurfaceType.None;

        definition = SurfaceDatabase.Instance.Get(hit.collider.sharedMaterial);
        return definition != null ? definition.surfaceType : SurfaceType.Ground;
    }

    private SurfaceType _lastAppliedSurface = SurfaceType.None;

    public void ApplySurfaceEffects(SurfaceType surface)
    {
        if (surface == _lastAppliedSurface) return; // ничего не изменилось — не трогаем эффекты
        _lastAppliedSurface = surface;

        var def = SurfaceDatabase.Instance.GetByType(surface);
        if (def != null && def.surfaceType == SurfaceType.Ground)
        {
            _dust.Play();
        }
        else 
        {
            _dust.Stop();
        }
        // переключить цвет/текстуру пылевой ParticleSystem (не Instantiate/Destroy!)
        /*var main = _dust.main;
        main.startColor = def != null ? def.dustColor : Color.white;

        // сменить пул звуков качения (не Play() каждый кадр, а смена клипа у уже играющего looping AudioSource)
        if (def != null && def.rollingClips.Length > 0)
            _rollAudioSource.clip = def.rollingClips[Random.Range(0, def.rollingClips.Length)];*/
    }

    /// <summary>
    /// Вызывается ВСЕМИ (сервер и клиенты) каждый Update.
    /// Строит визуальную позу колеса вручную, не полагаясь на GetWorldPose()
    /// (на клиенте эта функция не отражает актуальное состояние подвески/спина).
    /// </summary>
    public void ApplyVisual(float suspensionCompression, float steerAngle, float angularVelocity)
    {
        Vector3 mountPos = _wheelCollider.transform.position;
        Vector3 suspensionOffset = -_wheelCollider.transform.up * suspensionCompression;
        _wheelTransform.position = mountPos + suspensionOffset;

        _spinAngle = (_spinAngle + angularVelocity) % 360;

        Quaternion steerRotation = _wheelCollider.transform.rotation * Quaternion.Euler(0f, steerAngle, 0f);
        _wheelTransform.rotation = steerRotation * Quaternion.Euler(_spinAngle, 0f, 0f);
    }

    public void SteerWheel(float steering, float turnSpeed, float maxAngle)
    {
        float angleT = _wheelCollider.steerAngle / maxAngle;
        float t = Mathf.Lerp(angleT, steering, Time.deltaTime * turnSpeed / maxAngle / Mathf.Abs(steering - angleT));
        _wheelCollider.steerAngle = maxAngle * t;
    }

    public void SetTorque(float value)
    {
        _wheelCollider.motorTorque = value;
    }

    public void SetBrake(float value)
    {
        _wheelCollider.brakeTorque = value;
    }
}
