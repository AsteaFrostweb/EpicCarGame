using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(SimpleWheelCarController))]
[RequireComponent(typeof(PlayerRaceController))]
public class BotCarController : MonoBehaviour
{
    [SerializeField] private SimpleWheelCarController carController;
    [SerializeField] private PlayerRaceController raceController;
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private RacingLineController racingLine;

    [Header("Path Following")]
    [FormerlySerializedAs("lookAheadDistance")]
    [SerializeField] private float baseLookAheadDistance = 10f;
    [SerializeField] private float speedLookAheadDistance = 14f;
    [SerializeField] private float lateralErrorLookAheadMultiplier = 1.2f;
    [SerializeField] private float maxLateralErrorLookAhead = 18f;
    [SerializeField] private bool fallbackToCheckpoints = true;
    [SerializeField] private bool useDistanceCheckpointCompletion;
    [SerializeField] private float checkpointReachDistance = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float forwardAlignmentWeight = 0.65f;
    [SerializeField] private float steerAngleForFullInput = 45f;

    [Header("Driving")]
    [SerializeField] private float maxThrottle = 1f;
    [SerializeField] private float cautiousThrottle = 0.45f;
    [SerializeField] private float speedLimitBrakeMargin = 4f;
    [SerializeField] private float speedLimitBrakeInput = 0.25f;
    [SerializeField] private float steeringAngleForCaution = 45f;
    [SerializeField] private float brakeAngle = 80f;
    [SerializeField] private float wrongWayBrakeAngle = 120f;
    [SerializeField] private float handbrakeAngle = 65f;
    [SerializeField] private float nitrosMaxSteeringAngle = 10f;
    [SerializeField] private float nitrosMinSpeed01 = 0.35f;

    [Header("Steering Assist")]
    [SerializeField] private float headingSteerGain = 1f;
    [SerializeField] private float lateralSteerGain = 0.035f;
    [SerializeField] private float yawDampingGain = 0.18f;
    [SerializeField] private float steerSmoothing = 8f;
    [SerializeField] private float lateralErrorLookAhead = 5f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private bool enableObstacleAvoidance = true;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private float avoidanceRayOriginHeight = 0.75f;
    [SerializeField] private float avoidanceRayForwardOffset = 1.8f;
    [SerializeField] private float forwardAvoidanceRayLength = 14f;
    [SerializeField] private float sideAvoidanceRayLength = 4f;
    [SerializeField] private float forwardAvoidanceSteerGain = 1.15f;
    [SerializeField] private float sideAvoidanceSteerGain = 0.85f;
    [SerializeField] private float avoidanceBrakeDistance = 5f;
    [SerializeField] private float avoidanceBrakeInput = 0.35f;

    public float currentSteerInput;
    public float currentThrottleInput;
    public bool currentHandbrakeInput;
    public bool currentNitrosInput;
    public float segmentProgress;
    public float currentDesiredSpeed;
    public float currentObstacleAvoidanceSteer;
    public float currentObstacleBrakeInput;
    public Vector3 currentAimPoint;

    private readonly float[] forwardAvoidanceAngles = { -60f, -30f, 0f, 30f, 60f };
    private readonly RaycastHit[] obstacleHits = new RaycastHit[12];

    private float smoothedSteerInput;

    private void Awake()
    {
        if (carController == null)
        {
            carController = GetComponent<SimpleWheelCarController>();
        }

        if (raceController == null)
        {
            raceController = GetComponent<PlayerRaceController>();
        }

        if (carRigidbody == null)
        {
            carRigidbody = GetComponent<Rigidbody>();
        }

        if (racingLine == null)
        {
            racingLine = FindFirstObjectByType<RacingLineController>();
        }
    }

    private void OnValidate()
    {
        baseLookAheadDistance = Mathf.Max(0f, baseLookAheadDistance);
        speedLookAheadDistance = Mathf.Max(0f, speedLookAheadDistance);
        lateralErrorLookAheadMultiplier = Mathf.Max(0f, lateralErrorLookAheadMultiplier);
        maxLateralErrorLookAhead = Mathf.Max(0f, maxLateralErrorLookAhead);
        checkpointReachDistance = Mathf.Max(0.1f, checkpointReachDistance);
        steerAngleForFullInput = Mathf.Max(1f, steerAngleForFullInput);
        maxThrottle = Mathf.Clamp(maxThrottle, -1f, 1f);
        cautiousThrottle = Mathf.Clamp(cautiousThrottle, -1f, 1f);
        speedLimitBrakeMargin = Mathf.Max(0f, speedLimitBrakeMargin);
        speedLimitBrakeInput = Mathf.Clamp01(speedLimitBrakeInput);
        steeringAngleForCaution = Mathf.Max(1f, steeringAngleForCaution);
        brakeAngle = Mathf.Max(steeringAngleForCaution, brakeAngle);
        wrongWayBrakeAngle = Mathf.Max(brakeAngle, wrongWayBrakeAngle);
        handbrakeAngle = Mathf.Max(0f, handbrakeAngle);
        nitrosMaxSteeringAngle = Mathf.Max(0f, nitrosMaxSteeringAngle);
        nitrosMinSpeed01 = Mathf.Clamp01(nitrosMinSpeed01);
        headingSteerGain = Mathf.Max(0f, headingSteerGain);
        lateralSteerGain = Mathf.Max(0f, lateralSteerGain);
        yawDampingGain = Mathf.Max(0f, yawDampingGain);
        steerSmoothing = Mathf.Max(0f, steerSmoothing);
        lateralErrorLookAhead = Mathf.Max(0f, lateralErrorLookAhead);
        avoidanceRayOriginHeight = Mathf.Max(0f, avoidanceRayOriginHeight);
        avoidanceRayForwardOffset = Mathf.Max(0f, avoidanceRayForwardOffset);
        forwardAvoidanceRayLength = Mathf.Max(0f, forwardAvoidanceRayLength);
        sideAvoidanceRayLength = Mathf.Max(0f, sideAvoidanceRayLength);
        forwardAvoidanceSteerGain = Mathf.Max(0f, forwardAvoidanceSteerGain);
        sideAvoidanceSteerGain = Mathf.Max(0f, sideAvoidanceSteerGain);
        avoidanceBrakeDistance = Mathf.Max(0f, avoidanceBrakeDistance);
        avoidanceBrakeInput = Mathf.Clamp01(avoidanceBrakeInput);
    }

    private void FixedUpdate()
    {
        if (carController == null)
        {
            return;
        }

        float speed01 = carController.Speed01;
        float lookAheadDistance = baseLookAheadDistance + speedLookAheadDistance * speed01;
        bool hasAimDirection = TryGetRacingLineAimDirection(
            lookAheadDistance,
            out Vector3 directionToAimPoint,
            out float lateralError,
            out currentDesiredSpeed);

        if (hasAimDirection)
        {
            float extraLookAhead = Mathf.Min(
                Mathf.Abs(lateralError) * lateralErrorLookAheadMultiplier,
                maxLateralErrorLookAhead);

            if (extraLookAhead > 0.001f)
            {
                hasAimDirection = TryGetRacingLineAimDirection(
                    lookAheadDistance + extraLookAhead,
                    out directionToAimPoint,
                    out lateralError,
                    out currentDesiredSpeed);
            }
        }

        if (!hasAimDirection && fallbackToCheckpoints)
        {
            hasAimDirection = TryGetCheckpointAimDirection(lookAheadDistance, out directionToAimPoint);
            lateralError = 0f;
            currentDesiredSpeed = 0f;
        }

        if (!hasAimDirection)
        {
            SetBotInput(0f, 0f);
            return;
        }

        DriveToward(directionToAimPoint, lateralError, speed01, currentDesiredSpeed);
        CompleteCheckpointByDistanceIfNeeded();
    }

    private bool TryGetRacingLineAimDirection(
        float lookAheadDistance,
        out Vector3 directionToAimPoint,
        out float lateralError,
        out float desiredSpeed)
    {
        directionToAimPoint = Vector3.zero;
        lateralError = 0f;
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

        Vector3 closestPoint = racingLine.GetSampleWorldPoint(closestSampleIndex);
        Vector3 tangentPoint = racingLine.GetPointAheadByDistance(closestSampleIndex, lateralErrorLookAhead);
        Vector3 pathForward = tangentPoint - closestPoint;
        pathForward.y = 0f;

        if (pathForward.sqrMagnitude > 0.001f)
        {
            Vector3 toCar = transform.position - closestPoint;
            toCar.y = 0f;
            float side = Mathf.Sign(Vector3.SignedAngle(pathForward.normalized, toCar.normalized, Vector3.up));
            lateralError = toCar.magnitude * side;
        }

        currentAimPoint = racingLine.GetPointAheadByDistance(closestSampleIndex, lookAheadDistance);
        directionToAimPoint = currentAimPoint - transform.position;
        directionToAimPoint.y = 0f;

        return directionToAimPoint.sqrMagnitude >= 0.001f;
    }

    private bool TryGetCheckpointAimDirection(float lookAheadDistance, out Vector3 directionToAimPoint)
    {
        directionToAimPoint = Vector3.zero;

        if (raceController == null)
        {
            return false;
        }

        Transform previousCheckpoint = raceController.PreviousCheckpointTransform;
        Transform nextCheckpoint = raceController.NextCheckpointTransform;
        if (previousCheckpoint == null || nextCheckpoint == null)
        {
            return false;
        }

        Vector3 previousPosition = previousCheckpoint.position;
        Vector3 nextPosition = nextCheckpoint.position;
        Vector3 segment = nextPosition - previousPosition;
        float segmentLengthSquared = segment.sqrMagnitude;

        segmentProgress = segmentLengthSquared > 0.001f
            ? Mathf.Clamp01(Vector3.Dot(transform.position - previousPosition, segment) / segmentLengthSquared)
            : 1f;

        Vector3 blendedForward = Vector3.Slerp(
            previousCheckpoint.forward,
            nextCheckpoint.forward,
            segmentProgress).normalized;

        Vector3 checkpointDirection = nextPosition - transform.position;
        checkpointDirection.y = 0f;

        Vector3 alignmentDirection = blendedForward;
        alignmentDirection.y = 0f;

        Vector3 targetDirection = Vector3.Lerp(
            checkpointDirection.normalized,
            alignmentDirection.normalized,
            forwardAlignmentWeight).normalized;

        currentAimPoint = nextPosition + targetDirection * lookAheadDistance;
        directionToAimPoint = currentAimPoint - transform.position;
        directionToAimPoint.y = 0f;

        return directionToAimPoint.sqrMagnitude >= 0.001f;
    }

    private void DriveToward(Vector3 directionToAimPoint, float lateralError, float speed01, float desiredSpeed)
    {
        float signedAngle = Vector3.SignedAngle(transform.forward, directionToAimPoint.normalized, Vector3.up);
        float headingSteer = signedAngle / steerAngleForFullInput * headingSteerGain;
        float lateralSteer = Mathf.Clamp(lateralError * lateralSteerGain, -0.65f, 0.65f);
        float yawRate = GetYawRate();
        float forwardSteerInput = Mathf.Clamp(headingSteer - lateralSteer - yawRate * yawDampingGain, -1f, 1f);
        UpdateObstacleAvoidance(forwardSteerInput);
        forwardSteerInput = Mathf.Clamp(forwardSteerInput + currentObstacleAvoidanceSteer, -1f, 1f);

        float steeringSeverity = Mathf.Abs(signedAngle);
        currentThrottleInput = Mathf.Lerp(maxThrottle, cautiousThrottle, Mathf.InverseLerp(0f, steeringAngleForCaution, steeringSeverity));
        currentHandbrakeInput = steeringSeverity >= handbrakeAngle && speed01 > 0.2f;
        currentNitrosInput = steeringSeverity <= nitrosMaxSteeringAngle && speed01 >= nitrosMinSpeed01;

        if (steeringSeverity >= wrongWayBrakeAngle)
        {
            currentThrottleInput = -0.35f;
            currentHandbrakeInput = false;
            currentNitrosInput = false;
        }
        else if (steeringSeverity >= brakeAngle)
        {
            currentThrottleInput = -0.25f;
            currentNitrosInput = false;
        }

        if (currentObstacleBrakeInput > 0f && currentThrottleInput > -currentObstacleBrakeInput)
        {
            currentThrottleInput = -currentObstacleBrakeInput;
            currentHandbrakeInput = false;
            currentNitrosInput = false;
        }

        if (desiredSpeed > 0.01f && currentThrottleInput > 0f)
        {
            float forwardSpeed = Mathf.Max(0f, carController.currentForwardVelocity);
            float speedError = desiredSpeed - forwardSpeed;

            if (speedError <= -speedLimitBrakeMargin)
            {
                currentThrottleInput = -speedLimitBrakeInput;
                currentHandbrakeInput = false;
                currentNitrosInput = false;
            }
            else if (speedError <= 0f)
            {
                currentThrottleInput = Mathf.Min(currentThrottleInput, cautiousThrottle);
                currentNitrosInput = false;
            }
        }

        float targetSteerInput = currentThrottleInput < -0.01f ? -forwardSteerInput : forwardSteerInput;
        float steerT = steerSmoothing > 0f ? 1f - Mathf.Exp(-steerSmoothing * Time.fixedDeltaTime) : 1f;
        smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, targetSteerInput, steerT);
        currentSteerInput = Mathf.Clamp(smoothedSteerInput, -1f, 1f);

        SetBotInput(currentThrottleInput, currentSteerInput, currentNitrosInput, currentHandbrakeInput);
    }

    private void CompleteCheckpointByDistanceIfNeeded()
    {
        if (!useDistanceCheckpointCompletion || raceController == null)
        {
            return;
        }

        Transform nextCheckpoint = raceController.NextCheckpointTransform;
        if (nextCheckpoint == null)
        {
            return;
        }

        Vector3 checkpointDirection = nextCheckpoint.position - transform.position;
        checkpointDirection.y = 0f;

        if (checkpointDirection.magnitude <= checkpointReachDistance)
        {
            raceController.CompleteNextCheckpoint();
        }
    }

    private void OnDisable()
    {
        if (carController != null)
        {
            carController.ClearExternalInput();
        }
    }

    private float GetYawRate()
    {
        if (carRigidbody == null)
        {
            return 0f;
        }

        return Vector3.Dot(carRigidbody.angularVelocity, transform.up);
    }

    private void UpdateObstacleAvoidance(float desiredForwardSteer)
    {
        currentObstacleAvoidanceSteer = 0f;
        currentObstacleBrakeInput = 0f;

        if (!enableObstacleAvoidance)
        {
            return;
        }

        Vector3 origin = transform.position + transform.up * avoidanceRayOriginHeight + transform.forward * avoidanceRayForwardOffset;
        float steerTotal = 0f;
        float weightTotal = 0f;

        for (int i = 0; i < forwardAvoidanceAngles.Length; i++)
        {
            float angle = forwardAvoidanceAngles[i];
            Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;

            if (!TryRaycastObstacle(origin, direction, forwardAvoidanceRayLength, out RaycastHit hit))
            {
                continue;
            }

            float proximity = 1f - Mathf.Clamp01(hit.distance / forwardAvoidanceRayLength);
            float steerAway = GetForwardObstacleSteerAway(angle, hit, desiredForwardSteer);
            float centerWeight = 1f - Mathf.Abs(angle) / 90f;
            float weight = proximity * Mathf.Lerp(0.65f, 1.2f, centerWeight);

            steerTotal += steerAway * weight;
            weightTotal += weight;

            if (hit.distance <= avoidanceBrakeDistance)
            {
                currentObstacleBrakeInput = Mathf.Max(currentObstacleBrakeInput, avoidanceBrakeInput * proximity);
            }
        }

        AddSideAvoidance(origin, -transform.right, 1f, ref steerTotal, ref weightTotal);
        AddSideAvoidance(origin, transform.right, -1f, ref steerTotal, ref weightTotal);

        if (weightTotal > 0.001f)
        {
            currentObstacleAvoidanceSteer = Mathf.Clamp(steerTotal / weightTotal, -1f, 1f);
        }
    }

    private float GetForwardObstacleSteerAway(float rayAngle, RaycastHit hit, float desiredForwardSteer)
    {
        if (Mathf.Abs(rayAngle) > 0.1f)
        {
            return -Mathf.Sign(rayAngle) * forwardAvoidanceSteerGain;
        }

        float normalSide = Vector3.Dot(hit.normal, transform.right);
        if (Mathf.Abs(normalSide) > 0.1f)
        {
            return Mathf.Sign(normalSide) * forwardAvoidanceSteerGain;
        }

        float desiredSign = Mathf.Abs(desiredForwardSteer) > 0.05f ? Mathf.Sign(desiredForwardSteer) : 1f;
        return -desiredSign * forwardAvoidanceSteerGain;
    }

    private void AddSideAvoidance(Vector3 origin, Vector3 direction, float steerAway, ref float steerTotal, ref float weightTotal)
    {
        if (!TryRaycastObstacle(origin, direction, sideAvoidanceRayLength, out RaycastHit hit))
        {
            return;
        }

        float proximity = 1f - Mathf.Clamp01(hit.distance / sideAvoidanceRayLength);
        float weight = proximity * sideAvoidanceSteerGain;
        steerTotal += steerAway * weight;
        weightTotal += weight;
    }

    private bool TryRaycastObstacle(Vector3 origin, Vector3 direction, float distance, out RaycastHit closestHit)
    {
        closestHit = default;

        if (distance <= 0f)
        {
            return false;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            obstacleHits,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        bool foundHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = obstacleHits[i];
            if (hit.collider == null || IsOwnCollider(hit.collider))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private bool IsOwnCollider(Collider hitCollider)
    {
        return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableObstacleAvoidance)
        {
            return;
        }

        Vector3 origin = transform.position + transform.up * avoidanceRayOriginHeight + transform.forward * avoidanceRayForwardOffset;
        Gizmos.color = new Color(1f, 0.6f, 0.1f);

        for (int i = 0; i < forwardAvoidanceAngles.Length; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(forwardAvoidanceAngles[i], transform.up) * transform.forward;
            Gizmos.DrawLine(origin, origin + direction * forwardAvoidanceRayLength);
        }

        Gizmos.color = new Color(0.2f, 0.7f, 1f);
        Gizmos.DrawLine(origin, origin - transform.right * sideAvoidanceRayLength);
        Gizmos.DrawLine(origin, origin + transform.right * sideAvoidanceRayLength);
    }

    private void SetBotInput(float throttle, float steer, bool nitros = false, bool handbrake = false)
    {
        currentThrottleInput = Mathf.Clamp(throttle, -1f, 1f);
        currentSteerInput = Mathf.Clamp(steer, -1f, 1f);
        currentNitrosInput = nitros;
        currentHandbrakeInput = handbrake;
        carController.SetExternalInput(currentThrottleInput, currentSteerInput, currentNitrosInput, currentHandbrakeInput);
    }
}
