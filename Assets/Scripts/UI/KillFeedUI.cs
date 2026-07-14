using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Показывает kill-feed (список сообщений об убийствах) в углу экрана.
///
/// НАСТРОЙКА В UNITY:
/// 1. Создай Canvas (если ещё нет).
/// 2. Внутри Canvas создай пустой GameObject "KillFeedContainer" — это будет
///    _container. Повесь на него Vertical Layout Group (Child Alignment: Upper Left
///    или Upper Right — как тебе нужно, Control Child Size: Width/Height по вкусу)
///    и, желательно, Content Size Fitter (Vertical Fit: Preferred Size).
///    Разместить обычно в правом верхнем углу экрана.
/// 3. Создай префаб одной строки: GameObject с компонентом TextMeshProUGUI
///    (можно с фоном-Image, обводкой и т.п.) — это _entryPrefab.
///    Сам префаб НЕ должен лежать в сцене — вынеси его в Assets как префаб
///    и оттащи в поле _entryPrefab, либо оставь как child-объект и задизейбль,
///    главное чтобы скрипт мог его заспавнить через Instantiate.
/// 4. Повесь этот скрипт (KillFeedUI) на любой объект в сцене (например,
///    на тот же Canvas), и перетащи в инспекторе _container и _entryPrefab.
/// </summary>
public class KillFeedUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _container;
    [SerializeField] private TextMeshProUGUI _entryPrefab;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _messageLifetime = 5f;
    [SerializeField, Min(0)] private int _maxVisibleMessages = 5;

    [Header("Colors")]
    [SerializeField] private Color _killColor = Color.white;
    [SerializeField] private Color _suicideColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("Text templates")]
    [SerializeField] private string _killFormat = "{0}  <color=#FF4444>уничтожил(а)</color>  {1}";
    [SerializeField] private string _suicideFormat = "{0}  <color=#888888>разбился(ась)</color>";

    private readonly Queue<TextMeshProUGUI> _activeEntries = new Queue<TextMeshProUGUI>();

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnKillFeed -= HandleKillFeed;
        }
    }

    /// <summary>
    /// ScoreManager — это in-scene NetworkObject, который может ещё не успеть
    /// заспавниться к моменту OnEnable этого UI (например, если сцена с UI
    /// грузится раньше сетевой сцены). Поэтому ждём появления Instance.
    /// </summary>
    private IEnumerator SubscribeWhenReady()
    {
        while (ScoreManager.Instance == null)
        {
            yield return null;
        }

        ScoreManager.Instance.OnKillFeed += HandleKillFeed;
    }

    private void HandleKillFeed(string attackerName, string victimName, bool isSuicideOrEnvironment)
    {
        string message = isSuicideOrEnvironment
            ? string.Format(_suicideFormat, victimName)
            : string.Format(_killFormat, attackerName, victimName);

        Color color = isSuicideOrEnvironment ? _suicideColor : _killColor;

        AddEntry(message, color);
    }

    private void AddEntry(string message, Color color)
    {
        if (_container == null || _entryPrefab == null)
        {
            Debug.LogWarning("[KillFeedUI] _container или _entryPrefab не назначены в инспекторе.");
            return;
        }

        TextMeshProUGUI entry = Instantiate(_entryPrefab, _container);
        entry.text = message;
        entry.color = color;
        entry.gameObject.SetActive(true);

        _activeEntries.Enqueue(entry);

        if (_maxVisibleMessages > 0)
        {
            while (_activeEntries.Count > _maxVisibleMessages)
            {
                var oldest = _activeEntries.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest.gameObject);
                }
            }
        }

        StartCoroutine(RemoveAfterDelay(entry, _messageLifetime));
    }

    private IEnumerator RemoveAfterDelay(TextMeshProUGUI entry, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (entry == null) yield break;

        // Убираем именно этот объект из очереди, если он там ещё есть,
        // чтобы не удалить его дважды.
        if (_activeEntries.Contains(entry))
        {
            var temp = new Queue<TextMeshProUGUI>();
            while (_activeEntries.Count > 0)
            {
                var item = _activeEntries.Dequeue();
                if (item != entry) temp.Enqueue(item);
            }
            while (temp.Count > 0)
            {
                _activeEntries.Enqueue(temp.Dequeue());
            }
        }

        Destroy(entry.gameObject);
    }
}