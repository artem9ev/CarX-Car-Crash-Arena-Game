using UnityEngine;

public class PlayerLocalSavesHandler : MonoBehaviour
{
    private string _nickname;

    private static PlayerLocalSavesHandler _instance;

    public static PlayerLocalSavesHandler Instance => _instance;

    public string nickname => _nickname;

    private void Awake()
    {
        if (Instance != null) 
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

    public void SavePlayerNick(string nick)
    {
        PlayerPrefs.SetString("PlayerNickname", nick);
        PlayerPrefs.Save();
        _nickname = nick;
    }

    public string LoadPlayerNick()
    {
        _nickname = PlayerPrefs.GetString("PlayerNickname", "Anonimusss");
        return _nickname;
    }
}
