using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MainMenuPresenter : MonoBehaviour
{
    [SerializeField] private FirstScreenView _firstScreenView;
    [SerializeField] private LobbyView _lobbyView;

    private void Start()
    {
        string nickname = PlayerLocalSavesHandler.Instance.LoadPlayerNick();

        SetNickname(nickname);

        ConnectionManager.Instance.OnClientConnectionNotification += OnClientConnectionNotification;
    }


    private void OnEnable()
    {
        _firstScreenView.onSaveNickname += SaveNickName;
        _firstScreenView.onCreateLobby += ButtonCreateLobbyClick;
        _firstScreenView.onConnectLobby += ButtonConnectLobbyClick;
    }

    private void OnDisable()
    {
        _firstScreenView.onSaveNickname -= SaveNickName;
        _firstScreenView.onCreateLobby -= ButtonCreateLobbyClick;
        _firstScreenView.onConnectLobby -= ButtonConnectLobbyClick;
    }

    private void OnDestroy()
    {
        ConnectionManager.Instance.OnClientConnectionNotification -= OnClientConnectionNotification;
    }

    private void ActivateFirstScreen()
    {
        _firstScreenView.Activate();
        _lobbyView.Deactivate();
    }

    private void ActivateLobbyScreen()
    {
        _firstScreenView.Deactivate();
        _lobbyView.Activate();
    }

    private void OnClientConnectionNotification(ulong clientID, ConnectionManager.ConnectionState connectionState)
    {
        Debug.Log($"[Client Notification] id: {clientID, 16} | status: {connectionState}");

        switch (connectionState)
        {
            case ConnectionManager.ConnectionState.Disconnected:
                _firstScreenView.ActivateConnectionButtons(true);
                break;
            case ConnectionManager.ConnectionState.Connecting:
                
                break;
            case ConnectionManager.ConnectionState.Connected:
                ActivateLobbyScreen();
                break;
        }

        StartCoroutine(UpdatePlayersList());
    }    
    
    private IEnumerator UpdatePlayersList()
    {
        yield return new WaitForSeconds(0.2f);

        _lobbyView.Clear();

        Debug.Log("[CONNECTED CLIENTS: ]");

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            Debug.Log($"Connected client: {client.Key}");
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
