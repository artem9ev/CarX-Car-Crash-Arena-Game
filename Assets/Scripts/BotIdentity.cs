using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Вешается на префаб бота (defaultBotPrefab), рядом с VehicleHealth и SmartBotAI.
///
/// ВАЖНО: у бота при каждой смерти создаётся НОВЫЙ NetworkObject (новая машина),
/// поэтому его статистику/id нельзя привязывать к NetworkObjectId — он меняется
/// при каждом респавне. Вместо этого у бота есть стабильный "слот" (SlotId),
/// который назначается через AssignSlot() сразу после Instantiate (до Spawn())
/// и передаётся тем же самым при респавне — так статистика (Kills/Deaths)
/// продолжает копиться для "того же" бота, а не обнуляется каждую смерть.
///
/// Статистика хранится в статическом реестре BotStatsRegistry — он не привязан
/// к жизненному циклу конкретного NetworkObject и переживает респавн бота.
/// </summary>
[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(NetworkObject))]
public class BotIdentity : NetworkBehaviour
{
    [SerializeField] private string _displayNamePrefix = "Bot";

    /// <summary>Стабильный номер бота, не меняется между респавнами. Назначается извне через AssignSlot().</summary>
    public int SlotId { get; private set; } = -1;

    /// <summary>
    /// Псевдо-ClientId для статистики/лидерборда — строится от SlotId (не от NetworkObjectId!),
    /// поэтому остаётся одинаковым для "того же" бота между респавнами.
    /// Строится "с конца минус 1" диапазона ulong: именно "минус 1", а не просто
    /// ulong.MaxValue - SlotId, потому что при SlotId == 0 (первый бот) результат
    /// был бы РОВНО ulong.MaxValue — а это значение везде в коде (VehicleHealth,
    /// ScoreManager) зарезервировано как сентинел "атакующего нет / окружение".
    /// Без этого сдвига killы от первого заспавненного бота никогда не засчитывались бы.
    /// </summary>
    public ulong PseudoClientId => ulong.MaxValue - 1 - (ulong)SlotId;

    public string DisplayName => $"{_displayNamePrefix} #{SlotId}";

    public int Kills => BotStatsRegistry.GetStats(SlotId).kills;
    public int Deaths => BotStatsRegistry.GetStats(SlotId).deaths;
    public int Score => BotStatsRegistry.GetStats(SlotId).score;

    /// <summary>
    /// Назначает боту стабильный слот. Вызывать сразу после Instantiate, ДО Spawn() —
    /// см. SpawnManager.SpawnBot(). Если не вызвать — бот получит слот 0 по умолчанию,
    /// что приведёт к конфликту, если ботов несколько.
    /// </summary>
    public void AssignSlot(int slotId)
    {
        SlotId = slotId;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        BotRegistry.ById[PseudoClientId] = this;
        StartCoroutine(RegisterWhenReady());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        BotRegistry.ById.Remove(PseudoClientId);
        // Запись в Leaderboard НЕ удаляем — при респавне тот же PseudoClientId
        // просто обновит существующую строку (UpsertEntry). Если бот не респавнится
        // (например, дерби закончилось), можно вызвать ScoreManager.Instance?.RemoveEntry(PseudoClientId)
        // отдельно — например, из SpawnManager при остановке раунда.
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ScoreManager.Instance == null)
        {
            yield return null;
        }

        var stats = BotStatsRegistry.GetStats(SlotId);
        ScoreManager.Instance.UpsertEntry(PseudoClientId, DisplayName, stats.kills, stats.deaths, stats.score);
    }

    /// <summary>Вызывается ScoreManager'ом, когда этот бот кого-то убил.</summary>
    public void OnRegisteredAsKiller() => BotStatsRegistry.AddKill(SlotId);

    /// <summary>Вызывается ScoreManager'ом, когда этот бот погиб.</summary>
    public void OnRegisteredAsVictim() => BotStatsRegistry.AddDeath(SlotId);
}

/// <summary>Реестр всех живых ботов по их PseudoClientId (для быстрого поиска в ScoreManager). Актуален только на сервере.</summary>
public static class BotRegistry
{
    public static readonly Dictionary<ulong, BotIdentity> ById = new Dictionary<ulong, BotIdentity>();
}

/// <summary>
/// Статистика ботов по стабильному SlotId. Живёт независимо от NetworkObject —
/// не сбрасывается при респавне бота, только при остановке/перезапуске сервера.
/// </summary>
public static class BotStatsRegistry
{
    private static readonly Dictionary<int, (int kills, int deaths, int score)> _stats = new Dictionary<int, (int kills, int deaths, int score)>();

    public static (int kills, int deaths, int score) GetStats(int slotId)
    {
        return _stats.TryGetValue(slotId, out var stats) ? stats : (0, 0, 0);
    }

    public static void AddKill(int slotId)
    {
        var stats = GetStats(slotId);
        _stats[slotId] = (stats.kills + 1, stats.deaths, stats.score);
    }

    public static void AddDeath(int slotId)
    {
        var stats = GetStats(slotId);
        _stats[slotId] = (stats.kills, stats.deaths + 1, stats.score);
    }

    /// <summary>Сбросить статистику всех ботов (например, при старте нового раунда/матча).</summary>
    public static void ResetAll()
    {
        _stats.Clear();
    }
}