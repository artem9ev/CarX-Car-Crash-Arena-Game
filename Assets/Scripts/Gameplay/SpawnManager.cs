using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
    [SerializeField] private MovingCar defaultCarPrefab;
    [SerializeField] private MovingCar defaultBotPrefab;
    public int Count => _spawnPoints.Count;

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

    private void OnClientConnected(ulong сlientID)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SpawnPoint spawnPoint = GetFreeSpawnPoint();
        
        if (spawnPoint == null)
        {
            return;
        }

        spawnPoint.TryGetPoint(out Vector3 pos, out Quaternion rot);

        var car = Instantiate(defaultCarPrefab);
        car.GetComponent<NetworkObject>().SpawnWithOwnership(сlientID);
        car.SetSpawnPosition(pos, rot);

        spawnPoint = GetFreeSpawnPoint();
        spawnPoint.TryGetPoint(out pos, out rot);

        var bot = Instantiate(defaultBotPrefab);
        bot.GetComponent<NetworkObject>().Spawn();
        bot.SetSpawnPosition(pos, rot);
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

}
