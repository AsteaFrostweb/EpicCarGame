using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RacingLineController : MonoBehaviour
{
    [SerializeField] private bool closedLoop = true;
    [SerializeField] private int samplesPerSegment = 12;
    [SerializeField] private float gizmoPointRadius = 1.25f;
    [SerializeField] private Color controlPointColor = new Color(1f, 0.86f, 0.1f);
    [SerializeField] private Color slowLineColor = new Color(1f, 0.1f, 0.05f);
    [SerializeField] private Color fastLineColor = new Color(0.1f, 0.9f, 0.15f);
    [SerializeField] private float defaultDesiredSpeed = 35f;
    [SerializeField] private float slowColorSpeed = 10f;
    [SerializeField] private float fastColorSpeed = 60f;
    [SerializeField] private List<Vector3> localControlPoints = new List<Vector3>();
    [SerializeField] private List<float> desiredSpeeds = new List<float>();

    private readonly List<Vector3> sampledWorldPoints = new List<Vector3>();
    private readonly List<int> sampledSegmentIndices = new List<int>();
    private readonly List<float> sampledDesiredSpeeds = new List<float>();

    public bool ClosedLoop
    {
        get => closedLoop;
        set
        {
            closedLoop = value;
            RebuildSamples();
        }
    }

    public int ControlPointCount => localControlPoints.Count;
    public int SampleCount => sampledWorldPoints.Count;
    public IReadOnlyList<Vector3> SampledWorldPoints => sampledWorldPoints;
    public IReadOnlyList<float> SampledDesiredSpeeds => sampledDesiredSpeeds;

    private void OnEnable()
    {
        RebuildSamples();
    }

    private void OnValidate()
    {
        samplesPerSegment = Mathf.Max(2, samplesPerSegment);
        gizmoPointRadius = Mathf.Max(0.05f, gizmoPointRadius);
        defaultDesiredSpeed = Mathf.Max(0f, defaultDesiredSpeed);
        slowColorSpeed = Mathf.Max(0f, slowColorSpeed);
        fastColorSpeed = Mathf.Max(slowColorSpeed + 0.01f, fastColorSpeed);
        EnsureDesiredSpeedCount();
        RebuildSamples();
    }

    private void OnTransformChildrenChanged()
    {
        RebuildSamples();
    }

    public Vector3 GetControlPointWorld(int index)
    {
        return transform.TransformPoint(localControlPoints[index]);
    }

    public void SetControlPointWorld(int index, Vector3 worldPosition)
    {
        localControlPoints[index] = transform.InverseTransformPoint(worldPosition);
        RebuildSamples();
    }

    public float GetDesiredSpeed(int index)
    {
        EnsureDesiredSpeedCount();
        return desiredSpeeds[index];
    }

    public void SetDesiredSpeed(int index, float desiredSpeed)
    {
        EnsureDesiredSpeedCount();
        desiredSpeeds[index] = Mathf.Max(0f, desiredSpeed);
        RebuildSamples();
    }

    public void AppendPoint(Vector3 worldPosition)
    {
        localControlPoints.Add(transform.InverseTransformPoint(worldPosition));
        desiredSpeeds.Add(defaultDesiredSpeed);
        RebuildSamples();
    }

    public void InsertPointNearCurve(Vector3 worldPosition)
    {
        if (localControlPoints.Count < 2 || sampledWorldPoints.Count < 2)
        {
            AppendPoint(worldPosition);
            return;
        }

        int sampleIndex = GetClosestSampleIndex(worldPosition);
        int segmentIndex = sampledSegmentIndices[Mathf.Clamp(sampleIndex, 0, sampledSegmentIndices.Count - 1)];
        int insertIndex = Mathf.Clamp(segmentIndex + 1, 0, localControlPoints.Count);

        if (closedLoop && segmentIndex == localControlPoints.Count - 1)
        {
            insertIndex = localControlPoints.Count;
        }

        float insertedDesiredSpeed = GetDesiredSpeedForSegment(segmentIndex);
        localControlPoints.Insert(insertIndex, transform.InverseTransformPoint(worldPosition));
        desiredSpeeds.Insert(insertIndex, insertedDesiredSpeed);
        RebuildSamples();
    }

    public void RemovePoint(int index)
    {
        if (index < 0 || index >= localControlPoints.Count)
        {
            return;
        }

        localControlPoints.RemoveAt(index);
        if (index < desiredSpeeds.Count)
        {
            desiredSpeeds.RemoveAt(index);
        }

        RebuildSamples();
    }

    public void ClearPoints()
    {
        localControlPoints.Clear();
        desiredSpeeds.Clear();
        RebuildSamples();
    }

    public int GetClosestSampleIndex(Vector3 worldPosition)
    {
        int closestIndex = -1;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < sampledWorldPoints.Count; i++)
        {
            float distance = (sampledWorldPoints[i] - worldPosition).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public Vector3 GetSampleWorldPoint(int sampleIndex)
    {
        if (sampledWorldPoints.Count == 0)
        {
            return transform.position;
        }

        if (closedLoop)
        {
            sampleIndex = WrapIndex(sampleIndex, sampledWorldPoints.Count);
        }
        else
        {
            sampleIndex = Mathf.Clamp(sampleIndex, 0, sampledWorldPoints.Count - 1);
        }

        return sampledWorldPoints[sampleIndex];
    }

    public float GetDesiredSpeedAtSample(int sampleIndex)
    {
        if (sampledDesiredSpeeds.Count == 0)
        {
            return defaultDesiredSpeed;
        }

        if (closedLoop)
        {
            sampleIndex = WrapIndex(sampleIndex, sampledDesiredSpeeds.Count);
        }
        else
        {
            sampleIndex = Mathf.Clamp(sampleIndex, 0, sampledDesiredSpeeds.Count - 1);
        }

        return sampledDesiredSpeeds[sampleIndex];
    }

    public Vector3 GetPointAheadByDistance(int startSampleIndex, float distance)
    {
        if (sampledWorldPoints.Count == 0)
        {
            return transform.position;
        }

        float remainingDistance = Mathf.Max(0f, distance);
        int currentIndex = closedLoop
            ? WrapIndex(startSampleIndex, sampledWorldPoints.Count)
            : Mathf.Clamp(startSampleIndex, 0, sampledWorldPoints.Count - 1);

        for (int step = 0; step < sampledWorldPoints.Count; step++)
        {
            int nextIndex = currentIndex + 1;
            if (nextIndex >= sampledWorldPoints.Count)
            {
                if (!closedLoop)
                {
                    return sampledWorldPoints[currentIndex];
                }

                nextIndex = 0;
            }

            float segmentDistance = Vector3.Distance(sampledWorldPoints[currentIndex], sampledWorldPoints[nextIndex]);
            if (segmentDistance >= remainingDistance)
            {
                float t = segmentDistance > 0.001f ? remainingDistance / segmentDistance : 0f;
                return Vector3.Lerp(sampledWorldPoints[currentIndex], sampledWorldPoints[nextIndex], t);
            }

            remainingDistance -= segmentDistance;
            currentIndex = nextIndex;
        }

        return sampledWorldPoints[currentIndex];
    }

    public Vector3 GetSuggestedAppendWorldPosition()
    {
        if (localControlPoints.Count == 0)
        {
            return transform.position;
        }

        Vector3 lastPoint = GetControlPointWorld(localControlPoints.Count - 1);
        if (localControlPoints.Count == 1)
        {
            return lastPoint + transform.forward * 10f;
        }

        Vector3 previousPoint = GetControlPointWorld(localControlPoints.Count - 2);
        Vector3 direction = lastPoint - previousPoint;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        return lastPoint + direction.normalized * 10f;
    }

    public void RebuildSamples()
    {
        EnsureDesiredSpeedCount();
        sampledWorldPoints.Clear();
        sampledSegmentIndices.Clear();
        sampledDesiredSpeeds.Clear();

        int pointCount = localControlPoints.Count;
        if (pointCount == 0)
        {
            return;
        }

        if (pointCount < 4)
        {
            for (int i = 0; i < pointCount; i++)
            {
                sampledWorldPoints.Add(GetControlPointWorld(i));
                sampledSegmentIndices.Add(i);
                sampledDesiredSpeeds.Add(desiredSpeeds[i]);
            }

            return;
        }

        int segmentCount = closedLoop ? pointCount : pointCount - 1;
        for (int segment = 0; segment < segmentCount; segment++)
        {
            int p0 = closedLoop ? WrapIndex(segment - 1, pointCount) : Mathf.Max(segment - 1, 0);
            int p1 = segment;
            int p2 = closedLoop ? WrapIndex(segment + 1, pointCount) : Mathf.Min(segment + 1, pointCount - 1);
            int p3 = closedLoop ? WrapIndex(segment + 2, pointCount) : Mathf.Min(segment + 2, pointCount - 1);

            for (int sample = 0; sample < samplesPerSegment; sample++)
            {
                float t = sample / (float)samplesPerSegment;
                sampledWorldPoints.Add(CatmullRom(
                    GetControlPointWorld(p0),
                    GetControlPointWorld(p1),
                    GetControlPointWorld(p2),
                    GetControlPointWorld(p3),
                    t));
                sampledSegmentIndices.Add(segment);
                sampledDesiredSpeeds.Add(Mathf.Lerp(desiredSpeeds[p1], desiredSpeeds[p2], t));
            }
        }

        if (!closedLoop)
        {
            sampledWorldPoints.Add(GetControlPointWorld(pointCount - 1));
            sampledSegmentIndices.Add(pointCount - 2);
            sampledDesiredSpeeds.Add(desiredSpeeds[pointCount - 1]);
        }
    }

    private void OnDrawGizmos()
    {
        RebuildSamples();
        DrawSampledLine();
        DrawControlPoints();
    }

    private void DrawSampledLine()
    {
        if (sampledWorldPoints.Count < 2)
        {
            return;
        }

        for (int i = 0; i < sampledWorldPoints.Count - 1; i++)
        {
            Gizmos.color = GetLineColorForSample(i);
            Gizmos.DrawLine(sampledWorldPoints[i], sampledWorldPoints[i + 1]);
        }

        if (closedLoop && sampledWorldPoints.Count > 2)
        {
            Gizmos.color = GetLineColorForSample(sampledWorldPoints.Count - 1);
            Gizmos.DrawLine(sampledWorldPoints[sampledWorldPoints.Count - 1], sampledWorldPoints[0]);
        }
    }

    private void DrawControlPoints()
    {
        Gizmos.color = controlPointColor;
        for (int i = 0; i < localControlPoints.Count; i++)
        {
            Gizmos.DrawSphere(GetControlPointWorld(i), gizmoPointRadius);
        }
    }

    private void EnsureDesiredSpeedCount()
    {
        while (desiredSpeeds.Count < localControlPoints.Count)
        {
            desiredSpeeds.Add(defaultDesiredSpeed);
        }

        while (desiredSpeeds.Count > localControlPoints.Count)
        {
            desiredSpeeds.RemoveAt(desiredSpeeds.Count - 1);
        }

        for (int i = 0; i < desiredSpeeds.Count; i++)
        {
            desiredSpeeds[i] = Mathf.Max(0f, desiredSpeeds[i]);
        }
    }

    private float GetDesiredSpeedForSegment(int segmentIndex)
    {
        EnsureDesiredSpeedCount();

        if (desiredSpeeds.Count == 0)
        {
            return defaultDesiredSpeed;
        }

        int nextIndex = closedLoop
            ? WrapIndex(segmentIndex + 1, desiredSpeeds.Count)
            : Mathf.Clamp(segmentIndex + 1, 0, desiredSpeeds.Count - 1);

        segmentIndex = Mathf.Clamp(segmentIndex, 0, desiredSpeeds.Count - 1);
        return (desiredSpeeds[segmentIndex] + desiredSpeeds[nextIndex]) * 0.5f;
    }

    private Color GetLineColorForSample(int sampleIndex)
    {
        float desiredSpeed = sampleIndex >= 0 && sampleIndex < sampledDesiredSpeeds.Count
            ? sampledDesiredSpeeds[sampleIndex]
            : defaultDesiredSpeed;

        float t = Mathf.InverseLerp(slowColorSpeed, fastColorSpeed, desiredSpeed);
        return Color.Lerp(slowLineColor, fastLineColor, t);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static int WrapIndex(int index, int count)
    {
        return (index % count + count) % count;
    }
}
