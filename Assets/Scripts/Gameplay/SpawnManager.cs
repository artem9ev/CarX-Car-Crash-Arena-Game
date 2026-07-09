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

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong сlientID)
    {
        Debug.Log("[CONNECTED] Try to spawn player car");

        if (!NetworkManager.Singleton.IsServer) return;

        var car = Instantiate(defaultCarPrefab).GetComponent<NetworkObject>();
        car.SpawnWithOwnership(сlientID);
    }

}
