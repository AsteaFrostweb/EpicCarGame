using UnityEngine;

public interface ICarSfxController
{
    bool EngineRunning { get; }

    void StartEngine();
    void StopEngine();
    void SetEngineRunning(bool isRunning);
    void SetThrottle(float throttle);
    void SetReverse(float reverse);
    void SetBrake(float brake);
    void SetHandbrake(float handbrake);
    void SetSpeed(float speed, float maxSpeed);
    void SetNormalizedTelemetry(
        float speed01,
        float forwardSpeed01,
        float steering01,
        float engineLoad01,
        float wheelSpin01,
        float skid01,
        float drift01);
    void SetDrift(float driftAmount);
    void SetWheelSlip(float forwardSlip, float sidewaysSlip);
    void SetWheelSlipFromColliders(params WheelCollider[] wheelColliders);
    void SetState(CarSfxState state);
}
