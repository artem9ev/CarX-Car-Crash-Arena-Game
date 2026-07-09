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

    private VehicleHealth vehicleHealth;
    private Rigidbody rb;
    private bool isDead = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ищем VehicleHealth
        vehicleHealth = GetComponent<VehicleHealth>();
        if (vehicleHealth == null)
        {
            Debug.LogWarning("⚠️ WheelDustEffect: VehicleHealth не найден!");
        }
        else
        {
            // Подписываемся на событие смерти (чтобы остановить пыль)
            vehicleHealth.OnDeath += HandleDeath;
        }

        if (dustParticles == null || dustParticles.Count == 0)
        {
            Debug.LogWarning("WheelDustEffect: Не назначены Particle System для пыли!");
        }

        if (impactEffectPrefab == null)
        {
            Debug.LogWarning("WheelDustEffect: Не назначен префаб эффекта удара!");
        }

        // Выключаем пыль при старте
        if (dustParticles != null)
        {
            foreach (var p in dustParticles)
            {
                if (p != null) p.Stop();
            }
        }
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
        if (isDead || rb == null) return;

        UpdateWheelDust();
    }

    // ===== ОБРАБОТКА СМЕРТИ =====
    private void HandleDeath()
    {
        isDead = true;

        // Выключаем пыль
        if (dustParticles != null)
        {
            foreach (var p in dustParticles)
            {
                if (p != null && p.isPlaying) p.Stop();
            }
        }

        Debug.Log("💨 Пыль остановлена (машина уничтожена)");
    }

    // ===== ПЫЛЬ ИЗ-ПОД КОЛЁС =====
    private void UpdateWheelDust()
    {
        if (dustParticles == null) return;

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
        if (isDead) return;

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
}