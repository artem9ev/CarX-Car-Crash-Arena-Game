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

        _health.TakeDamage(impulse);

        CarCollisionEventData eventData = new CarCollisionEventData()
        {
            point = collision.contacts[0].point,
            normal = collision.contacts[0].normal,
            impulse = collision.impulse,
            relativeVelocity = collision.relativeVelocity
        };

        ClientCarCollisionRpc(eventData);
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
        // Сериализуем каждое поле Vector3 как три float
        serializer.SerializeValue(ref point);
        serializer.SerializeValue(ref normal);
        serializer.SerializeValue(ref impulse);
        serializer.SerializeValue(ref relativeVelocity);
    }
}
