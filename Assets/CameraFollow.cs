using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [System.Serializable]
    private struct CameraStyleTuning
    {
        [Header("Position")]
        public float distance;
        public float height;
        public float movementSpeed;
        public float minMovementSpeed;
        public float positionSlowdownDistance;

        [Header("Rotation")]
        public float lookHeight;
        public float rotationSpeed;
        public float minRotationSpeed;
        public float rotationSlowdownAngle;

        [Header("Drift Framing")]
        public float velocityBlendSpeed;
        public float minVelocityForDirection;
        [Range(0f, 1f)] public float velocityDirectionBias;
        public float extraDistanceAtMaxDrift;
        public float driftPullInDistance;
        public float driftPullInHeight;

        [Header("Speed Framing")]
        public float speedForMaxOffset;
        public float extraDistanceAtMaxSpeed;
        public float extraHeightAtMaxSpeed;

        [Header("Nitros Shake")]
        public float nitrosShakePosition;
        public float nitrosShakeRotation;
        public float nitrosShakeFrequency;
        public float nitrosShakeFadeSpeed;

        [Header("Angle-Based Side Correction")]
        public float sideCorrectionStrength;
        public float sideCorrectionSpeed;
        public float maxSideOffset;
        public bool invertSideCorrection;

        public void Clamp()
        {
            distance = Mathf.Max(0.1f, distance);
            height = Mathf.Max(0f, height);
            movementSpeed = Mathf.Max(0f, movementSpeed);
            minMovementSpeed = Mathf.Max(0f, minMovementSpeed);
            positionSlowdownDistance = Mathf.Max(0.01f, positionSlowdownDistance);
            lookHeight = Mathf.Max(0f, lookHeight);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            minRotationSpeed = Mathf.Max(0f, minRotationSpeed);
            rotationSlowdownAngle = Mathf.Max(0.01f, rotationSlowdownAngle);
            velocityBlendSpeed = Mathf.Max(0f, velocityBlendSpeed);
            minVelocityForDirection = Mathf.Max(0f, minVelocityForDirection);
            velocityDirectionBias = Mathf.Clamp01(velocityDirectionBias);
            extraDistanceAtMaxDrift = Mathf.Max(0f, extraDistanceAtMaxDrift);
            driftPullInDistance = Mathf.Max(0f, driftPullInDistance);
            driftPullInHeight = Mathf.Max(0f, driftPullInHeight);
            speedForMaxOffset = Mathf.Max(0.01f, speedForMaxOffset);
            extraDistanceAtMaxSpeed = Mathf.Max(0f, extraDistanceAtMaxSpeed);
            extraHeightAtMaxSpeed = Mathf.Max(0f, extraHeightAtMaxSpeed);
            nitrosShakePosition = Mathf.Max(0f, nitrosShakePosition);
            nitrosShakeRotation = Mathf.Max(0f, nitrosShakeRotation);
            nitrosShakeFrequency = Mathf.Max(0f, nitrosShakeFrequency);
            nitrosShakeFadeSpeed = Mathf.Max(0f, nitrosShakeFadeSpeed);
            sideCorrectionStrength = Mathf.Max(0f, sideCorrectionStrength);
            sideCorrectionSpeed = Mathf.Max(0f, sideCorrectionSpeed);
            maxSideOffset = Mathf.Max(0f, maxSideOffset);
        }
    }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerSettings playerSettings;

    [Header("Style Selection")]
    [SerializeField] private CameraStyle defaultCameraStyle = CameraStyle.Responsive;

    [Header("Responsive Style")]
    [SerializeField] private CameraStyleTuning responsiveTuning = new CameraStyleTuning
    {
        distance = 8f,
        height = 3f,
        movementSpeed = 5f,
        minMovementSpeed = 1.5f,
        positionSlowdownDistance = 4f,
        lookHeight = 1.5f,
        rotationSpeed = 5f,
        minRotationSpeed = 1.25f,
        rotationSlowdownAngle = 35f,
        velocityBlendSpeed = 4f,
        minVelocityForDirection = 2f,
        velocityDirectionBias = 0.75f,
        extraDistanceAtMaxDrift = 2.5f,
        driftPullInDistance = 1.4f,
        driftPullInHeight = 0.35f,
        speedForMaxOffset = 60f,
        extraDistanceAtMaxSpeed = 4f,
        extraHeightAtMaxSpeed = 1.8f,
        nitrosShakePosition = 0.12f,
        nitrosShakeRotation = 0.65f,
        nitrosShakeFrequency = 32f,
        nitrosShakeFadeSpeed = 10f,
        sideCorrectionStrength = 1f,
        sideCorrectionSpeed = 5f,
        maxSideOffset = 5f,
        invertSideCorrection = false
    };

    [Header("Cinematic Drift Style")]
    [SerializeField] private CameraStyleTuning cinematicDriftTuning = new CameraStyleTuning
    {
        distance = 10.5f,
        height = 3.4f,
        movementSpeed = 3.2f,
        minMovementSpeed = 0.75f,
        positionSlowdownDistance = 6.5f,
        lookHeight = 1.6f,
        rotationSpeed = 2.25f,
        minRotationSpeed = 0.55f,
        rotationSlowdownAngle = 55f,
        velocityBlendSpeed = 2.2f,
        minVelocityForDirection = 1.5f,
        velocityDirectionBias = 0.95f,
        extraDistanceAtMaxDrift = 4.5f,
        driftPullInDistance = 1.8f,
        driftPullInHeight = 0.45f,
        speedForMaxOffset = 60f,
        extraDistanceAtMaxSpeed = 6f,
        extraHeightAtMaxSpeed = 2.4f,
        nitrosShakePosition = 0.16f,
        nitrosShakeRotation = 0.85f,
        nitrosShakeFrequency = 30f,
        nitrosShakeFadeSpeed = 10f,
        sideCorrectionStrength = 1.2f,
        sideCorrectionSpeed = 2.75f,
        maxSideOffset = 7.5f,
        invertSideCorrection = false
    };

    [Header("Update Mode")]
    [SerializeField] private bool useFixedUpdate = false;

    [Header("Ground Clearance")]
    [SerializeField] private bool useGroundClearance = true;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundRaycastHeight = 25f;
    [SerializeField] private float groundRaycastDistance = 60f;
    [SerializeField] private float minGroundClearance = 1.2f;

    private float currentSideOffset;
    private float nitrosShakeAmount;
    private Rigidbody targetRigidbody;
    private SimpleWheelCarController targetCarController;
    private Vector3 smoothedPlanarFollowDirection;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        responsiveTuning.Clamp();
        cinematicDriftTuning.Clamp();
        groundRaycastHeight = Mathf.Max(0.1f, groundRaycastHeight);
        groundRaycastDistance = Mathf.Max(0.1f, groundRaycastDistance);
        minGroundClearance = Mathf.Max(0f, minGroundClearance);
    }

    private void FixedUpdate()
    {
        if (!useFixedUpdate) return;
        Follow(Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (useFixedUpdate) return;
        Follow(Time.deltaTime);
    }

    private void Follow(float deltaTime)
    {
        if (target == null) return;

        CacheReferences();
        CameraStyleTuning tuning = GetActiveTuning();
        UpdateFollowDirection(deltaTime, tuning);

        float desiredSideOffset = GetRequiredSideOffset(tuning);

        if (tuning.invertSideCorrection)
        {
            desiredSideOffset *= -1f;
        }

        desiredSideOffset *= tuning.sideCorrectionStrength;
        desiredSideOffset = Mathf.Clamp(desiredSideOffset, -tuning.maxSideOffset, tuning.maxSideOffset);

        float sideT = 1f - Mathf.Exp(-tuning.sideCorrectionSpeed * deltaTime);
        currentSideOffset = Mathf.Lerp(currentSideOffset, desiredSideOffset, sideT);
        UpdateNitrosShake(deltaTime, tuning);

        Vector3 desiredPosition = GetTargetPosition(tuning);
        Quaternion desiredRotation = GetTargetRotation(tuning);

        float distanceToDesiredPosition = Vector3.Distance(transform.position, desiredPosition);
        float positionLerp = Mathf.InverseLerp(0f, tuning.positionSlowdownDistance, distanceToDesiredPosition);
        float adaptiveMovementSpeed = Mathf.Lerp(tuning.minMovementSpeed, tuning.movementSpeed, positionLerp);
        float movementT = 1f - Mathf.Exp(-adaptiveMovementSpeed * deltaTime);

        float angleToDesiredRotation = Quaternion.Angle(transform.rotation, desiredRotation);
        float rotationLerp = Mathf.InverseLerp(0f, tuning.rotationSlowdownAngle, angleToDesiredRotation);
        float adaptiveRotationSpeed = Mathf.Lerp(tuning.minRotationSpeed, tuning.rotationSpeed, rotationLerp);
        float rotationT = 1f - Mathf.Exp(-adaptiveRotationSpeed * deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, movementT);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        ApplyNitrosShake(tuning);
    }

    private void CacheReferences()
    {
        if (target == null)
        {
            targetRigidbody = null;
            targetCarController = null;
            playerSettings = null;
            return;
        }

        if (targetRigidbody == null || targetRigidbody.transform != target)
        {
            targetRigidbody = target.GetComponent<Rigidbody>();
        }

        if (targetCarController == null || targetCarController.transform != target)
        {
            targetCarController = target.GetComponent<SimpleWheelCarController>();
        }

        if (playerSettings == null)
        {
            playerSettings = target.GetComponent<PlayerSettings>();
        }
    }

    private CameraStyle GetActiveCameraStyle()
    {
        return playerSettings != null ? playerSettings.CameraStyle : defaultCameraStyle;
    }

    private CameraStyleTuning GetActiveTuning()
    {
        switch (GetActiveCameraStyle())
        {
            case CameraStyle.CinematicDrift:
                return cinematicDriftTuning;
            default:
                return responsiveTuning;
        }
    }

    private Vector3 GetTargetPosition(CameraStyleTuning tuning)
    {
        Vector3 followDirection = GetFollowDirection();
        float driftAngle = GetDriftAngleDegrees(followDirection);
        float driftDistanceOffset = Mathf.Lerp(
            0f,
            tuning.extraDistanceAtMaxDrift,
            Mathf.InverseLerp(0f, 90f, driftAngle)
        );
        float speedOffsetT = GetSpeedOffsetT(tuning);
        float driftT = GetDriftT();

        Vector3 basePosition =
            target.position
            - followDirection * Mathf.Max(
                0.1f,
                tuning.distance + driftDistanceOffset + tuning.extraDistanceAtMaxSpeed * speedOffsetT - tuning.driftPullInDistance * driftT)
            + Vector3.up * Mathf.Max(
                0f,
                tuning.height + tuning.extraHeightAtMaxSpeed * speedOffsetT - tuning.driftPullInHeight * driftT);

        Vector3 sideCorrection = transform.right * currentSideOffset;
        return ApplyGroundClearance(basePosition + sideCorrection);
    }

    private void UpdateNitrosShake(float deltaTime, CameraStyleTuning tuning)
    {
        float targetShake = IsNitrosActive() ? 1f : 0f;
        float shakeT = 1f - Mathf.Exp(-tuning.nitrosShakeFadeSpeed * deltaTime);
        nitrosShakeAmount = Mathf.Lerp(nitrosShakeAmount, targetShake, shakeT);
    }

    private void ApplyNitrosShake(CameraStyleTuning tuning)
    {
        if (nitrosShakeAmount <= 0.001f || tuning.nitrosShakeFrequency <= 0f)
        {
            return;
        }

        float shakeTime = Time.time * tuning.nitrosShakeFrequency;
        float horizontalNoise = Mathf.PerlinNoise(shakeTime, 0.21f) * 2f - 1f;
        float verticalNoise = Mathf.PerlinNoise(0.47f, shakeTime) * 2f - 1f;
        float rollNoise = Mathf.PerlinNoise(shakeTime, shakeTime + 1.31f) * 2f - 1f;

        Vector3 positionOffset =
            transform.right * horizontalNoise * tuning.nitrosShakePosition * nitrosShakeAmount
            + transform.up * verticalNoise * tuning.nitrosShakePosition * nitrosShakeAmount;

        transform.position += positionOffset;
        transform.rotation *= Quaternion.Euler(0f, 0f, rollNoise * tuning.nitrosShakeRotation * nitrosShakeAmount);
    }

    private float GetDriftT()
    {
        return targetCarController != null ? targetCarController.Drift01 : 0f;
    }

    private bool IsNitrosActive()
    {
        return targetCarController != null && targetCarController.IsNitrosActive;
    }

    private float GetSpeedOffsetT(CameraStyleTuning tuning)
    {
        if (targetRigidbody == null)
        {
            return 0f;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(targetRigidbody.linearVelocity, Vector3.up);
        return Mathf.Clamp01(planarVelocity.magnitude / tuning.speedForMaxOffset);
    }

    private Vector3 ApplyGroundClearance(Vector3 desiredPosition)
    {
        if (!useGroundClearance)
        {
            return desiredPosition;
        }

        Vector3 rayStart = desiredPosition + Vector3.up * groundRaycastHeight;
        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return desiredPosition;
        }

        float minY = hit.point.y + minGroundClearance;
        if (desiredPosition.y < minY)
        {
            desiredPosition.y = minY;
        }

        return desiredPosition;
    }

    private Quaternion GetTargetRotation(CameraStyleTuning tuning)
    {
        Vector3 lookTarget = target.position + Vector3.up * tuning.lookHeight;
        Vector3 directionToTarget = lookTarget - transform.position;

        if (directionToTarget.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(directionToTarget, Vector3.up);
    }

    private void UpdateFollowDirection(float deltaTime, CameraStyleTuning tuning)
    {
        Vector3 desiredDirection = GetDesiredPlanarFollowDirection(tuning);

        if (smoothedPlanarFollowDirection.sqrMagnitude < 0.001f)
        {
            smoothedPlanarFollowDirection = desiredDirection;
            return;
        }

        float blendT = 1f - Mathf.Exp(-tuning.velocityBlendSpeed * deltaTime);
        Vector3 blendedDirection = Vector3.Slerp(smoothedPlanarFollowDirection, desiredDirection, blendT);
        smoothedPlanarFollowDirection = Vector3.ProjectOnPlane(blendedDirection, Vector3.up).normalized;
    }

    private Vector3 GetDesiredPlanarFollowDirection(CameraStyleTuning tuning)
    {
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;

        if (targetRigidbody == null)
        {
            return forward;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(targetRigidbody.linearVelocity, Vector3.up);

        if (planarVelocity.magnitude < tuning.minVelocityForDirection)
        {
            return forward;
        }

        Vector3 velocityDirection = planarVelocity.normalized;
        Vector3 desiredDirection = Vector3.Slerp(forward, velocityDirection, tuning.velocityDirectionBias);
        return Vector3.ProjectOnPlane(desiredDirection, Vector3.up).normalized;
    }

    private Vector3 GetFollowDirection()
    {
        if (smoothedPlanarFollowDirection.sqrMagnitude >= 0.001f)
        {
            return smoothedPlanarFollowDirection;
        }

        Vector3 planarForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        if (planarForward.sqrMagnitude < 0.001f)
        {
            return Vector3.forward;
        }

        return planarForward.normalized;
    }

    private float GetDriftAngleDegrees(Vector3 followDirection)
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;

        if (planarForward.sqrMagnitude < 0.001f || followDirection.sqrMagnitude < 0.001f)
        {
            return 0f;
        }

        return Mathf.Abs(Vector3.SignedAngle(planarForward, followDirection, Vector3.up));
    }

    private float GetRequiredSideOffset(CameraStyleTuning tuning)
    {
        Vector3 lookTarget = target.position + Vector3.up * tuning.lookHeight;
        Vector3 toTarget = lookTarget - transform.position;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget < 0.001f)
        {
            return 0f;
        }

        Vector3 flatCameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;

        if (flatCameraForward.sqrMagnitude < 0.001f || flatToTarget.sqrMagnitude < 0.001f)
        {
            return 0f;
        }

        float angleErrorDegrees = Vector3.SignedAngle(flatCameraForward, flatToTarget, Vector3.up);
        float angleErrorRadians = angleErrorDegrees * Mathf.Deg2Rad;
        return Mathf.Tan(angleErrorRadians) * distanceToTarget;
    }
}
