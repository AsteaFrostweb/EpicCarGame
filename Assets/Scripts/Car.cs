using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{    
    public int value = 5000;
    public new string name = string.Empty;
    public static Dictionary<string, Car> allCars = new Dictionary<string, Car>();
    private const string CarPrefabResourcePath = "Prefabs/Cars";

    [Header("Race Progress")]
    [SerializeField] private RacingLineController racingLine;
    [SerializeField] private bool trackRaceProgress = true;

    public float splineDistanceCovered;
    public float currentSplineDistance;
    public float currentUnwrappedSplineDistance;
    public float furthestUnwrappedSplineDistance;
    public int currentSplineSampleIndex = -1;
    public Vector3 currentClosestSplinePoint;

    private float raceStartSplineDistance;
    private float previousSplineDistance;
    private bool hasPreviousSplineDistance;


    public Car(int value, string name, GameObject prefab)
    {
        this.value = value;
        this.name = name;      
    }

    //Spawns a car gameobject if it can find one the dictionary and returns a reference to the spawned car
    public static GameObject SpawnCar(string carName, Vector3 position, Quaternion rotation)
    {        
        Car car;
        if (!allCars.TryGetValue(carName, out car)) return null;

        return Instantiate(car.gameObject, position, rotation);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void LoadAllCars()
    {
        allCars.Clear();

        GameObject[] carPrefabs = Resources.LoadAll<GameObject>(CarPrefabResourcePath);

        foreach (GameObject carPrefab in carPrefabs)
        {
            Car car = carPrefab.GetComponent<Car>();

            if (car == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(car.name))
            {
                Debug.LogWarning($"Car prefab '{carPrefab.name}' has a Car component but no car name.");
                continue;
            }

            if (allCars.ContainsKey(car.name))
            {
                Debug.LogWarning($"Duplicate car name '{car.name}' found on prefab '{carPrefab.name}'. Keeping the first loaded car.");
                continue;
            }

            allCars.Add(car.name, car);
        }
    }

    private void Awake()
    {
        if (racingLine == null)
        {
            racingLine = FindFirstObjectByType<RacingLineController>();
        }
    }

    private void OnEnable()
    {
        ResetRaceProgress();
    }

    private void FixedUpdate()
    {
        UpdateRaceProgress();
    }

    public void ResetRaceProgress()
    {
        splineDistanceCovered = 0f;
        currentSplineDistance = 0f;
        currentUnwrappedSplineDistance = 0f;
        furthestUnwrappedSplineDistance = 0f;
        currentSplineSampleIndex = -1;
        currentClosestSplinePoint = transform.position;
        raceStartSplineDistance = 0f;
        previousSplineDistance = 0f;
        hasPreviousSplineDistance = false;

        if (!UpdateCurrentSplinePosition())
        {
            return;
        }

        raceStartSplineDistance = currentSplineDistance;
        previousSplineDistance = currentSplineDistance;
        currentUnwrappedSplineDistance = currentSplineDistance;
        furthestUnwrappedSplineDistance = currentSplineDistance;
        hasPreviousSplineDistance = true;
    }

    public void SetRacingLine(RacingLineController newRacingLine)
    {
        racingLine = newRacingLine;
        ResetRaceProgress();
    }

    private void UpdateRaceProgress()
    {
        if (!trackRaceProgress || !UpdateCurrentSplinePosition())
        {
            return;
        }

        if (!hasPreviousSplineDistance)
        {
            raceStartSplineDistance = currentSplineDistance;
            previousSplineDistance = currentSplineDistance;
            currentUnwrappedSplineDistance = currentSplineDistance;
            furthestUnwrappedSplineDistance = currentSplineDistance;
            hasPreviousSplineDistance = true;
            return;
        }

        float splineDelta = currentSplineDistance - previousSplineDistance;
        float splineLength = racingLine.TotalLength;

        if (racingLine.ClosedLoop && splineLength > 0.001f)
        {
            splineDelta = GetShortestWrappedSplineDelta(splineDelta, splineLength);
            currentUnwrappedSplineDistance += splineDelta;
        }
        else
        {
            currentUnwrappedSplineDistance = currentSplineDistance;
        }

        if (currentUnwrappedSplineDistance > furthestUnwrappedSplineDistance)
        {
            furthestUnwrappedSplineDistance = currentUnwrappedSplineDistance;
        }

        splineDistanceCovered = Mathf.Max(0f, furthestUnwrappedSplineDistance - raceStartSplineDistance);
        previousSplineDistance = currentSplineDistance;
    }

    private float GetShortestWrappedSplineDelta(float splineDelta, float splineLength)
    {
        float halfSplineLength = splineLength * 0.5f;

        if (splineDelta < -halfSplineLength)
        {
            return splineDelta + splineLength;
        }

        if (splineDelta > halfSplineLength)
        {
            return splineDelta - splineLength;
        }

        return splineDelta;
    }

    private bool UpdateCurrentSplinePosition()
    {
        if (racingLine == null)
        {
            racingLine = FindFirstObjectByType<RacingLineController>();
        }

        if (racingLine == null || racingLine.SampleCount < 2)
        {
            return false;
        }

        return racingLine.TryGetClosestDistanceAlongSpline(
            transform.position,
            out currentSplineDistance,
            out currentSplineSampleIndex,
            out currentClosestSplinePoint);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
