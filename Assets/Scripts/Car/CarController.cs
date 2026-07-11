using Unity.Netcode;
using UnityEngine;

public class CarController : NetworkBehaviour
{
    private MovingCar _car;

    private void Awake()
    {
        _car = GetComponent<MovingCar>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        ClientEventBus.Instance.InvokeCarOwn(_car);

        PlayerInputHandler.Instance.onGas += OnGas;
        PlayerInputHandler.Instance.onBrake += OnBrake;
        PlayerInputHandler.Instance.onSteer += OnSteer;
        //PlayerInputHandler.Instance.onHandbrake += OnGas;

        Debug.Log($"client: {OwnerClientId} CAR CONTROLLER");

    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        PlayerInputHandler.Instance.onGas -= OnGas;
        PlayerInputHandler.Instance.onBrake -= OnBrake;
        PlayerInputHandler.Instance.onSteer -= OnSteer;
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
