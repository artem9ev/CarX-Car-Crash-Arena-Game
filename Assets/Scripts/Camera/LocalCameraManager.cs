using UnityEngine;

public class LocalCameraManager : MonoBehaviour
{
    [SerializeField] private CameraFollower cameraFollowerPrefab;

    private CameraFollower _cameraFollower;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        ClientEventBus.Instance.onCarOwn += OnPlayerCarSpawned;
    }

    private void OnDestroy()
    {
        ClientEventBus.Instance.onCarOwn -= OnPlayerCarSpawned;
    }    
    
    private void OnPlayerCarSpawned(MovingCar car)
    {
        if (_cameraFollower == null)
            _cameraFollower = Instantiate(cameraFollowerPrefab, transform);

        _cameraFollower.SetTarget(car);
    }
}
