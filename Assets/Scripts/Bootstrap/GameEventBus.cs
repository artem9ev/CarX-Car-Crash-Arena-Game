using UnityEngine;

public class GameEventBus : MonoBehaviour
{
    private static GameEventBus _instance;

    public static GameEventBus Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }
}
