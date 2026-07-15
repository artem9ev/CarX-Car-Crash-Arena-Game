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
/// (например, из CarRespawnHandler.HandleServerDeath). Работает как для игроков
/// (через PlayerData.ByClientId), так и для ботов (через BotRegistry.ById) —
/// метод сам определяет, кто есть кто, по переданному id.
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

    /// <summary>
    /// Синхронизируемый по сети список записей лидерборда (ник, килы, смерти).
    /// Пишет только сервер, читают все клиенты — подписывайтесь на Leaderboard.OnListChanged для UI.
    /// </summary>
    public readonly NetworkList<PlayerboardEntry> Leaderboard = new NetworkList<PlayerboardEntry>();

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
    /// Работает как для игроков, так и для ботов — для ботов передавайте
    /// BotIdentity.PseudoClientId вместо OwnerClientId (см. CarRespawnHandler).
    /// </summary>
    /// <param name="attackerClientId">
    /// ClientId/PseudoClientId убийцы. Передайте ulong.MaxValue, если машина погибла
    /// не от другого игрока/бота (например, от окружения) — засчитается только смерть, без килла.
    /// </param>
    /// <param name="victimClientId">ClientId/PseudoClientId жертвы.</param>
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
            UpsertEntry(victimClientId, victimName, victimData.Kills.Value, victimData.Deaths.Value);
        }
        else if (BotRegistry.ById.TryGetValue(victimClientId, out var victimBot))
        {
            victimBot.OnRegisteredAsVictim();
            victimName = victimBot.DisplayName;
            UpsertEntry(victimClientId, victimName, victimBot.Kills, victimBot.Deaths);
        }

        string attackerName = isEnvironmentKill ? "Environment" : victimName;
        if (!isEnvironmentKill && !isSuicide)
        {
            if (PlayerData.ByClientId.TryGetValue(attackerClientId, out var attackerData))
            {
                attackerData.Kills.Value++;
                attackerName = attackerData.PlayerName.Value.ToString();
                UpsertEntry(attackerClientId, attackerName, attackerData.Kills.Value, attackerData.Deaths.Value);
            }
            else if (BotRegistry.ById.TryGetValue(attackerClientId, out var attackerBot))
            {
                attackerBot.OnRegisteredAsKiller();
                attackerName = attackerBot.DisplayName;
                UpsertEntry(attackerClientId, attackerName, attackerBot.Kills, attackerBot.Deaths);
                Debug.Log($"[ScoreManager] Килл засчитан боту {attackerBot.DisplayName} (slot {attackerBot.SlotId}), теперь Kills={attackerBot.Kills}");
            }
            else
            {
                Debug.LogWarning($"[ScoreManager] attackerClientId={attackerClientId} не найден ни в PlayerData.ByClientId, ни в BotRegistry.ById — килл никому не засчитан. Проверь, есть ли BotIdentity на префабе бота.");
            }
        }

        OnKillRegisteredServer?.Invoke(attackerClientId, victimClientId);

        BroadcastKillFeedRpc(new FixedString64Bytes(attackerName), new FixedString64Bytes(victimName), isSuicide || isEnvironmentKill);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastKillFeedRpc(FixedString64Bytes attackerName, FixedString64Bytes victimName, bool isSuicide)
    {
        OnKillFeed?.Invoke(attackerName.ToString(), victimName.ToString(), isSuicide);
    }

    /// <summary>
    /// Добавляет или обновляет запись игрока/бота в таблице лидеров. Вызывать ТОЛЬКО на сервере.
    /// Вызывается автоматически из RegisterKill, а также из PlayerData/BotIdentity при спавне/смене ника.
    /// </summary>
    public void UpsertEntry(ulong clientId, string playerName, int kills, int deaths)
    {
        if (!IsServer) return;

        var entry = new PlayerboardEntry
        {
            ClientId = clientId,
            PlayerName = new FixedString64Bytes(playerName),
            Kills = kills,
            Deaths = deaths
        };

        for (int i = 0; i < Leaderboard.Count; i++)
        {
            if (Leaderboard[i].ClientId == clientId)
            {
                Leaderboard[i] = entry; // NetworkList требует именно переприсвоения элемента, а не мутации поля
                return;
            }
        }

        Leaderboard.Add(entry);
    }

    /// <summary>Убирает игрока/бота из таблицы лидеров (например, при отключении/деспавне). Вызывать ТОЛЬКО на сервере.</summary>
    public void RemoveEntry(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < Leaderboard.Count; i++)
        {
            if (Leaderboard[i].ClientId == clientId)
            {
                Leaderboard.RemoveAt(i);
                return;
            }
        }
    }
}

/// <summary>Одна строка таблицы лидеров, синхронизируется по сети через NetworkList.</summary>
public struct PlayerboardEntry : INetworkSerializable, System.IEquatable<PlayerboardEntry>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public int Kills;
    public int Deaths;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Kills);
        serializer.SerializeValue(ref Deaths);
    }

    public bool Equals(PlayerboardEntry other)
    {
        return ClientId == other.ClientId
            && PlayerName.Equals(other.PlayerName)
            && Kills == other.Kills
            && Deaths == other.Deaths;
    }
}