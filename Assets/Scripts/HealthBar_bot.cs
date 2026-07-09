using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar_bot : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Ссылки на системы")]
    [SerializeField] private Bot_hp Bot_hp;

    [Header("Настройки")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Gradient colorGradient;

    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    private Image fillImage;

    private void Awake()
    {
        fillImage = fill.GetComponent<Image>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Start()
    {
        // Ищем VehicleHealth игрока, если не назначен вручную
        if (Bot_hp == null)
        {
            GameObject playerCar = transform.parent.gameObject;
            if (playerCar != null)
            {
                Bot_hp = playerCar.GetComponent<Bot_hp>();
            }

        }

        if (Bot_hp == null)
        {
            Debug.LogError("❌ HealthBar: VehicleHealth не найден!");
            return;
        }

        // Подписываемся на событие изменения HP
        Bot_hp.OnHealthChanged += UpdateHealthBar;
        Bot_hp.OnDeath += HandleDeath;

        // Инициализация
        targetFillAmount = 1f;
        currentFillAmount = 1f;

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(Bot_hp.MaxHealth)} HP";
        }
    }

    private void OnDestroy()
    {
        if (Bot_hp != null)
        {
            Bot_hp.OnHealthChanged -= UpdateHealthBar;
            Bot_hp.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        UpdateFillVisual();

        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }

    // ===== ОБНОВЛЕНИЕ ПОЛОСКИ HP =====
    private void UpdateHealthBar(float currentHealth)
    {
        targetFillAmount = currentHealth / Bot_hp.MaxHealth;

        if (hpText != null)
        {
            int hpInt = Mathf.CeilToInt(currentHealth);
            hpText.text = $"{hpInt} / {Mathf.CeilToInt(Bot_hp.MaxHealth)} HP";
        }
    }

    // ===== ОБРАБОТКА СМЕРТИ =====
    private void HandleDeath()
    {
        // Плавно опускаем полоску до 0
        targetFillAmount = 0f;

        if (hpText != null)
        {
            hpText.text = "0 HP";
        }
    }

    private void UpdateFillVisual()
    {
        fill.localScale = new Vector3(currentFillAmount, 1f, 1f);

        if (colorGradient != null)
        {
            fillImage.color = colorGradient.Evaluate(currentFillAmount);
        }
        else
        {
            fillImage.color = Color.Lerp(Color.red, Color.green, currentFillAmount);
        }
    }

    // ===== ПУБЛИЧНЫЙ МЕТОД ДЛЯ ПЕРЕПОДПИСКИ ПОСЛЕ РЕСПАВНА =====
    public void SubscribeToNewVehicle()
    {
        // Отписываемся от старой
        if (Bot_hp != null)
        {
            Bot_hp.OnHealthChanged -= UpdateHealthBar;
            Bot_hp.OnDeath -= HandleDeath;
        }

        // Ищем новую машину игрока
        GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
        if (playerCar != null)
        {
            Bot_hp = playerCar.GetComponent<Bot_hp>();
        }
        else
        {
            Bot_hp = FindObjectOfType<Bot_hp>();
        }

        if (Bot_hp != null)
        {
            Bot_hp.OnHealthChanged += UpdateHealthBar;
            Bot_hp.OnDeath += HandleDeath;

            // Сбрасываем полоску
            targetFillAmount = 1f;
            currentFillAmount = 1f;

            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(Bot_hp.MaxHealth)} HP";
            }

            Debug.Log("✅ HealthBar: Подписка на новую машину");
        }
        else
        {
            Debug.LogError("❌ HealthBar: Не удалось найти новую машину!");
        }
    }
}