using Unity.Netcode;
using UnityEngine;

public class CarWheel : NetworkBehaviour
{
    [Header("Колёса")]
    [SerializeField] private WheelCollider _wheelCollider;

    [Header("Визуализация колёс")]
    [SerializeField] private Transform _wheelTransform;

    private float _spinAngle = 0f;

    public bool IsGrounded => _wheelCollider.isGrounded;
    public float CurrentSteerAngle => _wheelCollider.steerAngle;

    private void Update()
    {
        /*if (!IsServer) 
            return;*/

        //UpdateSingleWheelVisual(_wheelCollider, _wheelTransform);
    }

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

    /// <summary>
    /// Вызывается ВСЕМИ (сервер и клиенты) каждый Update.
    /// Строит визуальную позу колеса вручную, не полагаясь на GetWorldPose()
    /// (на клиенте эта функция не отражает актуальное состояние подвески/спина).
    /// </summary>
    public void ApplyVisual(float suspensionCompression, float steerAngle, float forwardSpeed)
    {
        Vector3 mountPos = _wheelCollider.transform.position;
        Vector3 suspensionOffset = -_wheelCollider.transform.up * suspensionCompression;
        _wheelTransform.position = mountPos + suspensionOffset;

        _spinAngle += (forwardSpeed / _wheelCollider.radius) * Mathf.Rad2Deg * Time.deltaTime;
        _spinAngle %= 360f;

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

public struct WheelVisualState : INetworkSerializable
{
    public float frCompression;
    public float flCompression;
    public float brCompression;
    public float blCompression;
    public float steerAngle;
    public float forwardSpeed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref frCompression);
        serializer.SerializeValue(ref flCompression);
        serializer.SerializeValue(ref brCompression);
        serializer.SerializeValue(ref blCompression);
        serializer.SerializeValue(ref steerAngle);
        serializer.SerializeValue(ref forwardSpeed);
    }
}
