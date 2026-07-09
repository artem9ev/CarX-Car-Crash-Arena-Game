using UnityEngine;

public class LocalCameraManager : MonoBehaviour
{
    [SerializeField] private GameObject cameraFollowerPrefab;
    private CameraFollower _cameraFollower;

    private void Start()
    {
        // Подписываемся один раз при старте менеджера
        ClientEventBus.Instance.onCarOwn += OnPlayerCarSpawned;
    }

    private void OnPlayerCarSpawned(MovingCar car)
    {
        if (_cameraFollower != null) return; // камера уже есть

        var camObj = Instantiate(cameraFollowerPrefab, car.transform);
        _cameraFollower = camObj.GetComponent<CameraFollower>();
        _cameraFollower.Target = car.transform;
    }

    private void OnDestroy()
    {
        ClientEventBus.Instance.onCarOwn -= OnPlayerCarSpawned;
    }
}
