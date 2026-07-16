using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
public class CarPhysics : NetworkBehaviour
{
    [SerializeField] private float _damageIgnoreZoneOffset = 1.5f;
    [SerializeField] private float _criticalDamageZoneOffset = -1.5f;
    [Header("Hits")]
    [SerializeField, Min(0f)] private float m_minHitImpulse = 500f;

    private VehicleHealth _health;
    private Transform _transform;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawRay(transform.position + transform.forward * _damageIgnoreZoneOffset - transform.right * 3, transform.right * 6f);
        Gizmos.DrawRay(transform.position + transform.forward * _criticalDamageZoneOffset - transform.right * 3, transform.right * 6f);
    }

    private void Awake()
    {
        _transform = transform;
        _health = GetComponent<VehicleHealth>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        float impulse = collision.impulse.magnitude;

        if (impulse < m_minHitImpulse) return;

        float pointOffsetZ = _transform.InverseTransformPoint(collision.contacts[0].point).z;

        if (pointOffsetZ < _damageIgnoreZoneOffset)
        {
            ulong attackerClientId = GetAttackerClientId(collision);
            _health.TakeDamage(impulse, attackerClientId, pointOffsetZ < _criticalDamageZoneOffset);
        }

        CarCollisionEventData eventData = new CarCollisionEventData()
        {
            point = collision.contacts[0].point,
            normal = collision.contacts[0].normal,
            impulse = collision.impulse,
            relativeVelocity = collision.relativeVelocity
        };

        ClientCarCollisionRpc(eventData);

        StartCoroutine(CollisionDebugRoitine(eventData));
    }

    private IEnumerator CollisionDebugRoitine(CarCollisionEventData eventData)
    {
        float t = 0;
        while (t < 5f)
        {
            Debug.DrawRay(eventData.point, eventData.normal * 2f, Color.blue);
            Debug.DrawRay(eventData.point, Vector3.up * 0.5f, Color.green);
            yield return null;
            t += Time.deltaTime;
        }
    }

    /// <summary>
    /// Пытается определить, чья машина ударила текущую.
    /// Возвращает ClientId владельца другой машины (или PseudoClientId, если это бот —
    /// у ботов свой BotIdentity, т.к. их OwnerClientId у всех совпадает с ServerClientId).
    /// Возвращает ulong.MaxValue, если это не машина игрока/бота (стена, окружение и т.п.).
    /// </summary>
    private ulong GetAttackerClientId(Collision collision)
    {
        // Коллайдер может висеть на дочернем объекте (колесо и т.д.),
        // поэтому ищем NetworkObject и VehicleHealth в родителях.
        var otherNetworkObject = collision.collider.GetComponentInParent<NetworkObject>();
        if (otherNetworkObject == null)
        {
            return ulong.MaxValue;
        }

        // Не считаем машину атакующей саму себя (например, столкновение частей одной машины).
        if (otherNetworkObject == NetworkObject)
        {
            return ulong.MaxValue;
        }

        var otherHealth = otherNetworkObject.GetComponent<VehicleHealth>();
        if (otherHealth == null)
        {
            return ulong.MaxValue;
        }

        // Если другая машина — бот, используем его псевдо-ID вместо OwnerClientId
        // (у всех ботов OwnerClientId одинаковый — ServerClientId — и его нельзя
        // использовать для различения ботов друг от друга и от хоста).
        var otherBotIdentity = otherNetworkObject.GetComponent<BotIdentity>();
        if (otherBotIdentity != null)
        {
            return otherBotIdentity.PseudoClientId;
        }

        return otherNetworkObject.OwnerClientId;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ClientCarCollisionRpc(CarCollisionEventData collisionData)
    {
        ClientEventBus.Instance.InvokeCarCollisionEvents(collisionData);
    }
}

[System.Serializable]
public struct CarCollisionEventData : INetworkSerializable
{
    public Vector3 point;
    public Vector3 normal;

    public Vector3 impulse;
    public Vector3 relativeVelocity;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Сериализуем каждое поле
        serializer.SerializeValue(ref point);
        serializer.SerializeValue(ref normal);
        serializer.SerializeValue(ref impulse);
        serializer.SerializeValue(ref relativeVelocity);
    }
}