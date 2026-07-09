using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : BaseViewUI
{
    [SerializeField] private ConnectedPlayerGUI _playerPrefabGUI;
    [SerializeField] private VerticalLayoutGroup _root;

    private List<ConnectedPlayerGUI> _playersGUIs = new List<ConnectedPlayerGUI>();

    public void UpdatePlayerNickname(ulong clientID, string nickname)
    {
        foreach (var player in _playersGUIs) 
        {
            if (player.ClientID == clientID)
            {
                player.SetNickname(nickname);
            }
        }
    }

    public void Add(ulong clientID, string nickname)
    {
        ConnectedPlayerGUI playerGUI = Instantiate(_playerPrefabGUI, _root.transform);

        playerGUI.SetNickname(nickname);
        playerGUI.SetClient(clientID);

        _playersGUIs.Add(playerGUI);
    }

    public void Remove(ulong clientID)
    {
        foreach (var playerGUI in _playersGUIs) 
        {
            if (playerGUI.ClientID == clientID)
            {
                _playersGUIs.Remove(playerGUI);
                Destroy(playerGUI);
                return;
            }
        }
    }

    public void Clear()
    {
        for (int i = _playersGUIs.Count - 1; i >= 0; i--)
        {
            Destroy(_playersGUIs[i].gameObject);
        }
        _playersGUIs.Clear();
    }
}
