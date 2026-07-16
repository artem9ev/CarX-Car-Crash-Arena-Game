using System.Collections;
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
    public NetworkVariable<int> Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Реестр PlayerData по ClientId, актуален только на сервере.</summary>
    public static readonly Dictionary<ulong, PlayerData> ByClientId = new Dictionary<ulong, PlayerData>();

    private string localPlayerName;
    private Coroutine _registerRoutine;

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

            PlayerName.OnValueChanged += OnPlayerNameChanged;

            // ScoreManager — тоже in-scene NetworkObject, и порядок спавна между ним
            // и игроком не гарантирован: если PlayerData заспавнится раньше ScoreManager,
            // Instance тут ещё будет null. Поэтому ждём его появления отдельной корутиной,
            // вместо однократного ScoreManager.Instance?.UpsertEntry(...).
            _registerRoutine = StartCoroutine(RegisterInLeaderboardWhenReady());

            //NetworkObject car = Instantiate(_defaultCar).GetComponent<NetworkObject>();

            //car.SpawnWithOwnership(OwnerClientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (_registerRoutine != null)
            {
                StopCoroutine(_registerRoutine);
                _registerRoutine = null;
            }

            ByClientId.Remove(OwnerClientId);
            ScoreManager.Instance?.RemoveEntry(OwnerClientId);
            PlayerName.OnValueChanged -= OnPlayerNameChanged;
        }
    }

    private IEnumerator RegisterInLeaderboardWhenReady()
    {
        while (ScoreManager.Instance == null)
        {
            yield return null;
        }

        ScoreManager.Instance.UpsertEntry(OwnerClientId, PlayerName.Value.ToString(), Kills.Value, Deaths.Value, Score.Value);
        _registerRoutine = null;
    }

    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        if (!IsServer) return;

        ScoreManager.Instance?.UpsertEntry(OwnerClientId, newValue.ToString(), Kills.Value, Deaths.Value, Score.Value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] // Только владелец может вызвать
    private void SetPlayerNameServerRpc(FixedString64Bytes newName, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        PlayerName.Value = newName;
    }
}