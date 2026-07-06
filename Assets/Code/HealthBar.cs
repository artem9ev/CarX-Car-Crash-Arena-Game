using UnityEngine;
using UnityEngine.UI;
using TMPro; // ← ВАЖНО! Добавь эту строку

public class HealthBar : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private MovingCar car;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI hpText; // ← ДОБАВЬ ЭТО ПОЛЕ

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
        if (car != null)
        {
            car.OnHealthChanged += UpdateHealthBar;
            targetFillAmount = 1f;
            currentFillAmount = 1f;

            // Показываем начальное значение HP
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(car.MaxHealth)} HP";
            }
        }
    }

    private void OnDestroy()
    {
        if (car != null)
        {
            car.OnHealthChanged -= UpdateHealthBar;
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

    private void UpdateHealthBar(float currentHealth)
    {
        targetFillAmount = currentHealth / car.MaxHealth;

        if (hpText != null)
        {
            int hpInt = Mathf.CeilToInt(currentHealth);
            hpText.text = $"{hpInt} / {Mathf.CeilToInt(car.MaxHealth)} HP";

            // 👆 ВСЁ! Больше ничего не нужно.
            // Текст останется того цвета, который ты задал в Inspector.
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