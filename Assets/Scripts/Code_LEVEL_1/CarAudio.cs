using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CarAudio : MonoBehaviour
{
    [Header("Звук взрыва (при HP = 0)")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float explosionVolume = 1f;
    [SerializeField] private float explosionPitchVariation = 0.1f;

    [Header("Звук удара")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float impactVolume = 0.7f;
    [SerializeField] private float impactPitchVariation = 0.15f;
    [SerializeField] private float minImpactForce = 3f;
    [SerializeField] private float maxImpactVolume = 1.5f;

    [Header("Звук двигателя")]
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private float minEngineVolume = 0.3f;
    [SerializeField] private float maxEngineVolume = 1f;
    [SerializeField] private float minEnginePitch = 0.8f;
    [SerializeField] private float maxEnginePitch = 1.5f;
    [SerializeField] private float engineSmoothSpeed = 5f;
    [SerializeField] private float maxSpeed = 100f;  // Макс. скорость для расчёта звука

    [Header("Настройки AudioSource")]
    [SerializeField] private bool playSoundsIn3D = true;

    [Header("Ссылки на системы")]
    [SerializeField] private VehicleHealth vehicleHealth;  // ← НОВОЕ

    private AudioSource audioSource;
    private AudioSource engineAudioSource;
    private Rigidbody rb;
    private float currentEngineVolume;
    private float currentEnginePitch;
    private bool isDead = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Ищем VehicleHealth
        if (vehicleHealth == null)
        {
            vehicleHealth = GetComponent<VehicleHealth>();
        }

        if (vehicleHealth == null)
        {
            Debug.LogError("❌ CarAudio: VehicleHealth не найден!");
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Создаём отдельный AudioSource для двигателя
        GameObject engineObj = new GameObject("EngineAudioSource");
        engineObj.transform.SetParent(transform);
        engineObj.transform.localPosition = Vector3.zero;
        engineAudioSource = engineObj.AddComponent<AudioSource>();
        engineAudioSource.playOnAwake = false;
        engineAudioSource.loop = true;
        engineAudioSource.spatialBlend = playSoundsIn3D ? 1f : 0f;

        // Настройки основного AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = playSoundsIn3D ? 1f : 0f;

        // Подписываемся на событие смерти
        vehicleHealth.OnDeath += HandleDeath;

        // Инициализируем значения двигателя
        currentEngineVolume = minEngineVolume;
        currentEnginePitch = minEnginePitch;

        Debug.Log("✅ CarAudio: Подписка на события успешна");
    }

    private void OnDestroy()
    {
        if (vehicleHealth != null)
        {
            vehicleHealth.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (isDead || vehicleHealth == null || rb == null) return;

        UpdateEngineSound();
    }

    // ===== ОБРАБОТКА СМЕРТИ МАШИНЫ =====
    private void HandleDeath()
    {
        isDead = true;
        PlayExplosionSound();
        StopEngineSound();
    }

    // ===== ОБНОВЛЕНИЕ ЗВУКА ДВИГАТЕЛЯ =====
    private void UpdateEngineSound()
    {
        if (engineSound == null || engineAudioSource == null) return;

        // Получаем текущую скорость в км/ч
        float speed = rb.linearVelocity.magnitude * 3.6f;

        // Нормализуем скорость (0 до 1)
        float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);

        // Вычисляем целевые значения громкости и тона
        float targetVolume = Mathf.Lerp(minEngineVolume, maxEngineVolume, normalizedSpeed);
        float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, normalizedSpeed);

        // Плавно изменяем значения
        currentEngineVolume = Mathf.Lerp(currentEngineVolume, targetVolume, Time.deltaTime * engineSmoothSpeed);
        currentEnginePitch = Mathf.Lerp(currentEnginePitch, targetPitch, Time.deltaTime * engineSmoothSpeed);

        // Применяем к AudioSource
        engineAudioSource.volume = currentEngineVolume;
        engineAudioSource.pitch = currentEnginePitch;

        // Запускаем звук, если ещё не играет
        if (!engineAudioSource.isPlaying)
        {
            engineAudioSource.clip = engineSound;
            engineAudioSource.Play();
        }
    }

    // ===== ОСТАНОВКА ЗВУКА ДВИГАТЕЛЯ =====
    private void StopEngineSound()
    {
        if (engineAudioSource != null && engineAudioSource.isPlaying)
        {
            engineAudioSource.Stop();
        }
    }

    // ===== ВОСПРОИЗВЕДЕНИЕ ЗВУКА ВЗРЫВА =====
    private void PlayExplosionSound()
    {
        if (explosionSound == null)
        {
            Debug.LogWarning("⚠️ CarAudio: explosionSound не назначен!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("❌ CarAudio: AudioSource не найден!");
            return;
        }

        float randomPitch = 1f + Random.Range(-explosionPitchVariation, explosionPitchVariation);
        audioSource.pitch = randomPitch;
        audioSource.PlayOneShot(explosionSound, explosionVolume);

        Debug.Log($"🔊 Звук взрыва проигран! Pitch: {randomPitch:F2}");
    }

    // ===== ВОСПРОИЗВЕДЕНИЕ ЗВУКА УДАРА =====
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead || rb == null) return;

        float impactForce = collision.relativeVelocity.magnitude * rb.mass;
        if (impactForce < minImpactForce) return;

        PlayImpactSound(impactForce);
    }

    private void PlayImpactSound(float impactForce)
    {
        if (impactSound == null)
        {
            Debug.LogWarning("⚠️ CarAudio: impactSound не назначен!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("❌ CarAudio: AudioSource не найден!");
            return;
        }

        float randomPitch = 1f + Random.Range(-impactPitchVariation, impactPitchVariation);
        audioSource.pitch = randomPitch;

        float normalizedForce = Mathf.Clamp01((impactForce - minImpactForce) / 50f);
        float scaledVolume = Mathf.Lerp(impactVolume * 0.5f, maxImpactVolume, normalizedForce);

        audioSource.PlayOneShot(impactSound, scaledVolume);
    }
}