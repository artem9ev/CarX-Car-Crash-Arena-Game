using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
    [SerializeField] private MovingCar defaultCarPrefab;
    [SerializeField] private MovingCar defaultBotPrefab;
    public int Count => _spawnPoints.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public SpawnPoint this[int index]
    {
        get
        {
            return _spawnPoints[index];
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
            {
                OnClientConnected(clientID);
            }
        }
    }

    private SpawnPoint GetFreeSpawnPoint()
    {
        foreach (var point in _spawnPoints)
        {
            if (point.IsClear)
            {
                return point;
            }
        }
        return null;
    }

    private void OnClientConnected(ulong сlientID)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SpawnCarForClient(сlientID);
        SpawnBot();
    }

    /// <summary>
    /// Спавнит машину для указанного клиента на свободной точке спавна.
    /// Используется как при первом подключении, так и при респавне после смерти.
    /// Вызывать только на сервере.
    /// </summary>
    public bool SpawnCarForClient(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return false;

        SpawnPoint spawnPoint = GetFreeSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[SpawnManager] Нет свободных точек спавна для клиента {clientId}");
            return false;
        }

        spawnPoint.TryGetPoint(out Vector3 pos, out Quaternion rot);

        var car = Instantiate(defaultCarPrefab);
        car.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        car.SetSpawnPosition(pos, rot);
        return true;
    }

    private int _nextBotSlotId = 0;

    public bool SpawnBot() => SpawnBot(-1);

    /// <summary>
    /// Спавнит бота (без владельца — принадлежит серверу) на свободной точке спавна.
    /// </summary>
    /// <param name="slotId">
    /// Стабильный "слот" бота (см. BotIdentity). Передайте -1, чтобы выдать новый слот
    /// (первый спавн). При респавне после смерти передавайте slotId погибшего бота —
    /// тогда его статистика (Kills/Deaths) продолжит копиться, а не обнулится.
    /// </param>
    public bool SpawnBot(int slotId)
    {
        if (!NetworkManager.Singleton.IsServer) return false;

        SpawnPoint spawnPoint = GetFreeSpawnPoint();
        if (spawnPoint == null) return false;

        spawnPoint.TryGetPoint(out Vector3 pos, out Quaternion rot);

        int assignedSlot = slotId >= 0 ? slotId : _nextBotSlotId++;

        var bot = Instantiate(defaultBotPrefab);
        if (bot.TryGetComponent(out BotIdentity botIdentity))
        {
            botIdentity.AssignSlot(assignedSlot);
        }
        else
        {
            Debug.LogWarning("[SpawnManager] На defaultBotPrefab не найден BotIdentity — статистика бота работать не будет.");
        }

        bot.GetComponent<NetworkObject>().Spawn();
        bot.SetSpawnPosition(pos, rot);
        return true;
    }
}