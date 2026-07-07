using System;
using Unity.Netcode;
using UnityEngine;

public class MainMenuPresenter : MonoBehaviour
{
    [SerializeField] private FirstScreenView _firstScreenView;
    [SerializeField] private LobbyView _lobbyView;

    private void Start()
    {
        string nickname = PlayerHandler.Instance.LoadPlayerNick();

        SetNickname(nickname);

        ConnectionManager.Instance.OnClientConnectionNotification += OnClientConnectionNotification;
        LobbyHandler.Instance.LobbyPlayers.OnListChanged += OnLobbyListChanged;
    }

    private void OnEnable()
    {

        _firstScreenView.onSaveNickname += SaveNickName;
    }

    private void OnDisable()
    {
        _firstScreenView.onSaveNickname -= SaveNickName;
    }

    private void OnDestroy()
    {
        ConnectionManager.Instance.OnClientConnectionNotification -= OnClientConnectionNotification;
        LobbyHandler.Instance.LobbyPlayers.OnListChanged -= OnLobbyListChanged;
    }

    private void OnClientConnectionNotification(ulong clientID, ConnectionManager.ConnectionState connectionState)
    {
        Debug.Log($"[Client Notification] id: {clientID, 16} | status: {connectionState}");
    }


    private void OnLobbyListChanged(NetworkListEvent<LobbyPlayerInfo> changeEvent)
    {
        Debug.Log("LOBBY LIST CHANGED");
        switch (changeEvent.Type)
        {
            case NetworkListEvent<LobbyPlayerInfo>.EventType.Add:
                Debug.Log($"Name: {changeEvent.Value.Nickname.ToString()}");
                _lobbyView.Add(changeEvent.Value.ClientId, changeEvent.Value.Nickname.ToString());
                break;
            case NetworkListEvent<LobbyPlayerInfo>.EventType.Insert:
                break;
            case NetworkListEvent<LobbyPlayerInfo>.EventType.Remove:
                _lobbyView.Remove(changeEvent.Value.ClientId);
                break;
            case NetworkListEvent<LobbyPlayerInfo>.EventType.RemoveAt:
                break;
            case NetworkListEvent<LobbyPlayerInfo>.EventType.Value:
                break;
            case NetworkListEvent<LobbyPlayerInfo>.EventType.Clear:
                _lobbyView.Clear();
                break;
            case NetworkListEvent<LobbyPlayerInfo>.EventType.Full:
                break;
            default:
                break;
        }
    }

    public void SetNickname(string value)
    {
        _firstScreenView.SetNickName(value);
    }

    private void SaveNickName()
    {
        PlayerHandler.Instance.SavePlayerNick(_firstScreenView.NickName);
    }
}
