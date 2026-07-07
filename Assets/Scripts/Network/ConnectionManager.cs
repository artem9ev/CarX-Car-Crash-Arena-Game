using System;
using System.Data;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class ConnectionManager : MonoBehaviour
{
    private NetworkManager _networkManager;

    public event Action<ulong, ConnectionState> OnClientConnectionNotification;

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    private static ConnectionManager _instance;
    public static ConnectionManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _networkManager = NetworkManager.Singleton;

        _networkManager.OnServerStarted += OnServerStart;
        _networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;

        var utp = _networkManager.GetComponent<UnityTransport>();
        utp.SetConnectionData("127.0.0.1", 7778 );
    }

    private void OnDestroy()
    {
        _networkManager.OnServerStarted -= OnServerStart;
        _networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    private void OnServerStart()
    {
        Debug.Log("Server Start");
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionState.Connected);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionState.Disconnected);
    }

    public void CreateLobby()
    {
        _networkManager.StartHost();

        SceneLoader.Instance.LoadLobby();
    }

    public void ConnectLobby()
    {
        _networkManager.StartClient();
    }

    public void Disconnect()
    {
        _networkManager.Shutdown();

        SceneLoader.Instance.LoadMainMenu();
    }

    public void DisconnectPlayer(NetworkObject player)
    {
        // Note: If a client invokes this method, it will throw an exception.
        _networkManager.DisconnectClient(player.OwnerClientId);
    }
}
