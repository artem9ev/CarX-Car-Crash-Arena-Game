using UnityEngine;

[RequireComponent(typeof(MovingCar))]
public class PlayerInput : MonoBehaviour
{
    [Header("Настройки чувствительности")]
    [SerializeField] private float steeringSensitivity = 1f;
    [SerializeField] private float accelerationSensitivity = 1f;

    private MovingCar movingCar;

    private void Start()
    {
        movingCar = GetComponent<MovingCar>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal") * steeringSensitivity;
        float vertical = Input.GetAxis("Vertical") * accelerationSensitivity;
        bool brake = Input.GetKey(KeyCode.Space);

        movingCar.SetInputs(vertical, horizontal, brake);
    }
}