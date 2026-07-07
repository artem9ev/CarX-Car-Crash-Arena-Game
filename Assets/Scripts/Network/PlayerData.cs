using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    // NetworkVariable для ника, доступный всем для чтения, но записывает только сервер
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Сохраняем ник локально, чтобы не потерять после отправки
    private string localPlayerName;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Получаем ник из PlayerPrefs или генерируем
            localPlayerName = PlayerPrefs.GetString("Username", PlayerHandler.Instance.nickname);

            SetPlayerNameServerRpc(new FixedString64Bytes(localPlayerName));

            // Подписываемся на изменение ника для обновления UI
            //PlayerName.OnValueChanged += OnPlayerNameChanged;
        }

        // Обновляем UI при спавне (для всех клиентов)
        //UpdateNameUI(PlayerName.Value.ToString());
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] // Только владелец может вызвать
    public void SetPlayerNameServerRpc(FixedString64Bytes newName, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        PlayerName.Value = newName;
    }
}