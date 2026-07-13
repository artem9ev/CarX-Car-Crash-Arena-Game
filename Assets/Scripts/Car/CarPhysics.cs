using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
public class CarPhysics : NetworkBehaviour
{
    [Header("Hits")]
    [SerializeField, Min(0f)] private float m_softHitImpulse = 500f;
    [SerializeField, Min(0f)] private float m_hardHitImpulse = 4000f;

    private VehicleHealth _health;

    private void Awake()
    {
        _health = GetComponent<VehicleHealth>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        float impulse = collision.impulse.magnitude;

        if (impulse < m_softHitImpulse) return;

        ulong attackerClientId = GetAttackerClientId(collision);

        _health.TakeDamage(impulse, attackerClientId);

        CarCollisionEventData eventData = new CarCollisionEventData()
        {
            point = collision.contacts[0].point,
            normal = collision.contacts[0].normal,
            impulse = collision.impulse,
            relativeVelocity = collision.relativeVelocity
        };

        ClientCarCollisionRpc(eventData);
    }

    /// <summary>
    /// Пытается определить, чья машина ударила текущую.
    /// Возвращает ClientId владельца другой машины, если это машина игрока (есть VehicleHealth),
    /// иначе ulong.MaxValue (столкновение со стеной/окружением/ботом без владельца и т.п.).
    /// </summary>
    private ulong GetAttackerClientId(Collision collision)
    {
        // Коллайдер может висеть на дочернем объекте (колесо и т.д.),
        // поэтому ищем NetworkObject и VehicleHealth в родителях.
        var otherNetworkObject = collision.collider.GetComponentInParent<NetworkObject>();
        if (otherNetworkObject == null)
            return ulong.MaxValue;

        // Не считаем машину атакующей саму себя (например, столкновение частей одной машины).
        if (otherNetworkObject == NetworkObject)
            return ulong.MaxValue;

        var otherHealth = otherNetworkObject.GetComponent<VehicleHealth>();
        if (otherHealth == null)
            return ulong.MaxValue;

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