using Unity.Netcode;

public class LobbyHandler : NetworkBehaviour
{
    private static LobbyHandler _instance;

    public static LobbyHandler Instance => _instance;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer || IsHost)
        {
            /*NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;*/
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer || IsHost)
        {
            /*NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;*/
        }
    }
}
