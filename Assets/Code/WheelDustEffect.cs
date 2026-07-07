using System.Collections.Generic;
using UnityEngine;

public class WheelDustEffect : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private List<ParticleSystem> dustParticles;
    [SerializeField] private float minSpeedForDust = 2f; // Минимальная скорость для пыли
    [SerializeField] private float dustIntensity = 1f;   // Интенсивность частиц

    private MovingCar car;
    private Rigidbody rb;

    private void Start()
    {
        car = GetComponent<MovingCar>();
        rb = GetComponent<Rigidbody>();
        
        if (dustParticles == null)
        {
            Debug.LogWarning("WheelDustEffect: Не назначен Particle System!");
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
                // Выключаем при низкой скорости
                if (particle.isPlaying)
                {
                    particle.Stop();
                }
            }
        }
    }

    // Опционально: проверка, что колёса на земле
    private bool IsWheelOnGround()
    {
        // Можно добавить рейкаст вниз для проверки
        return Physics.Raycast(transform.position, Vector3.down, 0.5f);
    }
}