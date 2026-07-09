using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Префабы")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private GameObject cameraFollowerPrefab;

    [Header("Точки спавна")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Настройки")]
    [SerializeField] private int maxCars = 5;
    [SerializeField] private float spawnInterval = 2f;

    private GameObject[] spawnedCars;
    private CameraFollower[] spawnedCameraFollowers;
    private bool[] carActive;
    private int currentCarCount = 0;
    private bool playerSpawned = false;
    private float lastSpawnTime = 0f;

    private void Start()
    {
        spawnedCars = new GameObject[maxCars];
        spawnedCameraFollowers = new CameraFollower[maxCars];
        carActive = new bool[maxCars];

        SpawnCar();
        playerSpawned = true;
    }

    private void Update()
    {
        if (currentCarCount < maxCars && Time.time - lastSpawnTime > spawnInterval)
        {
            SpawnCar();
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnCar()
    {
        if (currentCarCount >= maxCars) return;

        int spawnIndex = currentCarCount;
        Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];

        GameObject newCar = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedCars[spawnIndex] = newCar;

        if (!playerSpawned)
        {
            newCar.tag = "Player";
            playerSpawned = true;
            CreateCameraForCar(newCar);  // ← Передал только машину
        }
        else
        {
            newCar.tag = "Bot";
        }

        carActive[spawnIndex] = true;
        currentCarCount++;

        Debug.Log($"🚗 Машина #{spawnIndex + 1} заспавнена", gameObject);
    }

    // ← ИСПРАВЛЕНО: убрал carIndex
    private void CreateCameraForCar(GameObject car)
    {
        if (cameraFollowerPrefab == null)
        {
            Debug.LogWarning("⚠️ CameraFollower префаб не назначен!");
            return;
        }

        // Удаляем ВСЕ существующие камеры (не по индексу, а все!)
        CameraFollower[] allCameras = FindObjectsOfType<CameraFollower>();
        foreach (var cam in allCameras)
        {
            Debug.Log($"🗑️ Удаление старой камеры: {cam.gameObject.name}");
            Destroy(cam.gameObject);
        }

        // Создаём новую камеру
        GameObject cameraObj = Instantiate(cameraFollowerPrefab, car.transform.position, Quaternion.identity);
        CameraFollower cameraFollower = cameraObj.GetComponent<CameraFollower>();

        if (cameraFollower != null)
        {
            cameraFollower.Target = car.transform;

            // Сохраняем в массив (индекс 0 = игрок)
            spawnedCameraFollowers[0] = cameraFollower;
        }

        Debug.Log($"📹 Камера создана для {car.name}");
    }

    public void DespawnAllCars()
    {
        Debug.Log("🗑️ Удаление всех машин...");

        // Удаляем все машины
        for (int i = 0; i < spawnedCars.Length; i++)
        {
            if (spawnedCars[i] != null)
            {
                Destroy(spawnedCars[i]);
                spawnedCars[i] = null;
            }
            carActive[i] = false;
        }

        // Удаляем ВСЕ камеры на сцене (надёжнее)
        CameraFollower[] allCameras = FindObjectsOfType<CameraFollower>();
        foreach (var cam in allCameras)
        {
            Debug.Log($"🗑️ Удаление камеры: {cam.gameObject.name}");
            Destroy(cam.gameObject);
        }

        // Очищаем массив
        for (int i = 0; i < spawnedCameraFollowers.Length; i++)
        {
            spawnedCameraFollowers[i] = null;
        }

        currentCarCount = 0;
        playerSpawned = false;

        Debug.Log("✅ Все машины и камеры удалены");
    }

    public void ForceSpawnCar()
    {
        Debug.Log("🔄 ForceSpawnCar() вызван");

        // Сбрасываем всё
        currentCarCount = 0;
        playerSpawned = false;

        // Спавним новую машину
        SpawnCar();

        Debug.Log("✅ Новая машина заспавнена");
    }

    public GameObject GetPlayerCar()
    {
        return spawnedCars[0];
    }

    public CameraFollower GetPlayerCamera()
    {
        return spawnedCameraFollowers[0];
    }
}