using UnityEngine;

public class SoundEffectPoolsHandler : MonoBehaviour
{
    [SerializeField] private SoundEffectPool _carHitPool;
    [SerializeField] private SoundEffectPool _carExplosionPool;

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

        _carHitPool.OnPlaySound(eventData.point);
    }

    private void OnCarExplosion(Vector3 position)
    {
        if (_carExplosionPool == null)
            return;

        _carExplosionPool.OnPlaySound(position);
    }
}
