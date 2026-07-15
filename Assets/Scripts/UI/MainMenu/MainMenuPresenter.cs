using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MainMenuPresenter : PresenterUI
{
    [SerializeField] private FirstScreenView _firstScreenView;
    [SerializeField] private LobbyView _lobbyView;

    private void Awake()
    {
        AddView(_firstScreenView);
        AddView(_lobbyView);
    }

    private void Start()
    {
        string nickname = PlayerLocalSavesHandler.Instance.LoadPlayerNick();

        SetNickname(nickname);

        ConnectionManager.Instance.OnClientConnectionNotification += OnClientConnectionNotification;
    }

    private void OnDestroy()
    {
        ConnectionManager.Instance.OnClientConnectionNotification -= OnClientConnectionNotification;
    }

    public override void Subscribe()
    {
        _firstScreenView.onSaveNickname += SaveNickName;
        _firstScreenView.onCreateLobby += ButtonCreateLobbyClick;
        _firstScreenView.onConnectLobby += ButtonConnectLobbyClick;

        _firstScreenView.onIPChange += OnIPChage;

        ActivateView(_firstScreenView);
    }

    public override void Unsubscribe()
    {
        _firstScreenView.onSaveNickname -= SaveNickName;
        _firstScreenView.onCreateLobby -= ButtonCreateLobbyClick;
        _firstScreenView.onConnectLobby -= ButtonConnectLobbyClick;

        _firstScreenView.onIPChange -= OnIPChage;
    }

    private void OnClientConnectionNotification(ulong clientID, ConnectionManager.ConnectionState connectionState)
    {
        switch (connectionState)
        {
            case ConnectionManager.ConnectionState.Disconnected:
                _firstScreenView.ActivateConnectionButtons(true);
                break;
            case ConnectionManager.ConnectionState.Connecting:
                
                break;
            case ConnectionManager.ConnectionState.Connected:
                ActivateView(_lobbyView);
                break;
        }

        StartCoroutine(UpdatePlayersList());
    }    

    private void OnIPChage(string ip)
    {
        ConnectionManager.Instance.ChangeIP(ip);
    }
    
    private IEnumerator UpdatePlayersList()
    {
        yield return new WaitForSeconds(0.2f);

        _lobbyView.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (!client.Value.PlayerObject.TryGetComponent(out PlayerData data))
            {
                continue;
            }

            string nickname = data.PlayerName.Value.ToString();

            _lobbyView.Add(client.Key, nickname);
        }
    }

    private void ButtonConnectLobbyClick()
    {
        ConnectionManager.Instance.ConnectLobby();
    }

    private void ButtonCreateLobbyClick()
    {
        ConnectionManager.Instance.CreateLobby();
    }

    private void SaveNickName()
    {
        PlayerLocalSavesHandler.Instance.SavePlayerNick(_firstScreenView.NickName);
    }

    public void SetNickname(string value)
    {
        _firstScreenView.SetNickName(value);
    }
}
