using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(SimpleWheelCarController))]
public class BotCarController : MonoBehaviour
{
    [SerializeField] private SimpleWheelCarController carController;
    [SerializeField] private RacingLineController racingLine;

    [Header("Path Following")]
    [FormerlySerializedAs("lookAheadDistance")]
    [Tooltip("Distance to the first racing-line sample the bot aims toward. Lower values react earlier but can twitch; higher values smooth out straights but can turn late.")]
    [SerializeField] private float lookAheadDistance = 12f;
    [Tooltip("How many racing-line samples are blended into the movement direction.")]
    [SerializeField] private int pathSampleCount = 4;
    [Tooltip("Each extra sample is this many times farther than the previous one. 2 means octave-style distances: 12, 24, 48, etc.")]
    [SerializeField] private float pathSampleDistanceMultiplier = 1.6f;
    [Tooltip("Weight falloff for farther samples. Lower values favor near samples and earlier corner reaction; higher values favor smoother long-range direction.")]
    [Range(0f, 1f)]
    [SerializeField] private float pathSampleWeightFalloff = 0.55f;

    [Header("Collision Avoidance")]
    [Tooltip("Layers considered by the simple obstacle avoidance rays.")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Tooltip("How strongly the avoidance direction is added to the racing-line direction. 0 disables avoidance.")]
    [SerializeField] private float collisionAvoidanceWeight = 1.5f;
    [Tooltip("How far ahead the bot checks for obstacles.")]
    [SerializeField] private float avoidanceRayDistance = 12f;
    [Tooltip("Height above the car origin where avoidance rays are cast.")]
    [SerializeField] private float avoidanceRayHeight = 0.75f;
    [Tooltip("Forward offset from the car origin where avoidance rays start.")]
    [SerializeField] private float avoidanceRayForwardOffset = 1.5f;
    [Tooltip("Angle of the left and right feeler rays.")]
    [SerializeField] private float avoidanceRayAngle = 35f;

    [Header("Steering PID")]
    [Tooltip("Heading error, in degrees, that maps to full steering input before PID gains. Lower values make steering more sensitive.")]
    [SerializeField] private float steerAngleForFullInput = 45f;
    [Tooltip("Immediate steering response to heading error. Raise if the bot understeers; lower if it oscillates.")]
    [SerializeField] private float proportionalGain = 1f;
    [Tooltip("Long-term correction for persistent bias. Usually keep this at 0 or very small for vehicle steering.")]
    [SerializeField] private float integralGain;
    [Tooltip("Damps rapid changes in heading error. Raise to reduce weaving, lower if steering feels sluggish or noisy.")]
    [SerializeField] private float derivativeGain = 0.08f;
    [Tooltip("Caps accumulated integral correction so it cannot wind up too far.")]
    [SerializeField] private float integralLimit = 1f;

    [Header("Speed")]
    [Tooltip("Throttle sent when below the desired racing-line speed.")]
    [SerializeField] private float maxThrottle = 1f;
    [Tooltip("How far above desired speed the bot can be before braking.")]
    [SerializeField] private float speedBrakeMargin = 4f;
    [Tooltip("Reverse/brake input sent when the bot exceeds desired speed by more than the brake margin.")]
    [SerializeField] private float speedBrakeInput = 0.35f;

    public float currentSteerInput;
    public float currentThrottleInput;
    public float segmentProgress;
    public float currentDesiredSpeed;
    public float currentHeadingError;
    public Vector3 currentAimPoint;
    public Vector3 currentPathDirection;
    public Vector3 currentAvoidanceDirection;
    public Vector3 currentMovementDirection;

    private float steeringIntegral;
    private float previousHeadingError;
    private bool hasPreviousHeadingError;
    private readonly RaycastHit[] avoidanceHits = new RaycastHit[8];

    private void Awake()
    {
        if (carController == null)
        {
            carController = GetComponent<SimpleWheelCarController>();
        }

        if (racingLine == null)
        {
            racingLine = FindFirstObjectByType<RacingLineController>();
        }
    }

    private void OnValidate()
    {
        lookAheadDistance = Mathf.Max(0f, lookAheadDistance);
        pathSampleCount = Mathf.Max(1, pathSampleCount);
        pathSampleDistanceMultiplier = Mathf.Max(1f, pathSampleDistanceMultiplier);
        pathSampleWeightFalloff = Mathf.Clamp01(pathSampleWeightFalloff);
        collisionAvoidanceWeight = Mathf.Max(0f, collisionAvoidanceWeight);
        avoidanceRayDistance = Mathf.Max(0f, avoidanceRayDistance);
        avoidanceRayHeight = Mathf.Max(0f, avoidanceRayHeight);
        avoidanceRayForwardOffset = Mathf.Max(0f, avoidanceRayForwardOffset);
        avoidanceRayAngle = Mathf.Max(0f, avoidanceRayAngle);
        steerAngleForFullInput = Mathf.Max(1f, steerAngleForFullInput);
        proportionalGain = Mathf.Max(0f, proportionalGain);
        integralGain = Mathf.Max(0f, integralGain);
        derivativeGain = Mathf.Max(0f, derivativeGain);
        integralLimit = Mathf.Max(0f, integralLimit);
        maxThrottle = Mathf.Clamp(maxThrottle, -1f, 1f);
        speedBrakeMargin = Mathf.Max(0f, speedBrakeMargin);
        speedBrakeInput = Mathf.Clamp01(speedBrakeInput);
    }

    private void FixedUpdate()
    {
        if (carController == null || !TryGetMovementDirection(out Vector3 movementDirection, out currentDesiredSpeed))
        {
            ResetPid();
            SetBotInput(0f, 0f);
            return;
        }

        currentHeadingError = Vector3.SignedAngle(transform.forward, movementDirection, Vector3.up);
        currentSteerInput = CalculatePidSteer(currentHeadingError);
        currentThrottleInput = CalculateThrottle(currentDesiredSpeed);

        SetBotInput(currentThrottleInput, currentSteerInput);
    }

    private bool TryGetMovementDirection(out Vector3 movementDirection, out float desiredSpeed)
    {
        movementDirection = Vector3.zero;
        desiredSpeed = 0f;

        if (racingLine == null || racingLine.SampleCount < 2)
        {
            return false;
        }

        int closestSampleIndex = racingLine.GetClosestSampleIndex(transform.position);
        if (closestSampleIndex < 0)
        {
            return false;
        }

        desiredSpeed = racingLine.GetDesiredSpeedAtSample(closestSampleIndex);
        segmentProgress = racingLine.SampleCount > 1
            ? closestSampleIndex / (float)(racingLine.SampleCount - 1)
            : 0f;

        currentPathDirection = GetWeightedPathDirection(closestSampleIndex);
        if (currentPathDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        currentAvoidanceDirection = GetCollisionAvoidanceDirection();
        movementDirection = currentPathDirection + currentAvoidanceDirection * collisionAvoidanceWeight;
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        currentMovementDirection = movementDirection.normalized;
        currentAimPoint = transform.position + currentMovementDirection * lookAheadDistance;

        return true;
    }

    private Vector3 GetWeightedPathDirection(int closestSampleIndex)
    {
        Vector3 weightedDirection = Vector3.zero;
        float totalWeight = 0f;
        float sampleDistance = lookAheadDistance;
        float sampleWeight = 1f;

        for (int i = 0; i < pathSampleCount; i++)
        {
            Vector3 samplePoint = racingLine.GetPointAheadByDistance(closestSampleIndex, sampleDistance);
            Vector3 sampleDirection = samplePoint - transform.position;
            sampleDirection.y = 0f;

            if (sampleDirection.sqrMagnitude > 0.001f)
            {
                weightedDirection += sampleDirection.normalized * sampleWeight;
                totalWeight += sampleWeight;
            }

            sampleDistance *= pathSampleDistanceMultiplier;
            sampleWeight *= pathSampleWeightFalloff;
        }

        return totalWeight > 0.001f ? (weightedDirection / totalWeight).normalized : Vector3.zero;
    }

    private Vector3 GetCollisionAvoidanceDirection()
    {
        if (collisionAvoidanceWeight <= 0f || avoidanceRayDistance <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 origin = transform.position
            + Vector3.up * avoidanceRayHeight
            + transform.forward * avoidanceRayForwardOffset;
        Vector3 avoidanceDirection = Vector3.zero;

        AddAvoidanceFromRay(origin, transform.forward, -transform.right, ref avoidanceDirection);
        AddAvoidanceFromRay(origin, Quaternion.AngleAxis(-avoidanceRayAngle, Vector3.up) * transform.forward, transform.right, ref avoidanceDirection);
        AddAvoidanceFromRay(origin, Quaternion.AngleAxis(avoidanceRayAngle, Vector3.up) * transform.forward, -transform.right, ref avoidanceDirection);

        avoidanceDirection.y = 0f;
        return avoidanceDirection.sqrMagnitude > 0.001f ? avoidanceDirection.normalized : Vector3.zero;
    }

    private void AddAvoidanceFromRay(Vector3 origin, Vector3 rayDirection, Vector3 avoidDirection, ref Vector3 avoidanceDirection)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            rayDirection,
            avoidanceHits,
            avoidanceRayDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        bool foundHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = avoidanceHits[i];
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                foundHit = true;
            }
        }

        if (!foundHit)
        {
            return;
        }

        float proximity = 1f - Mathf.Clamp01(closestDistance / avoidanceRayDistance);
        avoidanceDirection += avoidDirection.normalized * proximity;
    }

    private float CalculatePidSteer(float headingError)
    {
        float normalizedError = headingError / steerAngleForFullInput;
        float deltaTime = Mathf.Max(Time.fixedDeltaTime, 0.0001f);

        steeringIntegral = Mathf.Clamp(
            steeringIntegral + normalizedError * deltaTime,
            -integralLimit,
            integralLimit);

        float derivative = hasPreviousHeadingError
            ? (normalizedError - previousHeadingError) / deltaTime
            : 0f;

        previousHeadingError = normalizedError;
        hasPreviousHeadingError = true;

        return Mathf.Clamp(
            normalizedError * proportionalGain
            + steeringIntegral * integralGain
            + derivative * derivativeGain,
            -1f,
            1f);
    }

    private float CalculateThrottle(float desiredSpeed)
    {
        if (desiredSpeed <= 0.01f)
        {
            return maxThrottle;
        }

        float forwardSpeed = Mathf.Max(0f, carController.currentForwardVelocity);
        float speedError = desiredSpeed - forwardSpeed;

        if (speedError <= -speedBrakeMargin)
        {
            return -speedBrakeInput;
        }

        if (speedError <= 0f)
        {
            return 0f;
        }

        return maxThrottle;
    }

    private void ResetPid()
    {
        steeringIntegral = 0f;
        previousHeadingError = 0f;
        hasPreviousHeadingError = false;
        currentSteerInput = 0f;
        currentThrottleInput = 0f;
        currentHeadingError = 0f;
    }

    private void OnDisable()
    {
        ResetPid();

        if (carController != null)
        {
            carController.ClearExternalInput();
        }
    }

    private void SetBotInput(float throttle, float steer)
    {
        currentThrottleInput = Mathf.Clamp(throttle, -1f, 1f);
        currentSteerInput = Mathf.Clamp(steer, -1f, 1f);
        carController.SetExternalInput(currentThrottleInput, currentSteerInput);
    }
}
