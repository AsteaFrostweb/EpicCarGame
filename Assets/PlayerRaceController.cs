using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{
    [SerializeField] private CheckpointController checkpointController;
    [SerializeField] private bool startAtCheckpointOne = true;

    public int nextCheckpointListIndex;
    public int lastCheckpointListIndex;
    public int completedCheckpoints;
    public int completedLaps;

    public CheckpointController CheckpointController => checkpointController;
    public Transform PreviousCheckpointTransform => GetCheckpointTransform(lastCheckpointListIndex);
    public Transform NextCheckpointTransform => GetCheckpointTransform(nextCheckpointListIndex);
    public int NextCheckpointNumber => GetCheckpointNumber(nextCheckpointListIndex);

    private void Awake()
    {
        if (checkpointController == null)
        {
            checkpointController = FindFirstObjectByType<CheckpointController>();
        }

        ResetRaceProgress();
    }

    public void ResetRaceProgress()
    {
        completedCheckpoints = 0;
        completedLaps = 0;
        lastCheckpointListIndex = 0;
        nextCheckpointListIndex = startAtCheckpointOne && CheckpointCount > 1 ? 1 : 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (checkpointController == null || nextCheckpointListIndex < 0 || nextCheckpointListIndex >= CheckpointCount)
        {
            return;
        }

        BoxCollider expectedCollider = checkpointController.checkpoints[nextCheckpointListIndex].collider;
        if (other != expectedCollider)
        {
            return;
        }

        CompleteNextCheckpoint();
    }

    public void CompleteNextCheckpoint()
    {
        if (CheckpointCount == 0)
        {
            return;
        }

        lastCheckpointListIndex = nextCheckpointListIndex;
        completedCheckpoints++;

        if (checkpointController.checkpoints[lastCheckpointListIndex].index == 0 && completedCheckpoints > 1)
        {
            completedLaps++;
        }

        nextCheckpointListIndex++;
        if (nextCheckpointListIndex >= CheckpointCount)
        {
            nextCheckpointListIndex = 0;
        }
    }

    private Transform GetCheckpointTransform(int listIndex)
    {
        if (checkpointController == null || listIndex < 0 || listIndex >= CheckpointCount)
        {
            return null;
        }

        return checkpointController.checkpoints[listIndex].checkpointTransform;
    }

    private int GetCheckpointNumber(int listIndex)
    {
        if (checkpointController == null || listIndex < 0 || listIndex >= CheckpointCount)
        {
            return -1;
        }

        return checkpointController.checkpoints[listIndex].index;
    }

    private int CheckpointCount => checkpointController != null && checkpointController.checkpoints != null
        ? checkpointController.checkpoints.Count
        : 0;
}
