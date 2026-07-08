using TMPro;
using UnityEngine;

public class ConnectedPlayerGUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textID;
    [SerializeField] private TextMeshProUGUI _textNickname;

    private ulong _clientID;

    public ulong ClientID => _clientID;

    public void SetClient(ulong value)
    {
        if (_textNickname == null)
            return;

        _clientID = value;
        _textID.text = value.ToString() + '.';
    }
    public void SetNickname(string value)
    {
        if (_textNickname == null)
            return;

        _textNickname.text = value;
    }
}
