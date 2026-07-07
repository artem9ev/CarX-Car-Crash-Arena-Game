using Unity.Netcode;
using UnityEngine;

public class LobbyHandler : NetworkBehaviour
{
    // NetworkList для хранения данных всех игроков. Синхронизируется автоматически.
    public NetworkList<LobbyPlayerInfo> LobbyPlayers = new NetworkList<LobbyPlayerInfo>();

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

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Network spawn!");

        if (IsServer || IsHost)
        {
            // Подписываемся на события подключения/отключения
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            Debug.Log("I'm a server!!!");

            // Если это хост или сервер, инициализируем список
            // Можно добавить всех уже подключенных игроков
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                // Для демонстрации добавляем заглушку. В реальности нужно получить данные от клиента.
                // LobbyPlayers.Add(new LobbyPlayerInfo { ClientId = client.Key, PlayerName = "Player " + client.Key });
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer || IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsClient) return;

        // Здесь нужно получить ник нового игрока, например, запросив его через RPC или из PlayerData.
        // Пока добавим заглушку.
        Debug.Log($"Player {clientId} connected. Total players: {LobbyPlayers.Count}");

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            string nickname = client.PlayerObject.GetComponent<PlayerData>().PlayerName.Value.ToString();

            LobbyPlayers.Add(new LobbyPlayerInfo { ClientId = clientId, PlayerName = nickname});
        }
        else 
        {
            LobbyPlayers.Add(new LobbyPlayerInfo { ClientId = clientId, PlayerName = "Loading..." });
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsClient) return;

        // Удаляем игрока из списка
        for (int i = LobbyPlayers.Count - 1; i >= 0; i--)
        {
            if (LobbyPlayers[i].ClientId == clientId)
            {
                LobbyPlayers.RemoveAt(i);
                break;
            }
        }
        Debug.Log($"Player {clientId} disconnected. Total players: {LobbyPlayers.Count}");
    }
}
