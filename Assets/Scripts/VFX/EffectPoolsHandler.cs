using UnityEngine;

public class EffectPoolsHandler : MonoBehaviour
{
    [SerializeField] private BurstParticlesPool _carHitPool;
    [SerializeField] private BurstParticlesPool _carExplosionPool;

    private void Start()
    {
        ClientEventBus.Instance.onCarCollision += OnCarHit;
        ClientEventBus.Instance.onCarExplosion += OnCarExplosion;
    }

    private void OnDestroy()
    {
        ClientEventBus.Instance.onCarCollision -= OnCarHit;
        ClientEventBus.Instance.onCarExplosion -= OnCarExplosion;
    }

    private void OnCarHit(CarCollisionEventData eventData)
    {
        if (_carHitPool == null)
            return;

        _carHitPool.OnEmmit(eventData.point, eventData.normal);
    }

    private void OnCarExplosion(Vector3 position)
    {
        if ( _carExplosionPool == null)
            return;

        _carExplosionPool.OnEmmit(position, Vector3.up);
    }
}
