using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleWheelCarController))]
[RequireComponent(typeof(CarSfxController))]
public class SimpleWheelCarSfxAdapter : MonoBehaviour
{
    [SerializeField] private SimpleWheelCarController carController;
    [SerializeField] private CarSfxController sfxController;
    [SerializeField] private bool engineRunning = true;

    private void Awake()
    {
        if (carController == null)
        {
            carController = GetComponent<SimpleWheelCarController>();
        }

        if (sfxController == null)
        {
            sfxController = GetComponent<CarSfxController>();
        }

        if (sfxController == null)
        {
            sfxController = gameObject.AddComponent<CarSfxController>();
        }
    }

    private void Update()
    {
        if (carController == null || sfxController == null)
        {
            return;
        }

        sfxController.SetState(new CarSfxState
        {
            engineRunning = engineRunning,
            throttle = carController.Throttle01,
            reverse = carController.Reverse01,
            brake = carController.Brake01,
            handbrake = carController.Handbrake01,
            speed = carController.speed,
            maxSpeed = carController.maxSpeed,
            speed01 = carController.Speed01,
            forwardSpeed01 = carController.ForwardSpeed01,
            steering = carController.Steering01,
            engineLoad = carController.EngineLoad01,
            wheelSpin = carController.WheelSpin01,
            skid = carController.Skid01,
            driftAmount = carController.Drift01,
            forwardSlip = carController.RearForwardSlip,
            sidewaysSlip = carController.RearSidewaysSlip
        });
    }

    public void SetEngineRunning(bool isRunning)
    {
        engineRunning = isRunning;
    }
}
