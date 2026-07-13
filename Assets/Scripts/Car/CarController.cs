using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class CarController : NetworkBehaviour
{
    private MovingCar _car;

    private bool _isControlling;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
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

    private void OnGas(float value) 
    { 
        OnGasRpc(value); 
    }
    private void OnBrake(float value)
    {
        OnBrakeRpc(value);
    }
    private void OnSteer(float value)
    {
        OnSteerRpc(value);
    }

    public void EnableControlls()
    {
        if (!IsOwner || _isControlling)
            return;

        PlayerInputHandler.Instance.onGas += OnGas;
        PlayerInputHandler.Instance.onBrake += OnBrake;
        PlayerInputHandler.Instance.onSteer += OnSteer;

        _isControlling = true;
    }

    public void DisableControlls()
    {
        if (!IsOwner || !_isControlling)
            return;

        PlayerInputHandler.Instance.onGas -= OnGas;
        PlayerInputHandler.Instance.onBrake -= OnBrake;
        PlayerInputHandler.Instance.onSteer -= OnSteer;

        _isControlling = false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnGasRpc(float value)
    {
        _car.OnGas(value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnBrakeRpc(float value)
    {
        _car.OnBrake(value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void OnSteerRpc(float value)
    {
        _car.OnSteer(value);
    }
}
