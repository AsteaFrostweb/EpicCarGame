using UnityEngine;

[RequireComponent(typeof(SimpleWheelCarController))]
[RequireComponent(typeof(PlayerRaceController))]
public class BotCarController : MonoBehaviour
{
    [SerializeField] private SimpleWheelCarController carController;
    [SerializeField] private PlayerRaceController raceController;

    [Header("Path Following")]
    [SerializeField] private float lookAheadDistance = 12f;
    [SerializeField] private bool useDistanceCheckpointCompletion;
    [SerializeField] private float checkpointReachDistance = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float forwardAlignmentWeight = 0.65f;

    [Header("Driving")]
    [SerializeField] private float maxThrottle = 1f;
    [SerializeField] private float cautiousThrottle = 0.45f;
    [SerializeField] private float steeringAngleForCaution = 45f;
    [SerializeField] private float brakeAngle = 80f;

    public float currentSteerInput;
    public float currentThrottleInput;
    public float segmentProgress;

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
    }

    private void FixedUpdate()
    {
        if (carController == null || raceController == null)
        {
            return;
        }

        Transform previousCheckpoint = raceController.PreviousCheckpointTransform;
        Transform nextCheckpoint = raceController.NextCheckpointTransform;
        if (previousCheckpoint == null || nextCheckpoint == null)
        {
            carController.SetExternalInput(0f, 0f);
            return;
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

        Vector3 aimPoint = nextPosition + targetDirection * lookAheadDistance;
        Vector3 directionToAimPoint = aimPoint - transform.position;
        directionToAimPoint.y = 0f;

        float signedAngle = Vector3.SignedAngle(transform.forward, directionToAimPoint.normalized, Vector3.up);
        currentSteerInput = Mathf.Clamp(signedAngle / 45f, -1f, 1f);

        float steeringSeverity = Mathf.Abs(signedAngle);
        currentThrottleInput = Mathf.Lerp(maxThrottle, cautiousThrottle, Mathf.InverseLerp(0f, steeringAngleForCaution, steeringSeverity));

        if (steeringSeverity >= brakeAngle)
        {
            currentThrottleInput = -0.25f;
        }

        carController.SetExternalInput(currentThrottleInput, currentSteerInput);

        if (useDistanceCheckpointCompletion && checkpointDirection.magnitude <= checkpointReachDistance)
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
}
