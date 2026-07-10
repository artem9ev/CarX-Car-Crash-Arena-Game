using UnityEngine;
using UnityEngine.Events;

public class ClientEventBus : MonoBehaviour
{
    private static ClientEventBus _instance;

    public static ClientEventBus Instance => _instance;

    public UnityAction<MovingCar> onCarOwn;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    public void InvokeCarOwn(MovingCar car)
    {
        onCarOwn?.Invoke(car);
    }
}
