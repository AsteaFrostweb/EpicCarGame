using System;
using System.Collections.Generic;
using UnityEngine;

public class TrackController : MonoBehaviour
{
    [Serializable]
    public struct TrackControllerData 
    {       
        public List<string> carNames;
        public int laps;       
    }

    public TrackControllerData trackData;

    private List<GameObject> spawnedCars = new List<GameObject>();
    public List<Car> raceStandings = new List<Car>();

    [SerializeField]
    private CarSpawnController carSpawnController;
    [SerializeField]
    private RacingLineController racingLineController;
    private bool initialized = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        //initialization
       
        if (carSpawnController == null)
        {
            initialized = false;
            Debug.LogWarning("TrackController needs a CarSpawnController");
        }

        if (racingLineController == null)
        {
            racingLineController = FindFirstObjectByType<RacingLineController>();
        }

        //Verify initialization
        if (!initialized) return;

        //If the track doesnt contain the players car in its default car list
        if (!trackData.carNames.Contains(GameData.playerCarName)) 
        {
            //if the cars to spawn is equal to the number of spawn points we need to remove a car to make spawn for the players car
            if (trackData.carNames.Count >= carSpawnController.carSpawnPoints.Count)            
                trackData.carNames.RemoveAt(0);
                
           //Add the players car to the spawn list
            trackData.carNames.Add(GameData.playerCarName);
        }

        SpawnCarsForRace();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized) return;

        UpdateRaceStandings();
    }

    private void SpawnCarsForRace() 
    {
        int carCount = trackData.carNames.Count;
        int index = 0;
        foreach (string name in trackData.carNames) 
        {
            Transform spawnPosition = carSpawnController.GetSpawnPoint(carCount);
            if (spawnPosition == null)
            {
                Debug.LogWarning($"Unable to spawn car:{name} as carSpawnController returned no spawn point");
                continue;
            }

            GameObject carObj = Car.SpawnCar(name, spawnPosition.position, spawnPosition.rotation);
            if (carObj == null)
            {
                Debug.LogWarning($"Unable to spawn car:{name} as it was not found in Car.allCars");
                continue;
            }

            Car car = carObj.GetComponent<Car>();
            if (car != null && racingLineController != null)
            {
                car.SetRacingLine(racingLineController);
                raceStandings.Add(car);
            }

            if (name == GameData.playerCarName)
                InitPlayerCar(carObj);

                
            spawnedCars.Add(carObj);

           index++;
        }

        UpdateRaceStandings();
    }

    private void UpdateRaceStandings()
    {
        raceStandings.RemoveAll(car => car == null);
        raceStandings.Sort((left, right) => right.splineDistanceCovered.CompareTo(left.splineDistanceCovered));
    }

    private void InitPlayerCar(GameObject carObj) 
    {
        carObj.GetComponent<BotCarController>().enabled = false;
        GameObject.Find("Main Camera").GetComponent<CameraFollow>().target = carObj.transform;        
    }
}
