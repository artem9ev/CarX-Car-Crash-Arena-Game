using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Разместите один экземпляр этого компонента в сцене (in-scene placed NetworkObject),
/// рядом с NetworkManager / SpawnManager / ClientEventBus.
///
/// Логика подсчёта (Kills/Deaths) выполняется ТОЛЬКО на сервере — вызывайте
/// RegisterKill(attackerClientId, victimClientId) из серверного кода
/// (например, из CarRespawnHandler.HandleServerDeath).
///
/// Рассылка kill-feed идёт через Rpc всем клиентам, чтобы показать сообщение
/// в UI ("Игрок A уничтожил Игрока B").
/// </summary>
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>
    /// Локальное клиентское событие для UI: атакующий, жертва, суицид/урон от окружения.
    /// Вызывается на всех клиентах (и хосте) после подтверждения сервером.
    /// </summary>
    public event UnityAction<string, string, bool> OnKillFeed;

    /// <summary>Локальное серверное событие — удобно для логирования / бэкенда статистики.</summary>
    public event UnityAction<ulong, ulong> OnKillRegisteredServer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Регистрирует убийство. Вызывать ТОЛЬКО на сервере.
    /// </summary>
    /// <param name="attackerClientId">
    /// ClientId убийцы. Передайте ulong.MaxValue, если машина погибла не от другого игрока
    /// (например, от окружения) — засчитается только смерть, без килла.
    /// </param>
    /// <param name="victimClientId">ClientId жертвы.</param>
    public void RegisterKill(ulong attackerClientId, ulong victimClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ScoreManager] RegisterKill вызван не на сервере — игнорируется.");
            return;
        }

        bool isEnvironmentKill = attackerClientId == ulong.MaxValue;
        bool isSuicide = !isEnvironmentKill && attackerClientId == victimClientId;

        string victimName = "Unknown";
        if (PlayerData.ByClientId.TryGetValue(victimClientId, out var victimData))
        {
            victimData.Deaths.Value++;
            victimName = victimData.PlayerName.Value.ToString();
        }

        string attackerName = isEnvironmentKill ? "Environment" : victimName;
        if (!isEnvironmentKill && !isSuicide && PlayerData.ByClientId.TryGetValue(attackerClientId, out var attackerData))
        {
            attackerData.Kills.Value++;
            attackerName = attackerData.PlayerName.Value.ToString();
        }

        OnKillRegisteredServer?.Invoke(attackerClientId, victimClientId);

        BroadcastKillFeedRpc(new FixedString64Bytes(attackerName), new FixedString64Bytes(victimName), isSuicide || isEnvironmentKill);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastKillFeedRpc(FixedString64Bytes attackerName, FixedString64Bytes victimName, bool isSuicide)
    {
        OnKillFeed?.Invoke(attackerName.ToString(), victimName.ToString(), isSuicide);
    }
}