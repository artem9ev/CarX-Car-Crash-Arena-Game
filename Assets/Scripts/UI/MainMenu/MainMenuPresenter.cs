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
        string nickname = PlayerHandler.Instance.LoadPlayerNick();

        SetNickname(nickname);

        ConnectionManager.Instance.OnClientConnectionNotification += OnClientConnectionNotification;
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
    }

    private void OnClientConnectionNotification(ulong clientID, ConnectionManager.ConnectionState connectionState)
    {
        Debug.Log($"[Client Notification] id: {clientID, 16} | status: {connectionState}");

        StartCoroutine(UpdatePlayersList());
    }    
    
    private IEnumerator UpdatePlayersList()
    {
        yield return new WaitForSeconds(0.2f);

        _lobbyView.Clear();

        Debug.Log("[CONNECTES CLIENTS]");

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


    public void SetNickname(string value)
    {
        _firstScreenView.SetNickName(value);
    }

    private void SaveNickName()
    {
        PlayerHandler.Instance.SavePlayerNick(_firstScreenView.NickName);
    }
}
