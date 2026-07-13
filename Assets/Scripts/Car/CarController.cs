using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class CarController : NetworkBehaviour
{
    private MovingCar _car;
    private CarEngine _engine;

    private bool _isControlling;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
        _engine = GetComponent<CarEngine>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        ClientEventBus.Instance.InvokeCarOwn(_car);

        EnableControlls();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        DisableControlls();
    }

    public void OnGas(float value) => OnGasRpc(value);
    public void OnBrake(float value) => OnBrakeRpc(value);
    public void OnSteer(float value) => OnSteerRpc(value);
    public void OnShiftUp() => ShiftUpRpc();
    public void OnShiftDown() => ShiftDownRpc();

    public void EnableControlls()
    {
        if (!IsOwner || _isControlling)
            return;

        PlayerInputHandler.Instance.onGas += OnGas;
        PlayerInputHandler.Instance.onBrake += OnBrake;
        PlayerInputHandler.Instance.onSteer += OnSteer;
        // Подключи к своим инпут-евентам для передач, если они есть:
        // PlayerInputHandler.Instance.onShiftUp += OnShiftUp;
        // PlayerInputHandler.Instance.onShiftDown += OnShiftDown;

        _isControlling = true;
    }

    public void DisableControlls()
    {
        if (!IsOwner || !_isControlling)
            return;

        PlayerInputHandler.Instance.onGas -= OnGas;
        PlayerInputHandler.Instance.onBrake -= OnBrake;
        PlayerInputHandler.Instance.onSteer -= OnSteer;
        // PlayerInputHandler.Instance.onShiftUp -= OnShiftUp;
        // PlayerInputHandler.Instance.onShiftDown -= OnShiftDown;

        _isControlling = false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnGasRpc(float value) => _engine.OnGas(value);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnBrakeRpc(float value) => _engine.OnBrake(value);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnSteerRpc(float value) => _car.OnSteer(value);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ShiftUpRpc() => _engine.NextGear();

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ShiftDownRpc() => _engine.PrevGear();
}