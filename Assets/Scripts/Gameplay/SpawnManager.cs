using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _spawnPointns = new List<SpawnPoint>();
    [SerializeField] private MovingCar defaultCarPrefab;
    public int Count => _spawnPointns.Count;

    public SpawnPoint this[int index]
    {
        get
        {
            return _spawnPointns[index];
        }
    }

    private void Awake()
    {
        for (int i = 1; i < transform.childCount; i++) {
            SpawnPoint point = transform.GetChild(i).GetComponent<SpawnPoint>();
            if (point != null && !_spawnPointns.Contains(point))
            {
                _spawnPointns.Add(point);
            }
        }
    }
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
    }

    private void OnPlayerConnected(ulong ClientID)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        var car = Instantiate(defaultCarPrefab).GetComponent<NetworkObject>();
        car.SpawnWithOwnership(ClientID);
    }

}
