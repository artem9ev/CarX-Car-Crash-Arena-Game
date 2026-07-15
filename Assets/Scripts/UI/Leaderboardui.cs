using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Таблица лидеров дерби: ник, килы, смерти (и вычисляемый счёт).
/// Данные берутся из ScoreManager.Leaderboard (NetworkList), который синхронизируется
/// сервером всем клиентам — поэтому таблица одинаково видна у всех, как в Roblox.
///
/// НАСТРОЙКА В UNITY:
/// 1. Создай Canvas-панель для таблицы (можно скрывать/показывать по Tab, как в шутерах).
/// 2. Внутри создай пустой GameObject "LeaderboardRows" с Vertical Layout Group —
///    это _rowsContainer.
/// 3. Создай префаб одной строки — GameObject с несколькими TextMeshProUGUI
///    (ник, килы, смерти, счёт) или с одним полем на всю строку (см. RowView ниже,
///    можно оставить только _nameText и остальные не назначать, если не нужны).
///    Вынеси в Assets как префаб.
/// 4. Повесь LeaderboardUI на объект в сцене, назначь _rowsContainer и _rowPrefab.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [System.Serializable]
    public class RowView
    {
        public GameObject root;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI killsText;
        public TextMeshProUGUI deathsText;
        public TextMeshProUGUI scoreText;
    }

    [Header("Refs")]
    [SerializeField] private Transform _rowsContainer;
    [SerializeField] private GameObject _rowPrefab; // на префабе должны быть дочерние объекты с именами "Name", "Kills", "Deaths", "Score" (см. FindRowView)

    [Header("Settings")]
    [SerializeField] private bool _sortByKillsDescending = true;
    [SerializeField] private bool _highlightLocalPlayer = true;
    [SerializeField] private Color _localPlayerColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _defaultColor = Color.white;

    private readonly List<GameObject> _spawnedRows = new List<GameObject>();

    /// <summary>
    /// Показывает/скрывает всю таблицу лидеров. Вызывается, например, из MatchResultsUi
    /// при переходе в фазу PostCombat, чтобы таблица не перекрывала панель результатов.
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Leaderboard.OnListChanged -= HandleLeaderboardChanged;
        }
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (ScoreManager.Instance == null)
        {
            yield return null;
        }

        ScoreManager.Instance.Leaderboard.OnListChanged += HandleLeaderboardChanged;
        RebuildRows();
    }

    private void HandleLeaderboardChanged(NetworkListEvent<PlayerboardEntry> changeEvent)
    {
        RebuildRows();
    }

    private void RebuildRows()
    {
        if (_rowsContainer == null || _rowPrefab == null)
        {
            Debug.LogWarning("[LeaderboardUI] _rowsContainer или _rowPrefab не назначены в инспекторе.");
            return;
        }

        foreach (var row in _spawnedRows)
        {
            if (row != null) Destroy(row);
        }
        _spawnedRows.Clear();

        if (ScoreManager.Instance == null) return;

        List<PlayerboardEntry> entries = new List<PlayerboardEntry>();
        foreach (var e in ScoreManager.Instance.Leaderboard)
        {
            entries.Add(e);
        }

        entries = _sortByKillsDescending
            ? entries.OrderByDescending(e => e.Kills).ThenBy(e => e.Deaths).ToList()
            : entries.OrderBy(e => e.PlayerName.ToString()).ToList();

        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

        foreach (var entry in entries)
        {
            GameObject rowObj = Instantiate(_rowPrefab, _rowsContainer);
            rowObj.SetActive(true);
            _spawnedRows.Add(rowObj);

            var row = FindRowView(rowObj);

            int score = entry.Kills - entry.Deaths; // при необходимости поменяй формулу подсчёта очков

            if (row.nameText != null) row.nameText.text = entry.PlayerName.ToString();
            if (row.killsText != null) row.killsText.text = entry.Kills.ToString();
            if (row.deathsText != null) row.deathsText.text = entry.Deaths.ToString();
            if (row.scoreText != null) row.scoreText.text = score.ToString();

            if (_highlightLocalPlayer && entry.ClientId == localClientId)
            {
                SetRowColor(row, _localPlayerColor);
            }
            else
            {
                SetRowColor(row, _defaultColor);
            }
        }
    }

    private void SetRowColor(RowView row, Color color)
    {
        if (row.nameText != null) row.nameText.color = color;
        if (row.killsText != null) row.killsText.color = color;
        if (row.deathsText != null) row.deathsText.color = color;
        if (row.scoreText != null) row.scoreText.color = color;
    }

    /// <summary>
    /// Ищет на префабе строки дочерние TextMeshProUGUI по именам объектов "Name", "Kills",
    /// "Deaths", "Score". Если у тебя другая структура префаба — просто перепиши этот метод
    /// под свою иерархию (или назначай их вручную через список RowView-полей на компоненте).
    /// </summary>
    private RowView FindRowView(GameObject rowObj)
    {
        return new RowView
        {
            root = rowObj,
            nameText = FindChildText(rowObj.transform, "Name"),
            killsText = FindChildText(rowObj.transform, "Kills"),
            deathsText = FindChildText(rowObj.transform, "Deaths"),
            scoreText = FindChildText(rowObj.transform, "Score")
        };
    }

    private TextMeshProUGUI FindChildText(Transform root, string childName)
    {
        Transform t = root.Find(childName);
        if (t == null) return null;
        return t.GetComponent<TextMeshProUGUI>();
    }
}