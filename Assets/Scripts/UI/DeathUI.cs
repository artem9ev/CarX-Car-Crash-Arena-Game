using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private TextMeshProUGUI respawnText;

    [Header("Настройки")]
    [SerializeField] private float delayBeforeShow = 1f;
    [SerializeField] private string respawnSceneName;

    [Header("Ссылки на системы")]
    [SerializeField] private VehicleHealth playerHealth;  // ← НОВОЕ

    private bool isDead = false;

    private void Start()
    {
        // Ищем VehicleHealth игрока (машина с тегом "Player" или первая найденная)
        if (playerHealth == null)
        {
            // Сначала ищем машину с тегом "Player"
            GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
            if (playerCar != null)
            {
                playerHealth = playerCar.GetComponent<VehicleHealth>();
            }

            // Если не нашли по тегу - ищем первую VehicleHealth
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<VehicleHealth>();
            }
        }

        if (playerHealth == null)
        {
            Debug.LogError("❌ DeathUI: VehicleHealth игрока не найден!");
            return;
        }

        // Выключаем панель при старте
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // Подписываемся на события
        playerHealth.OnDeath += HandleDeath;

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
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandleDeath;
        }
    }

    // ===== ОБРАБОТКА СМЕРТИ =====
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 Игрок умер, показываем UI...");

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

        // Ставим игру на паузу
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
        Debug.Log("🚪 Кнопка меню нажата");

        Time.timeScale = 1f;

        // Загружаем главную сцену
        if (!string.IsNullOrEmpty(respawnSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(respawnSceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ Имя сцены для респавна не указана!");
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

            // Ищем новую машину игрока и подписываемся на её события
            Invoke(nameof(SubscribeToNewPlayer), 0.1f);

            Debug.Log("✅ Игрок зареспавнен!");
        }
        else
        {
            Debug.LogError("❌ CarSpawner не найден! Невозможно зареспавнить игрока.");
        }

        // Сбрасываем флаг смерти
        isDead = false;
    }

    // ===== ПОДПИСКА НА НОВУЮ МАШИНУ ПОСЛЕ РЕСПАВНА =====
    private void SubscribeToNewPlayer()
    {
        // Отписываемся от старой машины (если она ещё существует)
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandleDeath;
        }

        // Ищем новую машину игрока
        GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
        if (playerCar != null)
        {
            playerHealth = playerCar.GetComponent<VehicleHealth>();
        }
        else
        {
            playerHealth = FindObjectOfType<VehicleHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnDeath += HandleDeath;
            Debug.Log("✅ Подписка на новую машину игрока");
        }
        else
        {
            Debug.LogError("❌ Не удалось найти новую машину игрока!");
        }
    }
}