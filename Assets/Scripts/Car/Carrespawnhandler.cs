using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Вешается на тот же GameObject, что и VehicleHealth / MovingCar.
/// При уничтожении машины (VehicleHealth.OnServerDeath):
///  1) сервер забирает владение уничтоженной машиной у игрока (ChangeOwnership -> Server),
///  2) через _respawnDelay секунд для того же клиента спавнится новая машина через SpawnManager.
/// Сама уничтоженная машина (обломки) при этом не уничтожается — ей просто меняется владелец.
/// Если нужно, чтобы обломки исчезали, включите _destroyWreckOnRespawn.
/// </summary>
[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(NetworkObject))]
public class CarRespawnHandler : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float _respawnDelay = 3f;
    [SerializeField] private bool _destroyWreckOnRespawn = false;

    private VehicleHealth _health;

    private void Awake()
    {
        _health = GetComponent<VehicleHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _health.OnServerDeath += HandleServerDeath;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        _health.OnServerDeath -= HandleServerDeath;
    }

    private void HandleServerDeath(ulong attackerClientId)
    {
        if (!IsServer) return;

        // Запоминаем владельца (жертву) ДО того, как заберём владение.
        ulong victimClientId = OwnerClientId;

        // Сразу фиксируем убийство/смерть в статистике — не дожидаясь респавна.
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterKill(attackerClientId, victimClientId);
        }

        StartCoroutine(RespawnRoutine(victimClientId));
    }

    private IEnumerator RespawnRoutine(ulong ownerClientId)
    {
        // Забираем владение у игрока сразу — машина становится "серверной" (обломки/труп машины).
        if (NetworkObject != null && NetworkObject.IsSpawned && NetworkObject.OwnerClientId != NetworkManager.ServerClientId)
        {
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        yield return new WaitForSeconds(_respawnDelay);

        // Проверяем, что клиент всё ещё подключён, прежде чем спавнить ему новую машину.
        if (NetworkManager.Singleton.ConnectedClientsIds.Contains(ownerClientId))
        {
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnCarForClient(ownerClientId);
            }
            else
            {
                Debug.LogError("[CarRespawnHandler] SpawnManager.Instance не найден в сцене.");
            }
        }

        if (_destroyWreckOnRespawn && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}