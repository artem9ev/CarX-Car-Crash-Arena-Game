using UnityEngine;

public class MainMenuPresenter : MonoBehaviour
{
    private void Start()
    {
        ConnectionManager.Instance.OnClientConnectionNotification += OnClientConnectionNotification;
    }

    private void OnDestroy()
    {
        ConnectionManager.Instance.OnClientConnectionNotification -= OnClientConnectionNotification;
    }

    private void OnClientConnectionNotification(ulong clientID, ConnectionManager.ConnectionState connectionState)
    {
        Debug.Log($"[Client Notification] id: {clientID, 16} | status: {connectionState}");
    }
}
