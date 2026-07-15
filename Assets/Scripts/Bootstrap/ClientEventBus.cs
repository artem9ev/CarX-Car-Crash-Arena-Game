using UnityEngine;
using UnityEngine.Events;

public class ClientEventBus : MonoBehaviour
{
    private static ClientEventBus _instance;

    public static ClientEventBus Instance => _instance;

    public event UnityAction<MovingCar> onCarOwn;

    public event UnityAction<CarCollisionEventData> onCarCollision;

    public event UnityAction<Vector3> onCarExplosion;

    public static event UnityAction onAwake;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject);

        onAwake?.Invoke();
    }

    public void InvokeCarOwn(MovingCar car)
    {
        onCarOwn?.Invoke(car);
    }

    public void InvokeCarCollisionEvents(CarCollisionEventData carCollisionData)
    {
        // Тут обработать пришедшее с сервера событие столкновения одной или нескольких из машин
        onCarCollision?.Invoke(carCollisionData);
    }

    public void InvokeCarExplosion(Vector3 position)
    {
        // Тут обработать пришедшее с сервера событие о смерти машины и ее взрыве
        onCarExplosion?.Invoke(position);
    }
}
