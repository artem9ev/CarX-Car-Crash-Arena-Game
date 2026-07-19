using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Вешается на GameObject с UI-кнопкой "Выйти в меню" / "Покинуть игру".
/// По нажатию отключается от сети (хост завершает сессию для всех, клиент просто
/// отключается сам) и возвращается в главное меню — переиспользует уже готовую
/// логику ConnectionManager.Disconnect() (NetworkManager.Shutdown() + SceneLoader.LoadMainMenu()).
///
/// НАСТРОЙКА В UNITY:
/// 1. Повесь этот скрипт на тот же GameObject, где Button (или любой другой —
///    главное, чтобы был компонент Button рядом/на этом же объекте).
/// 2. Ничего вручную привязывать в OnClick() Button не обязательно — скрипт сам
///    подпишется на клик в OnEnable(). Либо, если хочешь привязать вручную через
///    инспектор Button → OnClick() → перетащи этот объект → выбери
///    ExitToMainMenuButton.Disconnect — тогда просто не добавляй авто-подписку
///    (см. комментарий в OnEnable ниже).
/// </summary>
[RequireComponent(typeof(Button))]
public class ExitToMainMenuButton : MonoBehaviour
{
    [Tooltip("Показать окно подтверждения перед выходом (см. _confirmationPanel). Если объект не назначен — выход происходит сразу по клику.")]
    [SerializeField] private GameObject _confirmationPanel;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (_confirmationPanel != null)
        {
            _confirmationPanel.SetActive(true);
            return;
        }

        Disconnect();
    }

    /// <summary>
    /// Публичный метод — можно вызвать и напрямую (например, с кнопки "Да" в окне
    /// подтверждения, назначив её OnClick() на этот метод через инспектор).
    /// </summary>
    public void Disconnect()
    {
        if (_confirmationPanel != null)
        {
            _confirmationPanel.SetActive(false);
        }

        if (ConnectionManager.Instance == null)
        {
            Debug.LogError("[ExitToMainMenuButton] ConnectionManager.Instance не найден в сцене.");
            return;
        }

        ConnectionManager.Instance.Disconnect();
    }
}