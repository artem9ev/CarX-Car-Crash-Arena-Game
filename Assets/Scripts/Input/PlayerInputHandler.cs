using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions _input;

    private static PlayerInputHandler _instance;

    public static PlayerInputHandler Instance => _instance;

    public UnityAction<float> onGas;
    public UnityAction<float> onBrake;
    public UnityAction<float> onSteer;
    public UnityAction<float> onHandbrake;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(this);
    }

    private void OnEnable()
    {
        if (_input == null)
        {
            _input = new PlayerInputActions();
        }
        _input.Enable();
        _input.Player.Gas.performed += OnGasInput;
        _input.Player.Brake.performed += OnBrakeInput;
        _input.Player.Steer.performed += OnSteerInput;
        _input.Player.HandBrake.performed += OnHandbrakeInput;
    }

    private void OnDisable()
    {
        _input.Disable();

        _input.Player.Gas.performed -= OnGasInput;
        _input.Player.Brake.performed -= OnBrakeInput;
        _input.Player.Steer.performed -= OnSteerInput;
        _input.Player.HandBrake.performed -= OnHandbrakeInput;
    }

    private void OnGasInput(InputAction.CallbackContext ctx)
    {
        SetGasInput(ctx.ReadValue<float>());
    }
    private void OnBrakeInput(InputAction.CallbackContext ctx)
    {
        SetBrakeInput(ctx.ReadValue<float>());
    }
    private void OnSteerInput(InputAction.CallbackContext ctx)
    {
        SetSteerInput(ctx.ReadValue<float>());
    }
    private void OnHandbrakeInput(InputAction.CallbackContext ctx)
    {
        SetHandbrakeInput(ctx.ReadValue<float>());
    }


    public void SetGasInput(float value)
    {
        onGas?.Invoke(value);
    }
    public void SetBrakeInput(float value)
    {
        onBrake?.Invoke(value);
    }
    public void SetSteerInput(float value)
    {
        onSteer?.Invoke(value);
    }
    public void SetHandbrakeInput(float value)
    {
        onHandbrake?.Invoke(value);
    }
}
