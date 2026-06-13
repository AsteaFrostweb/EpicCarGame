using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GarageUIMasterController : MonoBehaviour
{
    [Serializable]
    public struct CarLoadEntity 
    {
        public Car car;
        public GameObject spawnInstance;
    }
    public List<CarLoadEntity> sucessfullyLoadedCars = new List<CarLoadEntity>();

    public GameObject CarSpawnPosition;

    public CarLoadEntity currentLoadedCar;
    private int currentLoadedIndex = 0;
    private int totalLoadedCars;

    private bool initialized = true;

    private void Start()
    {
        sucessfullyLoadedCars.Clear();

        foreach (Car car in Car.allCars.Values.OrderBy(car => car.name)) 
        {
            CarLoadEntity cle = new CarLoadEntity
            {
                car = car,
                spawnInstance = null
            };

            sucessfullyLoadedCars.Add(cle);
        }
        totalLoadedCars = sucessfullyLoadedCars.Count;

        //if the total loaded cars is 0 then set the UI to be un-initialized
        if (totalLoadedCars == 0) initialized = false;
        if (!initialized) return;

        SpawnCar();
    }

    private void SpawnCar() 
    {
        LoadCar(currentLoadedIndex);
        currentLoadedCar.spawnInstance = GameObject.Instantiate(currentLoadedCar.car.gameObject, CarSpawnPosition.transform);
    }
    private void LoadCar(int index) 
    {
        if (currentLoadedCar.spawnInstance != null)
            Destroy(currentLoadedCar.spawnInstance);

        if (index > totalLoadedCars - 1 || index < 0) 
        {
            Debug.LogWarning("attempting to load car outside the boudn of the sucesfully loaded cars list");
            return;
        }        

        currentLoadedCar = sucessfullyLoadedCars[index];
    }
    public void LoadNextCar()
    {
        if (!initialized) return;

        currentLoadedIndex = (currentLoadedIndex + 1) % totalLoadedCars;
        SpawnCar();
    }

    public void LoadPrevCar()
    {
        if (!initialized) return;

        currentLoadedIndex = (currentLoadedIndex - 1 + totalLoadedCars) % totalLoadedCars;
        SpawnCar();
    }


    public void LoadRace() 
    {
        GameData.playerCarName = currentLoadedCar.car.name;
        SceneManager.LoadScene(GameData.selectedTrackSceneName);
    }
}
