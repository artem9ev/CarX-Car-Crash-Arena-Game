using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject deathPanel;           // Панель смерти (изначально выключена)
    [SerializeField] private Button respawnButton;            // Кнопка респавна
    [SerializeField] private Button mainMenuButton;           // Кнопка выхода в меню (опционально)
    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private TextMeshProUGUI respawnText;

    [Header("Настройки")]
    [SerializeField] private float delayBeforeShow = 1f;      // Задержка перед показом UI
    [SerializeField] private string respawnSceneName;         // Имя сцены для респавна (если нужно)

    private MovingCar car;
    private bool isDead = false;

    private void Start()
    {
        // Ищем машину на сцене
        car = FindObjectOfType<MovingCar>();

        if (car == null)
        {
            Debug.LogError("❌ DeathUI: MovingCar не найден на сцене!");
            return;
        }

        // Выключаем панель при старте
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // Подписываемся на события
        car.OnDeath += HandleDeath;

        // Настраиваем кнопки
        if (respawnButton != null)
        {
            respawnButton.onClick.AddListener(OnRespawnClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        Debug.Log("✅ DeathUI: Инициализация завершена");
    }

    private void OnDestroy()
    {
        if (car != null)
        {
            car.OnDeath -= HandleDeath;
        }
    }

    // ===== ОБРАБОТКА СМЕРТИ =====
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(" Игрок умер, показываем UI...");

        // Показываем UI с задержкой
        Invoke(nameof(ShowDeathUI), delayBeforeShow);
    }

    // ===== ПОКАЗ UI СМЕРТИ =====
    private void ShowDeathUI()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        // Блокируем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ставим игру на паузу (опционально)
        Time.timeScale = 0f;

        Debug.Log("🎯 UI смерти показан");
    }

    // ===== ОБРАБОТКА НАЖАТИЯ КНОПКИ РЕСПАВНА =====
    private void OnRespawnClicked()
    {
        Debug.Log("🔄 Кнопка респавна нажата");

        // Возвращаем время
        Time.timeScale = 1f;

        // Скрываем UI
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // Респавним игрока
        RespawnPlayer();
    }

    // ===== ОБРАБОТКА НАЖАТИЯ КНОПКИ МЕНЮ =====
    private void OnMainMenuClicked()
    {
        Debug.Log(" Кнопка меню нажата");

        Time.timeScale = 1f;

        // Загружаем главную сцену
        if (!string.IsNullOrEmpty(respawnSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(respawnSceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ Имя сцены для респавна не указано!");
        }
    }

    // ===== РЕСПАВН ИГРОКА =====
    private void RespawnPlayer()
    {
        // Находим CarSpawner на сцене
        CarSpawner spawner = FindObjectOfType<CarSpawner>();

        if (spawner != null)
        {
            // Удаляем все старые машины
            spawner.DespawnAllCars();

            // Спавним новую машину
            spawner.ForceSpawnCar();

            Debug.Log("✅ Игрок зареспавнен!");
        }
        else
        {
            Debug.LogError("❌ CarSpawner не найден! Невозможно зареспавнить игрока.");
        }

        // Сбрасываем флаг смерти
        isDead = false;
    }
}