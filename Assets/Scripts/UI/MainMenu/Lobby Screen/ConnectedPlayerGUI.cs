using TMPro;
using UnityEngine;

public class ConnectedPlayerGUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textNumber;
    [SerializeField] private TextMeshProUGUI _textNickname;

    public void SetNumber(int value)
    {
        if (_textNickname == null)
            return;

        _textNumber.text = value.ToString() + '.';
    }
    public void SetNickname(string value)
    {
        if (_textNickname == null)
            return;

        _textNickname.text = value;
    }
}
