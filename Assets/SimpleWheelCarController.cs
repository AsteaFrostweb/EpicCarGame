using UnityEngine;

public class SimpleWheelCarController : MonoBehaviour
{
    private const float PresetMotorTorque = 2600f;
    private const float PresetBrakeTorque = 5000f;
    private const float PresetMaxSteerAngle = 30f;
    private const float PresetHandbrakeTorque = 4000f;
    private const float PresetMaxSpeed = 32f;

    private static readonly Vector3 PresetCenterOfMassOffset = new Vector3(0f, -0.35f, -0.1f);

    private const float PresetDownforce = 6f;
    private const float PresetDriftGrip = 0.55f;
    private const float PresetAngularDragAtSpeed = 0.4f;
    private const float PresetAntiRollForce = 4500f;

    private const float PresetSteerResponse = 12f;

    private const float PresetAccelResponse = 10f;
    private const float PresetReverseTorqueMultiplier = 0.5f;
    private const float PresetCoastBrake = 80f;
    private const float PresetBrakeResponseSpeed = 1.5f;

    private const float PresetFrontSidewaysGrip = 0.95f;
    private const float PresetRearSidewaysGrip = 0.75f;
    private const float PresetFrontForwardGrip = 1.1f;
    private const float PresetRearForwardGrip = 0.95f;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Settings")]
    public float motorTorque = PresetMotorTorque;
    public float brakeTorque = PresetBrakeTorque;
    public float maxSteerAngle = PresetMaxSteerAngle;
    public float handbrakeTorque = PresetHandbrakeTorque;
    public float maxSpeed = PresetMaxSpeed;

    [Header("Stability")]
    public Vector3 centerOfMassOffset = PresetCenterOfMassOffset;
    public float downforce = PresetDownforce;
    public float driftGrip = PresetDriftGrip;
    public float angularDragAtSpeed = PresetAngularDragAtSpeed;
    public float antiRollForce = PresetAntiRollForce;

    [Header("Steering")]
    public float steerAcceleration = PresetSteerResponse * 12f;
    public float steerReturnSpeed = PresetSteerResponse * 14f;

    [Header("Throttle")]
    public float accelResponse = PresetAccelResponse;
    public float reverseTorqueMultiplier = PresetReverseTorqueMultiplier;
    public float coastBrake = PresetCoastBrake;
    public float brakeResponseSpeed = PresetBrakeResponseSpeed;

    [Header("Grip")]
    public float frontSidewaysGrip = PresetFrontSidewaysGrip;
    public float rearSidewaysGrip = PresetRearSidewaysGrip;
    public float frontForwardGrip = PresetFrontForwardGrip;
    public float rearForwardGrip = PresetRearForwardGrip;

    private Rigidbody rb;

    private float verticalInput;
    private float horizontalInput;
    private float smoothedVerticalInput;
    private float currentSteerAngle;
    public bool isBraking;
    public bool isHandbraking;
    public float currentForwardVelocity;
    private float forwardVelocity => Vector3.Dot(rb.linearVelocity, transform.forward);
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.centerOfMass += centerOfMassOffset;
            rb.angularDamping = angularDragAtSpeed;
        }

        ApplyWheelFriction();
    }

    [ContextMenu("Apply Arcade Tuning Preset")]
    private void ApplyArcadeTuningPreset()
    {
        motorTorque = PresetMotorTorque;
        brakeTorque = PresetBrakeTorque;
        maxSteerAngle = PresetMaxSteerAngle;
        handbrakeTorque = PresetHandbrakeTorque;
        maxSpeed = PresetMaxSpeed;

        centerOfMassOffset = PresetCenterOfMassOffset;
        downforce = PresetDownforce;
        driftGrip = PresetDriftGrip;
        angularDragAtSpeed = PresetAngularDragAtSpeed;
        antiRollForce = PresetAntiRollForce;

        steerAcceleration = PresetSteerResponse * 12f;
        steerReturnSpeed = PresetSteerResponse * 14f;

        accelResponse = PresetAccelResponse;
        reverseTorqueMultiplier = PresetReverseTorqueMultiplier;
        coastBrake = PresetCoastBrake;
        brakeResponseSpeed = PresetBrakeResponseSpeed;

        frontSidewaysGrip = PresetFrontSidewaysGrip;
        rearSidewaysGrip = PresetRearSidewaysGrip;
        frontForwardGrip = PresetFrontForwardGrip;
        rearForwardGrip = PresetRearForwardGrip;

        ApplyWheelFriction();
    }

    private void Update()
    {
        GetInput();
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        UpdateCurrentVelocity();
        SmoothInputs();
        ApplyWheelFriction();
        ApplyMotor();
        ApplySteering();
        ApplyBrakes();
        //ApplyStability();
    }

    private void GetInput()
    {
        verticalInput = Input.GetAxis("Vertical");     // W/S or Up/Down
        horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right

        currentForwardVelocity = forwardVelocity;
        isBraking = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && (currentForwardVelocity > 0.005f);
        isHandbraking = Input.GetKey(KeyCode.Space);
    }

    private void SmoothInputs()
    {
        float accelT = 1f - Mathf.Exp(-accelResponse * Time.fixedDeltaTime);

        smoothedVerticalInput = Mathf.Lerp(smoothedVerticalInput, verticalInput, accelT);
    }

    private void ApplyMotor()
    {
        float throttleInput = Mathf.Clamp01(smoothedVerticalInput);
        float reverseInput = Mathf.Clamp01(-smoothedVerticalInput);
        float speedLimiter = 1f - Mathf.Clamp01(Mathf.Abs(currentForwardVelocity) / maxSpeed);
        float torque = 0f;

        if (throttleInput > 0.01f && currentForwardVelocity > -brakeResponseSpeed)
        {
            torque = throttleInput * motorTorque * speedLimiter;
        }
        else if (reverseInput > 0.01f && currentForwardVelocity < brakeResponseSpeed)
        {
            torque = -reverseInput * motorTorque * reverseTorqueMultiplier * speedLimiter;
        }

        rearLeftCollider.motorTorque = torque;
        rearRightCollider.motorTorque = torque;
    }

    private void ApplySteering()
    {
        float dt = Time.fixedDeltaTime;

        if (Mathf.Abs(horizontalInput) > 0.001f)
        {
            currentSteerAngle += horizontalInput * steerAcceleration * dt;
        }
        else
        {
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, 0f, steerReturnSpeed * dt);
        }

        currentSteerAngle = Mathf.Clamp(currentSteerAngle, -maxSteerAngle, maxSteerAngle);

        frontLeftCollider.steerAngle = currentSteerAngle;
        frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void ApplyBrakes()
    {
        bool brakingForDirectionChange =
            (verticalInput < -0.01f && currentForwardVelocity > brakeResponseSpeed)
            || (verticalInput > 0.01f && currentForwardVelocity < -brakeResponseSpeed);

        float currentBrakeTorque = (isBraking || brakingForDirectionChange) ? brakeTorque : 0f;
        float currentHandbrakeTorque = isHandbraking ? handbrakeTorque : 0f;
        float currentCoastBrake = Mathf.Approximately(verticalInput, 0f) ? coastBrake : 0f;

        frontLeftCollider.brakeTorque = currentBrakeTorque + currentCoastBrake;
        frontRightCollider.brakeTorque = currentBrakeTorque + currentCoastBrake;

        rearLeftCollider.brakeTorque = currentBrakeTorque + currentHandbrakeTorque + currentCoastBrake;
        rearRightCollider.brakeTorque = currentBrakeTorque + currentHandbrakeTorque + currentCoastBrake;
    }

    private void ApplyStability()
    {
        if (!IsGrounded())
        {
            return;
        }

        //rb.AddForce(-transform.up * downforce * speed, ForceMode.Force);
        ApplyAntiRoll(frontLeftCollider, frontRightCollider);
        ApplyAntiRoll(rearLeftCollider, rearRightCollider);
    }

    private void ApplyWheelFriction()
    {
        float frontForward = frontForwardGrip;
        float frontSideways = frontSidewaysGrip;
        float rearForward = rearForwardGrip;
        float rearSideways = rearSidewaysGrip;

        if (isHandbraking)
        {
            frontSideways *= 0.95f;
            rearForward *= 0.9f;
            rearSideways = driftGrip;
        }

        ApplyFrictionToWheel(frontLeftCollider, frontForward, frontSideways);
        ApplyFrictionToWheel(frontRightCollider, frontForward, frontSideways);
        ApplyFrictionToWheel(rearLeftCollider, rearForward, rearSideways);
        ApplyFrictionToWheel(rearRightCollider, rearForward, rearSideways);
    }

    private void ApplyFrictionToWheel(WheelCollider wheel, float forwardStiffness, float sidewaysStiffness)
    {
        if (wheel == null)
        {
            return;
        }

        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = 0.35f;
        forwardFriction.extremumValue = 1f;
        forwardFriction.asymptoteSlip = 0.8f;
        forwardFriction.asymptoteValue = 0.8f;
        forwardFriction.stiffness = forwardStiffness;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = 0.28f;
        sidewaysFriction.extremumValue = 1f;
        sidewaysFriction.asymptoteSlip = 0.65f;
        sidewaysFriction.asymptoteValue = 0.75f;
        sidewaysFriction.stiffness = sidewaysStiffness;
        wheel.sidewaysFriction = sidewaysFriction;
    }

    private bool IsGrounded()
    {
        return WheelIsGrounded(frontLeftCollider)
            || WheelIsGrounded(frontRightCollider)
            || WheelIsGrounded(rearLeftCollider)
            || WheelIsGrounded(rearRightCollider);
    }

    private bool WheelIsGrounded(WheelCollider wheel)
    {
        return wheel != null && wheel.GetGroundHit(out _);
    }

    private void ApplyAntiRoll(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        if (leftWheel == null || rightWheel == null)
        {
            return;
        }

        float leftTravel = GetSuspensionTravel(leftWheel);
        float rightTravel = GetSuspensionTravel(rightWheel);
        float antiRoll = (leftTravel - rightTravel) * antiRollForce;

        if (leftWheel.GetGroundHit(out _))
        {
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRoll, leftWheel.transform.position);
        }

        if (rightWheel.GetGroundHit(out _))
        {
            rb.AddForceAtPosition(rightWheel.transform.up * antiRoll, rightWheel.transform.position);
        }
    }

    private float GetSuspensionTravel(WheelCollider wheel)
    {
        if (!wheel.GetGroundHit(out WheelHit hit))
        {
            return 1f;
        }

        Vector3 wheelLocalHitPoint = wheel.transform.InverseTransformPoint(hit.point);
        float travel = (-wheelLocalHitPoint.y - wheel.radius) / wheel.suspensionDistance;
        return Mathf.Clamp01(travel);
    }

    private void UpdateCurrentVelocity()
    {
        currentForwardVelocity = forwardVelocity;
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftCollider, frontLeftMesh);
        UpdateSingleWheel(frontRightCollider, frontRightMesh);
        UpdateSingleWheel(rearLeftCollider, rearLeftMesh);
        UpdateSingleWheel(rearRightCollider, rearRightMesh);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelMesh)
    {
        if (wheelCollider == null || wheelMesh == null)
            return;

        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheelMesh.position = position;
        wheelMesh.rotation = rotation;
    }
}
