using UnityEngine;

public class CarEffector : MonoBehaviour
{
    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem _engineSmoke;
    [SerializeField] private ParticleSystem _carExhaust;
    [Header("Настройки привязки")]
    public float minEmission = 5f;  // частиц в секунду на холостых
    public float maxEmission = 150f; // частиц в секунду на максимальных оборотах

    private CarEngine _engine;
    
    private ParticleSystem.EmissionModule emissionModule;

    void Start()
    {
        if (_carExhaust != null)
        {
            emissionModule = _carExhaust.emission;
            _carExhaust.Play();
        }
    }

    // Этот метод вызывается каждый кадр из скрипта управления машиной
    public void SetEngineRPM(float currentRPM)
    {
        // Нормируем обороты от 0 до 1
        float t = Mathf.InverseLerp(_engine.idleRPM, _engine.peakRPM, currentRPM);

        // Рассчитываем желаемую эмиссию
        float targetEmission = Mathf.Lerp(minEmission, maxEmission, t);

        // Применяем
        emissionModule.rateOverTime = targetEmission;
    }

    private void Awake()
    {
        _engine = GetComponent<CarEngine>();
    }

    private void FixedUpdate()
    {
        if (_carExhaust != null && _engine != null)
        {
            SetEngineRPM(_engine.rpm);
        }
    }
}
