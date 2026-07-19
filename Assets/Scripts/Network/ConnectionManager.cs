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
    [SerializeField] private string hostIP = "127.0.0.1";
    [SerializeField] private ushort port = 7778;

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

    public static Action onStart;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject); // уничтожаем весь дубликат целиком, а не только этот компонент
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        onStart?.Invoke();

        _networkManager = NetworkManager.Singleton;

        _networkManager.OnServerStarted += OnServerStart;
        _networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;

        var utp = _networkManager.GetComponent<UnityTransport>();
        utp.SetConnectionData(hostIP, port);

        if (BootstrapLoader.ShouldFastConnect)
        {
            //CreateLobby();
        }
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

    public void ChangeIP(string ip)
    {
        Debug.Log("$IP CHANGE!!! {ip}");
        hostIP = ip;
        var utp = _networkManager.GetComponent<UnityTransport>();
        utp.SetConnectionData(hostIP, port);
    }

    public void CreateLobby()
    {
        _networkManager.StartHost();

        SceneLoader.Instance.StartGame();
    }

    public void ConnectLobby()
    {
        _networkManager.StartClient();

        SceneLoader.Instance.StartGame();
    }

    public void Disconnect()
    {
        Debug.Log($"[ConnectionManager] Disconnect() вызван. IsServer={_networkManager.IsServer}, IsClient={_networkManager.IsClient}, IsListening={_networkManager.IsListening}");

        _networkManager.Shutdown();

        Debug.Log("[ConnectionManager] NetworkManager.Shutdown() выполнен, вызываем SceneLoader.LoadMainMenu()...");

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[ConnectionManager] SceneLoader.Instance == null! Убедись, что объект с SceneLoader существует и не был уничтожен.");
            return;
        }

        SceneLoader.Instance.LoadMainMenu();
        GameStateMachine.Instance.ChangeState(GameState.MainMenu);
        ClientEventBus.Instance.InvokeCarOwn(null);
    }

    public void DisconnectPlayer(NetworkObject player)
    {
        // Note: If a client invokes this method, it will throw an exception.
        _networkManager.DisconnectClient(player.OwnerClientId);
    }
}