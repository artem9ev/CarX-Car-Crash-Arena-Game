using System.Collections.Generic;
using UnityEngine;

public class WheelDustEffect : MonoBehaviour
{
    [Header("Настройки пыли из-под колёс")]
    [SerializeField] private List<ParticleSystem> dustParticles;
    [SerializeField] private float minSpeedForDust = 2f;
    [SerializeField] private float dustIntensity = 1f;

    [Header("Настройки эффекта удара")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float minImpactForce = 5f;
    [SerializeField] private float impactIntensityMultiplier = 0.5f;
    [SerializeField] private bool rotateByNormal = true;

    [Header("Дым при повреждении")]
    [SerializeField] private ParticleSystem smokeEffect;
    [SerializeField] private float smokeThreshold = 0.5f;   // Порог HP для дыма (50%)
    [SerializeField] private float smokeIntensity = 1f;

    [Header("Взрыв при смерти")]
    [SerializeField] private GameObject explosionEffectPrefab;  // ПРЕФАБ взрыва

    private MovingCar car;
    private Rigidbody rb;
    private bool isSmoking = false;
    private bool isDead = false;
    private float currentHealth;  // Храним текущее HP

    private void Start()
    {
        car = GetComponent<MovingCar>();
        rb = GetComponent<Rigidbody>();

        if (dustParticles == null || dustParticles.Count == 0)
        {
            Debug.LogWarning("WheelDustEffect: Не назначены Particle System для пыли!");
        }

        if (impactEffectPrefab == null)
        {
            Debug.LogWarning("WheelDustEffect: Не назначен префаб эффекта удара!");
        }

        // Выключаем дым при старте
        if (smokeEffect != null) smokeEffect.Stop();

        // Инициализируем HP
        if (car != null)
        {
            currentHealth = car.MaxHealth;

            // Подписываемся на события
            car.OnHealthChanged += HandleHealthChanged;
            car.OnDeath += HandleDeath;
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (car != null)
        {
            car.OnHealthChanged -= HandleHealthChanged;
            car.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (car == null || rb == null) return;

        // ===== ПЫЛЬ ИЗ-ПОД КОЛЁС =====
        UpdateWheelDust();

        // ===== ДЫМ =====
        UpdateSmokeEffect();
    }

    // ===== ОБРАБОТКА ИЗМЕНЕНИЯ HP =====
    private void HandleHealthChanged(float newHealth)
    {
        currentHealth = newHealth;
    }

    // ===== ОБРАБОТКА СМЕРТИ =====
    private void HandleDeath()
    {
        isDead = true;

        // Выключаем дым
        if (smokeEffect != null && smokeEffect.isPlaying)
        {
            smokeEffect.Stop();
        }

        // Выключаем пыль
        if (dustParticles != null)
        {
            foreach (var p in dustParticles)
            {
                if (p != null && p.isPlaying) p.Stop();
            }
        }

        // Запускаем взрыв
        if (explosionEffectPrefab != null)
        {
            GameObject explosionInstance = Instantiate(
                explosionEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            // Уничтожаем через время
            ParticleSystem ps = explosionInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                float duration = main.duration + main.startLifetime.constantMax;
                Destroy(explosionInstance, duration);
            }
            else
            {
                Destroy(explosionInstance, 2f);
            }

            Debug.Log("💥 Взрыв!");
        }
    }

    // ===== УПРАВЛЕНИЕ ДЫМОМ =====
    private void UpdateSmokeEffect()
    {
        if (isDead || smokeEffect == null || car == null) return;

        float healthPercent = currentHealth / car.MaxHealth;

        // Если HP ниже порога и дым ещё не идёт
        if (healthPercent <= smokeThreshold && !isSmoking)
        {
            smokeEffect.Play();
            isSmoking = true;
            Debug.Log($"💨 Машина начала дымить! HP: {healthPercent * 100:F0}%");
        }

        // Если дым идёт, меняем интенсивность в зависимости от HP
        if (isSmoking)
        {
            var main = smokeEffect.main;
            float intensity = (1f - healthPercent) * smokeIntensity;
            main.startSpeed = intensity * 5f;
            main.startSize = intensity * 2f;
        }

        // Если HP восстановилось выше порога
        if (healthPercent > smokeThreshold && isSmoking)
        {
            smokeEffect.Stop();
            isSmoking = false;
        }
    }

    // ===== ПЫЛЬ ИЗ-ПОД КОЛЁС =====
    private void UpdateWheelDust()
    {
        if (isDead || dustParticles == null) return;

        float speed = rb.linearVelocity.magnitude;

        if (speed > minSpeedForDust)
        {
            foreach (var particle in dustParticles)
            {
                if (particle == null) continue;

                if (!particle.isPlaying)
                {
                    particle.Play();
                }

                var main = particle.main;
                main.startSpeed = speed * dustIntensity;
            }
        }
        else
        {
            foreach (var particle in dustParticles)
            {
                if (particle == null) continue;

                if (particle.isPlaying)
                {
                    particle.Stop();
                }
            }
        }
    }

    // ===== ЧАСТИЦЫ ПРИ СТОЛКНОВЕНИИ =====
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;  // Не создаём эффекты после смерти

        if (impactEffectPrefab == null || rb == null) return;
        if (collision.contacts == null || collision.contacts.Length == 0) return;
        if (LayerMask.LayerToName(collision.thisCollider.gameObject.layer) == "Default") return;
        float impactForce = collision.relativeVelocity.magnitude * rb.mass;

        if (impactForce < minImpactForce) return;

        Vector3 contactPoint = collision.contacts[0].point;
        Vector3 contactNormal = collision.contacts[0].normal;

        PlayImpactEffect(impactForce, contactPoint, contactNormal);
    }

    private void PlayImpactEffect(float impactForce, Vector3 contactPoint, Vector3 contactNormal)
    {
        Quaternion rotation = Quaternion.identity;
        if (rotateByNormal)
        {
            rotation = Quaternion.LookRotation(contactNormal);
        }

        GameObject impactInstance = Instantiate(
            impactEffectPrefab,
            contactPoint,
            rotation
        );

        ParticleSystem instancePS = impactInstance.GetComponent<ParticleSystem>();
        if (instancePS == null)
        {
            Destroy(impactInstance);
            return;
        }

        var main = instancePS.main;
        float intensity = impactForce * impactIntensityMultiplier;
        intensity = Mathf.Clamp(intensity, 1f, 10f);

        main.startSpeed = intensity * 3f;
        main.startSize = intensity * 0.3f;
        main.startLifetime = 0.5f + intensity * 0.1f;

        instancePS.Play();

        float duration = main.duration + main.startLifetime.constantMax;
        Destroy(impactInstance, duration);
    }

    private bool IsWheelOnGround()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.5f);
    }
}