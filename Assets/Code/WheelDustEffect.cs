using System.Collections.Generic;
using UnityEngine;

public class WheelDustEffect : MonoBehaviour
{
    [Header("Настройки пыли из-под колёс")]
    [SerializeField] private List<ParticleSystem> dustParticles;
    [SerializeField] private float minSpeedForDust = 2f; // Минимальная скорость для пыли
    [SerializeField] private float dustIntensity = 1f;   // Интенсивность частиц

    [Header("Настройки эффекта удара")]
    [SerializeField] private GameObject impactEffectPrefab;   // ПРЕФАБ эффекта удара
    [SerializeField] private float minImpactForce = 5f;       // Минимальная сила удара для эффекта
    [SerializeField] private float impactIntensityMultiplier = 0.5f; // Множитель интенсивности
    [SerializeField] private bool rotateByNormal = true;      // Поворачивать эффект по нормали удара

    private MovingCar car;
    private Rigidbody rb;

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
    }

    private void Update()
    {
        if (dustParticles == null || car == null) return;

        // Проверяем скорость
        float speed = rb.linearVelocity.magnitude;

        if (speed > minSpeedForDust)
        {
            foreach (var particle in dustParticles)
            {
                if (particle == null) continue;

                // Включаем частицы
                if (!particle.isPlaying)
                {
                    particle.Play();
                }

                // Меняем интенсивность в зависимости от скорости
                var main = particle.main;
                main.startSpeed = speed * dustIntensity;
            }
        }
        else
        {
            foreach (var particle in dustParticles)
            {
                if (particle == null) continue;

                // Выключаем при низкой скорости
                if (particle.isPlaying)
                {
                    particle.Stop();
                }
            }
        }
    }

    // ===== НОВАЯ МЕХАНИКА: ЧАСТИЦЫ ПРИ СТОЛКНОВЕНИИ =====
    private void OnCollisionEnter(Collision collision)
    {
        if (impactEffectPrefab == null || rb == null) return;
        if (collision.contacts == null || collision.contacts.Length == 0) return;

        // Сила удара = относительная скорость * масса
        float impactForce = collision.relativeVelocity.magnitude * rb.mass;

        // Если удар слабее порога - игнорируем
        if (impactForce < minImpactForce) return;

        // Получаем точку контакта
        Vector3 contactPoint = collision.contacts[0].point;
        Vector3 contactNormal = collision.contacts[0].normal;

        // Запускаем эффект удара
        PlayImpactEffect(impactForce, contactPoint, contactNormal);
    }

    private void PlayImpactEffect(float impactForce, Vector3 contactPoint, Vector3 contactNormal)
    {
        // Определяем поворот эффекта
        Quaternion rotation = Quaternion.identity;
        if (rotateByNormal)
        {
            // Поворот так, чтобы частицы вылетали от поверхности
            rotation = Quaternion.LookRotation(contactNormal);
        }

        // Создаём КОПИЮ эффекта в точке удара
        GameObject impactInstance = Instantiate(
            impactEffectPrefab,
            contactPoint,
            rotation
        );

        // Получаем ParticleSystem у копии
        ParticleSystem instancePS = impactInstance.GetComponent<ParticleSystem>();
        if (instancePS == null)
        {
            Debug.LogWarning("У префаба эффекта удара нет ParticleSystem!");
            Destroy(impactInstance);
            return;
        }

        // Меняем интенсивность в зависимости от силы удара
        var main = instancePS.main;
        float intensity = impactForce * impactIntensityMultiplier;
        intensity = Mathf.Clamp(intensity, 1f, 10f); // Ограничиваем макс. интенсивность

        main.startSpeed = intensity * 3f;
        main.startSize = intensity * 0.3f;
        main.startLifetime = 0.5f + intensity * 0.1f;

        // Запускаем
        instancePS.Play();

        // Уничтожаем копию после завершения проигрывания
        float duration = main.duration + main.startLifetime.constantMax;
        Destroy(impactInstance, duration);

        Debug.Log($"💥 Удар! Сила: {impactForce:F1} | Частицы в точке: {contactPoint}");
    }
    // ================================================

    // Опционально: проверка, что колёса на земле
    private bool IsWheelOnGround()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.5f);
    }
}