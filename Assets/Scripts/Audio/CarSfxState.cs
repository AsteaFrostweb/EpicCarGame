using UnityEngine;

[System.Serializable]
public struct CarSfxState
{
    [Range(0f, 1f)] public float throttle;
    [Range(0f, 1f)] public float reverse;
    [Range(0f, 1f)] public float brake;
    [Range(0f, 1f)] public float handbrake;
    public bool engineRunning;
    public float speed;
    public float maxSpeed;
    [Range(0f, 1f)] public float speed01;
    [Range(0f, 1f)] public float forwardSpeed01;
    [Range(-1f, 1f)] public float steering;
    [Range(0f, 1f)] public float engineLoad;
    [Range(0f, 1f)] public float wheelSpin;
    [Range(0f, 1f)] public float skid;
    [Range(0f, 1f)] public float driftAmount;
    public float forwardSlip;
    public float sidewaysSlip;
}
