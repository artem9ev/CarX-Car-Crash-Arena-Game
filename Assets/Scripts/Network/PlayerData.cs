using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [SerializeField] private MovingCar _defaultCar;

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Дерби-статистика")]
    public NetworkVariable<int> Kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Deaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Реестр PlayerData по ClientId, актуален только на сервере.</summary>
    public static readonly Dictionary<ulong, PlayerData> ByClientId = new Dictionary<ulong, PlayerData>();

    private string localPlayerName;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            localPlayerName = PlayerPrefs.GetString("Username", PlayerLocalSavesHandler.Instance.nickname);

            SetPlayerNameServerRpc(new FixedString64Bytes(localPlayerName)); ;
        }

        if (IsServer)
        {
            ByClientId[OwnerClientId] = this;

            //NetworkObject car = Instantiate(_defaultCar).GetComponent<NetworkObject>();

            //car.SpawnWithOwnership(OwnerClientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            ByClientId.Remove(OwnerClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] // Только владелец может вызвать
    private void SetPlayerNameServerRpc(FixedString64Bytes newName, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        PlayerName.Value = newName;
    }
}