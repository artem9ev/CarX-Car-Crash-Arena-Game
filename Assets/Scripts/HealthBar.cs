using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Ссылки на системы")]
    [SerializeField] private VehicleHealth vehicleHealth;  // ← НОВОЕ (можно оставить пустым)

    [Header("Настройки")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Gradient colorGradient;

    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    private Image fillImage;

    private void Awake()
    {
        fillImage = fill.GetComponent<Image>();
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Ищем VehicleHealth игрока, если не назначен вручную
        if (vehicleHealth == null)
        {
            GameObject playerCar = transform.parent.gameObject;
            if (playerCar != null)
            {
                vehicleHealth = playerCar.GetComponent<VehicleHealth>();
            }

        }

        if (vehicleHealth == null)
        {
            Debug.LogError("❌ HealthBar: VehicleHealth не найден!");
            return;
        }

        // Подписываемся на событие изменения HP
        vehicleHealth.OnHealthChanged += UpdateHealthBar;
        vehicleHealth.OnDeath += HandleDeath;

        // Инициализация
        targetFillAmount = 1f;
        currentFillAmount = 1f;

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(vehicleHealth.MaxHealth)} HP";
        }
    }

    private void OnDestroy()
    {
        if (vehicleHealth != null)
        {
            vehicleHealth.OnHealthChanged -= UpdateHealthBar;
            vehicleHealth.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        //transform.LookAt(transform.position + mainCamera.transform.forward);
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        UpdateFillVisual();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }

    // ===== ОБНОВЛЕНИЕ ПОЛОСКИ HP =====
    private void UpdateHealthBar(float currentHealth)
    {
        targetFillAmount = currentHealth / vehicleHealth.MaxHealth;

        if (hpText != null)
        {
            int hpInt = Mathf.CeilToInt(currentHealth);
            hpText.text = $"{hpInt} / {Mathf.CeilToInt(vehicleHealth.MaxHealth)} HP";
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
}