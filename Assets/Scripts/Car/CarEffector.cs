using UnityEngine;

public class CarEffector : MonoBehaviour
{
    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem smokeEffect;        // Дым при повреждении
    [SerializeField] private ParticleSystem explosionEffect;    // Взрыв при смерти
    [SerializeField] private float smokeThreshold = 0.5f;       // Порог HP для дыма (50%)
    [SerializeField] private float smokeIntensity = 1f;         // Интенсивность дыма

    [Header("Частицы из-под колёс")]
    [SerializeField] private ParticleSystem[] wheelDustEffects; // Массив частиц для каждого колеса (4 шт)
    [SerializeField] private float minSpeedForDust = 2f;        // Мин. скорость для появления пыли (м/с)
    [SerializeField] private float dustIntensityMultiplier = 0.5f; // Множитель интенсивности пыли

    private bool isSmoking = false; // Флаг: идёт ли дым


    // ===== УПРАВЛЕНИЕ ДЫМОМ =====
    /*private void UpdateSmokeEffect()
    {
        if (isDead || smokeEffect == null) return;

        float healthPercent = currentHealth / maxHealth;

        if (healthPercent <= smokeThreshold && !isSmoking)
        {
            smokeEffect.Play();
            isSmoking = true;
            Debug.Log($"💨 Машина начала дымить! HP: {healthPercent * 100:F0}%");
        }

        if (isSmoking)
        {
            var main = smokeEffect.main;
            float intensity = (1f - healthPercent) * smokeIntensity;
            main.startSpeed = intensity * 5f;
            main.startSize = intensity * 2f;
        }

        if (healthPercent > smokeThreshold && isSmoking)
        {
            smokeEffect.Stop();
            isSmoking = false;
        }
    }*/

    // ===== ЧАСТИЦЫ ИЗ-ПОД КАЖДОГО КОЛЕСА =====
    /*private void UpdateWheelDust()
    {
        if (isDead || wheelDustEffects == null) return;

        float speed = _rb.linearVelocity.magnitude;

        // Проверяем каждое колесо отдельно
        UpdateWheelDustForWheel(wheelDustEffects.Length > 0 ? wheelDustEffects[0] : null, frontLeftWheel, speed);
        UpdateWheelDustForWheel(wheelDustEffects.Length > 1 ? wheelDustEffects[1] : null, frontRightWheel, speed);
        UpdateWheelDustForWheel(wheelDustEffects.Length > 2 ? wheelDustEffects[2] : null, rearLeftWheel, speed);
        UpdateWheelDustForWheel(wheelDustEffects.Length > 3 ? wheelDustEffects[3] : null, rearRightWheel, speed);
    }
*/
    private void UpdateWheelDustForWheel(ParticleSystem dustEffect, WheelCollider wheel, float speed)
    {
        if (dustEffect == null) return;

        // Проверяем, касается ли колесо земли
        bool isGrounded = wheel.isGrounded;

        if (speed > minSpeedForDust && isGrounded)
        {
            // Включаем пыль, если она ещё не играет
            if (!dustEffect.isPlaying)
            {
                dustEffect.Play();
            }

            // Меняем интенсивность в зависимости от скорости
            var main = dustEffect.main;
            float intensity = (speed - minSpeedForDust) * dustIntensityMultiplier;
            intensity = Mathf.Clamp(intensity, 0.1f, 3f); // Ограничиваем макс. интенсивность

            main.startSpeed = intensity * 3f;
            main.startSize = intensity * 0.5f;
            main.startLifetime = 0.5f + intensity * 0.3f;
        }
        else
        {
            // Выключаем пыль, если колесо в воздухе или машина стоит
            if (dustEffect.isPlaying)
            {
                dustEffect.Stop();
            }
        }
    }
}
