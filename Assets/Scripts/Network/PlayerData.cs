using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System.Collections;

public class PlayerData : NetworkBehaviour
{
    [SerializeField] private MovingCar _defaultCar;

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private string localPlayerName;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            localPlayerName = PlayerPrefs.GetString("Username", PlayerHandler.Instance.nickname);

            SetPlayerNameServerRpc(new FixedString64Bytes(localPlayerName));;
        }

        if (IsServer) 
        {
            NetworkObject car = Instantiate(_defaultCar).GetComponent<NetworkObject>();

            car.SpawnWithOwnership(OwnerClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] // Только владелец может вызвать
    private void SetPlayerNameServerRpc(FixedString64Bytes newName, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        PlayerName.Value = newName;
    }
}