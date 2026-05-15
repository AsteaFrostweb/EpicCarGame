using UnityEngine;

public class SimpleWheelCarController : MonoBehaviour
{
    private const float PresetMotorTorque = 3000f;
    private const float PresetBrakeTorque = 6200f;
    private const float PresetHandbrakeTorque = 3600f;
    private const float PresetMaxSpeed = 72f;
    private const float PresetMaxSteerAngle = 36f;

    private static readonly Vector3 PresetCenterOfMassOffset = new Vector3(0f, -0.45f, -0.08f);

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

    [Header("Core Drive")]
    public float motorTorque = PresetMotorTorque;
    public float brakeTorque = PresetBrakeTorque;
    public float handbrakeTorque = PresetHandbrakeTorque;
    public float maxSpeed = PresetMaxSpeed;
    public float reverseTorqueMultiplier = 0.45f;
    public float brakeResponseSpeed = 2.25f;

    [Header("Input Feel")]
    public float throttleResponse = 8f;
    public float steerResponse = 10f;
    public float steerReturnResponse = 14f;
    [Range(0f, 0.5f)] public float steeringDeadzone = 0.08f;
    public float steeringInputExponent = 1.35f;

    [Header("Speed Curves")]
    public float maxSteerAngle = PresetMaxSteerAngle;
    public AnimationCurve steerBySpeed = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.45f, 0.9f),
        new Keyframe(1f, 0.72f));
    public AnimationCurve torqueBySpeed = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.7f, 0.9f),
        new Keyframe(1f, 0.12f));

    [Header("Rigidbody Setup")]
    public bool applyCenterOfMassOffsetOnAwake;
    public Vector3 centerOfMassOffset = PresetCenterOfMassOffset;
    public float downforce = 8f;
    public float extraGravity = 0f;
    public float groundedDownforceFadeSpeed = 10f;

    [Header("Turn Assist")]
    public float normalTurnYawTorque = 8f;
    public float turnAssistMinSpeed = 8f;
    public float turnAssistFullSpeed = 55f;
    [Range(0f, 1f)] public float driftTurnAssistMultiplier = 0.35f;
    public float maxYawRate = 1.6f;
    public float yawDamping = 7f;
    public float straightLineYawDamping = 12f;
    public float stabilityAssist = 4f;

    [Header("Normal Grip")]
    public float frontForwardGrip = 1.55f;
    public float rearForwardGrip = 1.45f;
    public float frontSidewaysGrip = 1.75f;
    public float rearSidewaysGrip = 1.5f;

    [Header("Drift")]
    public bool enableDrift = true;
    public float driftEnterSpeed = 8f;
    public float driftBuildSpeed = 6f;
    public float driftRecoverSpeed = 4f;
    public float driftRearSidewaysGrip = 0.76f;
    public float driftRearForwardGrip = 1.05f;
    public float driftSteerMultiplier = 1.18f;
    public float driftThrottleMultiplier = 0.78f;
    public float driftYawTorque = 8f;
    [Range(0f, 1f)] public float counterSteerAssist = 0.18f;

    [Header("Wheel Collider Defaults")]
    public float wheelMass = 26f;
    public float wheelDampingRate = 0.9f;
    public float suspensionDistance = 0.28f;
    public float forceAppPointDistance = 0.08f;
    public float suspensionSpring = 36000f;
    public float suspensionDamper = 5200f;
    [Range(0f, 1f)] public float suspensionTargetPosition = 0.5f;

    private Rigidbody rb;

    private float verticalInput;
    private float horizontalInput;
    private float smoothedVerticalInput;
    private float smoothedSteerInput;
    private float currentSteerAngle;
    private float driftAmount;
    private float slipAngle;
    private float rearForwardSlip;
    private float rearSidewaysSlip;
    private float currentDriveTorque;
    private float groundedAmount;

    public bool isBraking;
    public bool isHandbraking;
    public float currentForwardVelocity;
    public float DriftAmount => driftAmount;
    public float SlipAngle => slipAngle;
    public float RearForwardSlip => rearForwardSlip;
    public float RearSidewaysSlip => rearSidewaysSlip;
    public float RecoveryAssist => 0f;
    public float Speed01 => Mathf.Clamp01(speed / maxSpeed);
    public float ForwardSpeed01 => Mathf.Clamp01(Mathf.Abs(currentForwardVelocity) / maxSpeed);
    public float Throttle01 => Mathf.Clamp01(smoothedVerticalInput);
    public float Reverse01 => Mathf.Clamp01(-smoothedVerticalInput);
    public float Brake01 => isBraking ? 1f : 0f;
    public float Handbrake01 => isHandbraking ? 1f : 0f;
    public float Steering01 => Mathf.Clamp(smoothedSteerInput, -1f, 1f);
    public float EngineLoad01 => motorTorque > 0f ? Mathf.Clamp01(Mathf.Abs(currentDriveTorque) / motorTorque) : 0f;
    public float WheelSpin01 => Mathf.Clamp01(rearForwardSlip);
    public float Skid01 => Mathf.Clamp01(Mathf.Max(Mathf.Abs(slipAngle) / 45f, rearSidewaysSlip));
    public float Drift01 => driftAmount;
    public float forwardVelocity => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
    public float speed => rb != null ? rb.linearVelocity.magnitude : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null && applyCenterOfMassOffsetOnAwake)
        {
            rb.centerOfMass += centerOfMassOffset;
        }

        ApplyWheelColliderDefaults();
        ApplyWheelFriction();
    }

    private void OnValidate()
    {
        motorTorque = Mathf.Max(0f, motorTorque);
        brakeTorque = Mathf.Max(0f, brakeTorque);
        handbrakeTorque = Mathf.Max(0f, handbrakeTorque);
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        maxSteerAngle = Mathf.Max(0f, maxSteerAngle);
        reverseTorqueMultiplier = Mathf.Max(0f, reverseTorqueMultiplier);
        brakeResponseSpeed = Mathf.Max(0f, brakeResponseSpeed);
        throttleResponse = Mathf.Max(0f, throttleResponse);
        steerResponse = Mathf.Max(0f, steerResponse);
        steerReturnResponse = Mathf.Max(0f, steerReturnResponse);
        steeringInputExponent = Mathf.Max(0.1f, steeringInputExponent);
        downforce = Mathf.Max(0f, downforce);
        groundedDownforceFadeSpeed = Mathf.Max(0f, groundedDownforceFadeSpeed);
        normalTurnYawTorque = Mathf.Max(0f, normalTurnYawTorque);
        turnAssistMinSpeed = Mathf.Max(0f, turnAssistMinSpeed);
        turnAssistFullSpeed = Mathf.Max(turnAssistMinSpeed + 0.01f, turnAssistFullSpeed);
        maxYawRate = Mathf.Max(0.01f, maxYawRate);
        yawDamping = Mathf.Max(0f, yawDamping);
        straightLineYawDamping = Mathf.Max(0f, straightLineYawDamping);
        stabilityAssist = Mathf.Max(0f, stabilityAssist);
        driftEnterSpeed = Mathf.Max(0f, driftEnterSpeed);
        driftBuildSpeed = Mathf.Max(0f, driftBuildSpeed);
        driftRecoverSpeed = Mathf.Max(0f, driftRecoverSpeed);
        driftYawTorque = Mathf.Max(0f, driftYawTorque);
        suspensionDistance = Mathf.Max(0.01f, suspensionDistance);
        suspensionSpring = Mathf.Max(0f, suspensionSpring);
        suspensionDamper = Mathf.Max(0f, suspensionDamper);

        if (!Application.isPlaying)
        {
            ApplyWheelFriction();
        }
    }

    [ContextMenu("Apply NFS Baseline Defaults")]
    public void ApplyNfsBaselineDefaults()
    {
        motorTorque = PresetMotorTorque;
        brakeTorque = PresetBrakeTorque;
        handbrakeTorque = PresetHandbrakeTorque;
        maxSpeed = PresetMaxSpeed;
        maxSteerAngle = PresetMaxSteerAngle;
        reverseTorqueMultiplier = 0.45f;
        brakeResponseSpeed = 2.25f;

        throttleResponse = 8f;
        steerResponse = 10f;
        steerReturnResponse = 14f;
        steeringDeadzone = 0.08f;
        steeringInputExponent = 1.35f;
        steerBySpeed = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.45f, 0.9f),
            new Keyframe(1f, 0.72f));
        torqueBySpeed = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 0.9f),
            new Keyframe(1f, 0.12f));

        applyCenterOfMassOffsetOnAwake = false;
        centerOfMassOffset = PresetCenterOfMassOffset;
        downforce = 8f;
        extraGravity = 0f;
        groundedDownforceFadeSpeed = 10f;
        normalTurnYawTorque = 8f;
        turnAssistMinSpeed = 8f;
        turnAssistFullSpeed = 55f;
        driftTurnAssistMultiplier = 0.35f;
        maxYawRate = 1.6f;
        yawDamping = 7f;
        straightLineYawDamping = 12f;
        stabilityAssist = 4f;

        frontForwardGrip = 1.55f;
        rearForwardGrip = 1.45f;
        frontSidewaysGrip = 1.75f;
        rearSidewaysGrip = 1.5f;

        enableDrift = true;
        driftEnterSpeed = 8f;
        driftBuildSpeed = 6f;
        driftRecoverSpeed = 4f;
        driftRearSidewaysGrip = 0.76f;
        driftRearForwardGrip = 1.05f;
        driftSteerMultiplier = 1.18f;
        driftThrottleMultiplier = 0.78f;
        driftYawTorque = 8f;
        counterSteerAssist = 0.18f;

        wheelMass = 26f;
        wheelDampingRate = 0.9f;
        suspensionDistance = 0.28f;
        forceAppPointDistance = 0.08f;
        suspensionSpring = 36000f;
        suspensionDamper = 5200f;
        suspensionTargetPosition = 0.5f;

        ApplyWheelColliderDefaults();
        ApplyWheelFriction();
    }

    [ContextMenu("Apply Arcade Drift Defaults")]
    public void ApplyArcadeDriftDefaults()
    {
        ApplyNfsBaselineDefaults();
    }

    [ContextMenu("Apply Wheel Collider Defaults")]
    public void ApplyWheelColliderDefaults()
    {
        ApplyWheelColliderDefaults(frontLeftCollider);
        ApplyWheelColliderDefaults(frontRightCollider);
        ApplyWheelColliderDefaults(rearLeftCollider);
        ApplyWheelColliderDefaults(rearRightCollider);
    }

    private void Update()
    {
        GetInput();
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        UpdateTelemetry();
        SmoothInputs();
        UpdateDriftAmount();
        ApplyWheelFriction();
        ApplyMotor();
        ApplySteering();
        ApplyBrakes();
        ApplyArcadeForces();
    }

    private void GetInput()
    {
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
        currentForwardVelocity = forwardVelocity;
        isBraking = verticalInput < -0.01f && currentForwardVelocity > brakeResponseSpeed;
        isHandbraking = Input.GetKey(KeyCode.Space);
    }

    private void SmoothInputs()
    {
        float throttleT = 1f - Mathf.Exp(-throttleResponse * Time.fixedDeltaTime);
        smoothedVerticalInput = Mathf.Lerp(smoothedVerticalInput, verticalInput, throttleT);

        float targetSteerInput = ShapeSteeringInput(horizontalInput);
        float steerRate = Mathf.Abs(targetSteerInput) > 0.001f ? steerResponse : steerReturnResponse;
        float steerT = 1f - Mathf.Exp(-steerRate * Time.fixedDeltaTime);
        smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, targetSteerInput, steerT);
    }

    private void UpdateTelemetry()
    {
        currentForwardVelocity = forwardVelocity;
        slipAngle = CalculateSlipAngle();
        rearForwardSlip = GetAverageAbsForwardSlip(rearLeftCollider, rearRightCollider);
        rearSidewaysSlip = GetAverageAbsSidewaysSlip(rearLeftCollider, rearRightCollider);
        groundedAmount = Mathf.MoveTowards(
            groundedAmount,
            GetGroundedWheelRatio(),
            groundedDownforceFadeSpeed * Time.fixedDeltaTime);
    }

    private void UpdateDriftAmount()
    {
        bool canDrift = enableDrift && speed >= driftEnterSpeed;
        bool wantsDrift = canDrift && isHandbraking && Mathf.Abs(smoothedSteerInput) > 0.05f;
        float targetDriftAmount = wantsDrift ? 1f : 0f;
        float rate = targetDriftAmount > driftAmount ? driftBuildSpeed : driftRecoverSpeed;

        driftAmount = Mathf.MoveTowards(driftAmount, targetDriftAmount, rate * Time.fixedDeltaTime);
    }

    private void ApplyMotor()
    {
        float throttleInput = Mathf.Clamp01(smoothedVerticalInput);
        float reverseInput = Mathf.Clamp01(-smoothedVerticalInput);
        float speed01 = Speed01;
        float torqueScale = EvaluateCurve(torqueBySpeed, speed01, 1f);
        float torque = 0f;

        if (throttleInput > 0.01f && Mathf.Abs(currentForwardVelocity) < maxSpeed)
        {
            float driftPower = Mathf.Lerp(1f, driftThrottleMultiplier, driftAmount);
            torque = throttleInput * motorTorque * torqueScale * driftPower;
        }
        else if (!isBraking && reverseInput > 0.01f && currentForwardVelocity < brakeResponseSpeed)
        {
            torque = -reverseInput * motorTorque * reverseTorqueMultiplier;
        }

        currentDriveTorque = torque;
        SetRearMotorTorque(torque);
    }

    private void ApplySteering()
    {
        float steerScale = EvaluateCurve(steerBySpeed, Speed01, 1f);
        float driftScale = Mathf.Lerp(1f, driftSteerMultiplier, driftAmount);
        float steerInput = GetSteerInputWithAssist();
        float targetSteerAngle = steerInput * maxSteerAngle * steerScale * driftScale;

        currentSteerAngle = Mathf.MoveTowards(
            currentSteerAngle,
            targetSteerAngle,
            maxSteerAngle * steerResponse * Time.fixedDeltaTime);

        SetSteerAngle(frontLeftCollider, currentSteerAngle);
        SetSteerAngle(frontRightCollider, currentSteerAngle);
    }

    private float GetSteerInputWithAssist()
    {
        if (driftAmount <= 0.01f || counterSteerAssist <= 0f)
        {
            return Mathf.Clamp(smoothedSteerInput, -1f, 1f);
        }

        float counterInput = Mathf.Clamp(-slipAngle / 45f, -1f, 1f);
        return Mathf.Clamp(smoothedSteerInput + counterInput * counterSteerAssist * driftAmount, -1f, 1f);
    }

    private float ShapeSteeringInput(float input)
    {
        float absInput = Mathf.Abs(input);

        if (absInput <= steeringDeadzone)
        {
            return 0f;
        }

        float normalizedInput = Mathf.InverseLerp(steeringDeadzone, 1f, absInput);
        float shapedInput = Mathf.Pow(normalizedInput, steeringInputExponent);
        return Mathf.Sign(input) * shapedInput;
    }

    private void ApplyBrakes()
    {
        float currentBrakeTorque = isBraking ? brakeTorque : 0f;
        float currentHandbrakeTorque = isHandbraking ? handbrakeTorque : 0f;

        SetBrakeTorque(frontLeftCollider, currentBrakeTorque);
        SetBrakeTorque(frontRightCollider, currentBrakeTorque);
        SetBrakeTorque(rearLeftCollider, currentBrakeTorque + currentHandbrakeTorque);
        SetBrakeTorque(rearRightCollider, currentBrakeTorque + currentHandbrakeTorque);
    }

    private void ApplyWheelFriction()
    {
        float rearForward = Mathf.Lerp(rearForwardGrip, driftRearForwardGrip, driftAmount);
        float rearSideways = Mathf.Lerp(rearSidewaysGrip, driftRearSidewaysGrip, driftAmount);

        ApplyFrictionToWheel(frontLeftCollider, frontForwardGrip, frontSidewaysGrip);
        ApplyFrictionToWheel(frontRightCollider, frontForwardGrip, frontSidewaysGrip);
        ApplyFrictionToWheel(rearLeftCollider, rearForward, rearSideways);
        ApplyFrictionToWheel(rearRightCollider, rearForward, rearSideways);
    }

    private void ApplyArcadeForces()
    {
        if (rb == null)
        {
            return;
        }

        if (downforce > 0f)
        {
            rb.AddForce(-transform.up * downforce * Speed01 * Speed01 * groundedAmount, ForceMode.Acceleration);
        }

        if (extraGravity > 0f)
        {
            rb.AddForce(Physics.gravity.normalized * extraGravity, ForceMode.Acceleration);
        }

        ApplyYawStability();
        ApplyTurnAssist();

        if (driftAmount > 0.01f && driftYawTorque > 0f)
        {
            float yawInput = smoothedSteerInput * driftYawTorque * driftAmount * Mathf.Clamp01(speed / 20f);
            rb.AddTorque(transform.up * yawInput, ForceMode.Acceleration);
        }
    }

    private void ApplyTurnAssist()
    {
        if (normalTurnYawTorque <= 0f || Mathf.Abs(smoothedSteerInput) < 0.01f)
        {
            return;
        }

        float speedT = Mathf.InverseLerp(turnAssistMinSpeed, turnAssistFullSpeed, Mathf.Abs(currentForwardVelocity));
        float driftT = Mathf.Lerp(1f, driftTurnAssistMultiplier, driftAmount);
        float yawTorque = smoothedSteerInput * normalTurnYawTorque * speedT * driftT;
        rb.AddTorque(transform.up * yawTorque, ForceMode.Acceleration);
    }

    private void ApplyYawStability()
    {
        float yawRate = Vector3.Dot(rb.angularVelocity, transform.up);
        float speedT = Speed01;

        if (yawDamping > 0f)
        {
            rb.AddTorque(transform.up * -yawRate * yawDamping * speedT, ForceMode.Acceleration);
        }

        if (straightLineYawDamping > 0f && driftAmount < 0.2f)
        {
            float straightLineT = 1f - Mathf.Clamp01(Mathf.Abs(smoothedSteerInput) / 0.35f);
            rb.AddTorque(transform.up * -yawRate * straightLineYawDamping * speedT * straightLineT, ForceMode.Acceleration);
        }

        if (stabilityAssist > 0f && driftAmount < 0.2f && Mathf.Abs(slipAngle) > 1f)
        {
            float stabilityTorque = Mathf.Clamp(-slipAngle / 45f, -1f, 1f) * stabilityAssist * speedT;
            rb.AddTorque(transform.up * stabilityTorque, ForceMode.Acceleration);
        }

        if (Mathf.Abs(yawRate) > maxYawRate)
        {
            Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
            localAngularVelocity.y = Mathf.Sign(localAngularVelocity.y) * maxYawRate;
            rb.angularVelocity = transform.TransformDirection(localAngularVelocity);
        }
    }

    private void ApplyFrictionToWheel(WheelCollider wheel, float forwardStiffness, float sidewaysStiffness)
    {
        if (wheel == null)
        {
            return;
        }

        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = 0.34f;
        forwardFriction.extremumValue = 1f;
        forwardFriction.asymptoteSlip = 0.82f;
        forwardFriction.asymptoteValue = 0.78f;
        forwardFriction.stiffness = forwardStiffness;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = 0.26f;
        sidewaysFriction.extremumValue = 1f;
        sidewaysFriction.asymptoteSlip = 0.72f;
        sidewaysFriction.asymptoteValue = 0.72f;
        sidewaysFriction.stiffness = sidewaysStiffness;
        wheel.sidewaysFriction = sidewaysFriction;
    }

    private void ApplyWheelColliderDefaults(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return;
        }

        wheel.mass = wheelMass;
        wheel.wheelDampingRate = wheelDampingRate;
        wheel.suspensionDistance = suspensionDistance;
        wheel.forceAppPointDistance = forceAppPointDistance;

        JointSpring spring = wheel.suspensionSpring;
        spring.spring = suspensionSpring;
        spring.damper = suspensionDamper;
        spring.targetPosition = suspensionTargetPosition;
        wheel.suspensionSpring = spring;
    }

    private float CalculateSlipAngle()
    {
        if (rb == null)
        {
            return 0f;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, transform.up);

        if (planarVelocity.sqrMagnitude < 0.25f)
        {
            return 0f;
        }

        return Vector3.SignedAngle(transform.forward, planarVelocity.normalized, transform.up);
    }

    private float GetAverageAbsForwardSlip(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        int hitCount = 0;
        float totalSlip = 0f;

        if (leftWheel != null && leftWheel.GetGroundHit(out WheelHit leftHit))
        {
            totalSlip += Mathf.Abs(leftHit.forwardSlip);
            hitCount++;
        }

        if (rightWheel != null && rightWheel.GetGroundHit(out WheelHit rightHit))
        {
            totalSlip += Mathf.Abs(rightHit.forwardSlip);
            hitCount++;
        }

        return hitCount > 0 ? totalSlip / hitCount : 0f;
    }

    private float GetAverageAbsSidewaysSlip(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        int hitCount = 0;
        float totalSlip = 0f;

        if (leftWheel != null && leftWheel.GetGroundHit(out WheelHit leftHit))
        {
            totalSlip += Mathf.Abs(leftHit.sidewaysSlip);
            hitCount++;
        }

        if (rightWheel != null && rightWheel.GetGroundHit(out WheelHit rightHit))
        {
            totalSlip += Mathf.Abs(rightHit.sidewaysSlip);
            hitCount++;
        }

        return hitCount > 0 ? totalSlip / hitCount : 0f;
    }

    private float GetGroundedWheelRatio()
    {
        int wheelCount = 0;
        int groundedCount = 0;

        AddGroundedWheel(frontLeftCollider, ref wheelCount, ref groundedCount);
        AddGroundedWheel(frontRightCollider, ref wheelCount, ref groundedCount);
        AddGroundedWheel(rearLeftCollider, ref wheelCount, ref groundedCount);
        AddGroundedWheel(rearRightCollider, ref wheelCount, ref groundedCount);

        return wheelCount > 0 ? groundedCount / (float)wheelCount : 0f;
    }

    private void AddGroundedWheel(WheelCollider wheel, ref int wheelCount, ref int groundedCount)
    {
        if (wheel == null)
        {
            return;
        }

        wheelCount++;

        if (wheel.isGrounded)
        {
            groundedCount++;
        }
    }

    private float EvaluateCurve(AnimationCurve curve, float time, float fallback)
    {
        return curve != null && curve.length > 0 ? Mathf.Max(0f, curve.Evaluate(time)) : fallback;
    }

    private void SetRearMotorTorque(float torque)
    {
        SetMotorTorque(rearLeftCollider, torque);
        SetMotorTorque(rearRightCollider, torque);
    }

    private void SetMotorTorque(WheelCollider wheel, float torque)
    {
        if (wheel != null)
        {
            wheel.motorTorque = torque;
        }
    }

    private void SetSteerAngle(WheelCollider wheel, float steerAngle)
    {
        if (wheel != null)
        {
            wheel.steerAngle = steerAngle;
        }
    }

    private void SetBrakeTorque(WheelCollider wheel, float torque)
    {
        if (wheel != null)
        {
            wheel.brakeTorque = torque;
        }
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
        {
            return;
        }

        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheelMesh.position = position;
        wheelMesh.rotation = rotation;
    }
}
