using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxCars = 5;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float minDistance = 10f;

    [Header("Случайный спавн")]
    [SerializeField] private bool useRandomSpawning = true;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(50, 0, 50);

    [Header("Настройки камеры игрока")]
    [SerializeField] private Vector3 playerCameraOffset = new Vector3(0, 1, -1);
    [SerializeField] private float playerCameraAngle = 15f;
    [SerializeField] private float cameraRange = 2f;
    [SerializeField] private float cameraAcceleration = 3f;
    [SerializeField] private LayerMask cameraHitMask;

    private GameObject[] spawnedCars;
    private CameraFollower[] spawnedCameraFollowers;
    private float lastSpawnTime;
    private int currentCarCount;
    private bool playerSpawned = false;

    private void Start()
    {
        spawnedCars = new GameObject[maxCars];
        spawnedCameraFollowers = new CameraFollower[maxCars];
        lastSpawnTime = Time.time;

        if (carPrefab != null)
        {
            SpawnCar();
        }
    }

    private void Update()
    {
        UpdateCarCount();

        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            if (currentCarCount < maxCars)
            {
                SpawnCar();
                lastSpawnTime = Time.time;
            }
        }
    }

    private void UpdateCarCount()
    {
        currentCarCount = 0;
        for (int i = 0; i < spawnedCars.Length; i++)
        {
            if (spawnedCars[i] != null)
            {
                currentCarCount++;
            }
            else
            {
                spawnedCars[i] = null;
                spawnedCameraFollowers[i] = null;
            }
        }
    }

    private void SpawnCar()
    {
        Vector3 spawnPosition = GetSpawnPosition();

        if (IsValidSpawnPosition(spawnPosition))
        {
            GameObject newCar = Instantiate(carPrefab, spawnPosition, Quaternion.identity);

            if (useRandomSpawning)
            {
                float randomY = Random.Range(0f, 360f);
                newCar.transform.rotation = Quaternion.Euler(0, randomY, 0);
            }

            int carIndex = AddToSpawnedCars(newCar);
            CreateCameraForCar(newCar, carIndex);

            Debug.Log($"🚗 Машина #{carIndex + 1} заспавнена! Всего машин: {currentCarCount + 1}");
        }
    }

    private void CreateCameraForCar(GameObject car, int carIndex)
    {
        bool isPlayer = !playerSpawned;

        if (isPlayer)
        {
            // ===== КАМЕРА ТОЛЬКО ДЛЯ ИГРОКА =====
            MovingCar carScript = car.GetComponent<MovingCar>();
            if (carScript == null)
            {
                carScript = car.GetComponentInChildren<MovingCar>();
            }

            if (carScript == null)
            {
                Debug.LogError("❌ MovingCar не найден на машине игрока!");
                return;
            }

            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(car.transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;

            Camera carCamera = cameraObj.AddComponent<Camera>();
            CameraFollower cameraFollower = cameraObj.AddComponent<CameraFollower>();

            carCamera.tag = "MainCamera";
            carCamera.name = "PlayerCamera";
            carCamera.fieldOfView = 75f;
            carCamera.depth = 0;
            carCamera.enabled = true;

            cameraFollower.Initialize(
                carScript,
                playerCameraOffset,
                playerCameraAngle,
                cameraRange,
                cameraAcceleration,
                cameraHitMask
            );

            spawnedCameraFollowers[carIndex] = cameraFollower;

            playerSpawned = true;
            Debug.Log("🎮 Создана Main Camera для игрока!");
        }
        else
        {
            // ===== БОТЫ БЕЗ КАМЕР =====
            spawnedCameraFollowers[carIndex] = null;
            Debug.Log($"🤖 Бот #{carIndex + 1} заспавнен (без камеры)");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (useRandomSpawning)
        {
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                0,
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );
            return randomPos;
        }
        else
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        for (int i = 0; i < spawnedCars.Length; i++)
        {
            if (spawnedCars[i] != null)
            {
                float distance = Vector3.Distance(position, spawnedCars[i].transform.position);
                if (distance < minDistance)
                {
                    return false;
                }
            }
        }

        if (Physics.Raycast(position + Vector3.up * 5, Vector3.down, out RaycastHit hit, 10f))
        {
            return true;
        }

        return false;
    }

    private int AddToSpawnedCars(GameObject car)
    {
        for (int i = 0; i < spawnedCars.Length; i++)
        {
            if (spawnedCars[i] == null)
            {
                spawnedCars[i] = car;
                return i;
            }
        }
        return -1;
    }

    public void ForceSpawnCar()
    {
        if (currentCarCount < maxCars)
        {
            SpawnCar();
        }
    }

    public void DespawnAllCars()
    {
        for (int i = 0; i < spawnedCars.Length; i++)
        {
            if (spawnedCars[i] != null)
            {
                Destroy(spawnedCars[i]);
                spawnedCars[i] = null;
            }
            if (spawnedCameraFollowers[i] != null)
            {
                Destroy(spawnedCameraFollowers[i].gameObject);
                spawnedCameraFollowers[i] = null;
            }
        }
        currentCarCount = 0;
        playerSpawned = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);

        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 1f);
                }
            }
        }
    }
}