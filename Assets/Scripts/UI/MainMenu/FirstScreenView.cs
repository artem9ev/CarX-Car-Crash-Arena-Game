using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FirstScreenView : MonoBehaviour, IFirstScreenView
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField _nicknameField;
    [SerializeField] private Button _buttonCreateLobby;
    [SerializeField] private Button _buttonConnectLobby;
    [SerializeField] private TextMeshProUGUI _textNicknameWarning;
    [Header("Nickname Warnings")]
    [SerializeField] private string _warningNicknameSize = "Nick name lenghth should be > 3 and < 21";

    public UnityAction<string> onNicknameChange;
    public UnityAction onConnectLobby;
    public UnityAction onCreateLobby;
    public UnityAction onSaveNickname;

    public string NickName => _nicknameField.text;

    private void OnEnable()
    {
        _buttonCreateLobby.onClick.AddListener(CreateLobby);
        _buttonConnectLobby.onClick.AddListener(ConnectToLobby);

        _nicknameField.onValueChanged.AddListener(OnNicknameChange);
    }

    private void OnDisable()
    {
        _buttonCreateLobby.onClick.RemoveListener(CreateLobby);
        _buttonConnectLobby.onClick.RemoveListener(ConnectToLobby);
    }

    private void OnNicknameChange(string value)
    {
        if (value.Length > 20)
        {
            _nicknameField.text = value.Substring(0, 20);
        }

        if (value.Length < 4)
        {
            SetNetworkButtonsInteractable(false);
        }
        else 
        {
            onNicknameChange?.Invoke(_nicknameField.text);
            SetNetworkButtonsInteractable(true);
        }
    }

    private void SetNetworkButtonsInteractable(bool value)
    {
        _buttonCreateLobby.interactable = value;
        _buttonConnectLobby.interactable = value;
    }

    private void SaveNickName()
    {
        onSaveNickname?.Invoke();
    }

    public void SetNickName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        _nicknameField.text = value;
    }

    public void CreateLobby()
    {
        SaveNickName();
        ConnectionManager.Instance.CreateLobby();
    }

    public void ConnectToLobby()
    {
        SaveNickName();
        ConnectionManager.Instance.ConnectLobby();
    }

    
}
