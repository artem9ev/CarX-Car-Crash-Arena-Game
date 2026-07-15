using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Панель результатов конца матча. Во время фазы Combat (сражение) показывает
/// оставшееся время таймера матча; при переходе в PostCombat показывает панель
/// с итоговыми результатами — берёт те же данные, что и обычная LeaderboardUI
/// (ScoreManager.Instance.Leaderboard), но отсортированные по кол-ву убийств
/// и с подсветкой топ-3 мест.
///
/// НАСТРОЙКА В UNITY (независимо от LeaderboardUI):
/// 1. _resultsPanel — отдельная панель результатов, должна быть выключена
///    по умолчанию в сцене (включается на фазе PostCombat).
/// 2. _rowsContainer — контейнер с Vertical Layout Group внутри панели.
/// 3. _rowPrefab — префаб строки. Дочерние объекты (такие же по названиям как
///    в LeaderboardUI): "Rank", "Name", "Kills", "Deaths", "Score" с TextMeshProUGUI.
/// 4. _timerText — необязательный текст таймера обратного отсчёта матча.
/// 5. _leaderboardUI — ссылка на обычную LeaderboardUI, чтобы скрывать её
///    на время показа панели результатов (см. HandlePhaseChanged).
/// </summary>
public class MatchResultsUi : MonoBehaviour
{
    [System.Serializable]
    public class RowView
    {
        public GameObject root;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI killsText;
        public TextMeshProUGUI deathsText;
        public TextMeshProUGUI scoreText;
    }

    [Header("Компоненты")]
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private Transform _rowsContainer;
    [SerializeField] private GameObject _rowPrefab;

    [Header("Таблица лидеров (скрывается на время показа результатов)")]
    [SerializeField] private LeaderboardUI _leaderboardUI;

    [Header("Таймер матча (опционально)")]
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("Цвета мест")]
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _localPlayerColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _firstPlaceColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color _secondPlaceColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color _thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f);

    private readonly List<GameObject> _spawnedRows = new List<GameObject>();

    private void OnEnable()
    {
        if (_resultsPanel != null)
            _resultsPanel.SetActive(false);

        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (MatchManager.Instance != null)
            MatchManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (MatchManager.Instance == null)
            yield return null;

        MatchManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(MatchManager.Instance.Phase);
    }

    private void Update()
    {
        if (_timerText == null || MatchManager.Instance == null) return;
        if (MatchManager.Instance.Phase != MatchPhase.Combat) return;

        float t = Mathf.Max(0f, MatchManager.Instance.TimeRemaining);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        _timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void HandlePhaseChanged(MatchPhase newPhase)
    {
        bool showResults = newPhase == MatchPhase.PostCombat;

        if (_resultsPanel != null)
            _resultsPanel.SetActive(showResults);

        // Прячем обычный лидерборд на время показа панели результатов
        // (и возвращаем обратно, если матч почему-то вернулся в Combat).
        if (_leaderboardUI != null)
            _leaderboardUI.SetVisible(!showResults);

        if (showResults)
            RebuildResults();
    }

    private void RebuildResults()
    {
        if (_rowsContainer == null || _rowPrefab == null)
        {
            Debug.LogWarning("[MatchResultsUI] _rowsContainer или _rowPrefab не назначены в инспекторе.");
            return;
        }

        foreach (var row in _spawnedRows)
        {
            if (row != null) Destroy(row);
        }
        _spawnedRows.Clear();

        Debug.Log($"Rebuild post match board: {ScoreManager.Instance != null}");
        if (ScoreManager.Instance == null) return;

        List<PlayerboardEntry> entries = new List<PlayerboardEntry>();
        foreach (var e in ScoreManager.Instance.Leaderboard)
        {
            entries.Add(e);
        }

        entries = entries
            .OrderByDescending(e => e.Kills)
            .ThenBy(e => e.Deaths)
            .ToList();

        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

        Debug.Log($"Try to Instantiate row's | entries: {entries.Count}");

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerboardEntry entry = entries[i];
            int rank = i + 1;

            GameObject rowObj = Instantiate(_rowPrefab, _rowsContainer);
            rowObj.SetActive(true);
            _spawnedRows.Add(rowObj);

            RowView row = FindRowView(rowObj);
            int score = entry.Kills - entry.Deaths;

            if (row.rankText != null) row.rankText.text += rank.ToString();
            if (row.nameText != null) row.nameText.text += entry.PlayerName.ToString();
            if (row.killsText != null) row.killsText.text += entry.Kills.ToString();
            if (row.deathsText != null) row.deathsText.text += entry.Deaths.ToString();
            if (row.scoreText != null) row.scoreText.text += score.ToString();

            Color color = rank switch
            {
                1 => _firstPlaceColor,
                2 => _secondPlaceColor,
                3 => _thirdPlaceColor,
                _ => _defaultColor
            };

            // Подсветка локального игрока должна побеждать подсветку места, чтобы
            // всегда было видно себя в списке, даже если ты не в топ-3.
            if (entry.ClientId == localClientId)
                color = _localPlayerColor;

            SetRowColor(row, color);
        }
    }

    private void SetRowColor(RowView row, Color color)
    {
        if (row.rankText != null) row.rankText.color = color;
        if (row.nameText != null) row.nameText.color = color;
        if (row.killsText != null) row.killsText.color = color;
        if (row.deathsText != null) row.deathsText.color = color;
        if (row.scoreText != null) row.scoreText.color = color;
    }

    private RowView FindRowView(GameObject rowObj)
    {
        return new RowView
        {
            root = rowObj,
            rankText = FindChildText(rowObj.transform, "Rank"),
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