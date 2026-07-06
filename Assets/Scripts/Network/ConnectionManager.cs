using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class ConnectionManager : MonoBehaviour
{
    private string _profileName;
    private string _sessionName;
    private int _maxPlayers = 10;
    private ConnectionState _state = ConnectionState.Disconnected;
    //private ISession _session;
    private NetworkManager m_NetworkManager;

    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    private NetworkManager _networkManager;

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
        _networkManager = GetComponent<NetworkManager>();

        /*_networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        _networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        await UnityServices.InitializeAsync();*/
    }

    private void Start()
    {

    }
}
