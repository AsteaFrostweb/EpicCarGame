using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

public class GarageUIMasterController : MonoBehaviour
{
    [Serializable]
    public struct CarLoadEntity 
    {
        public string name;
        public GameObject reference;
        public GameObject spawnInstance;
    }
    public List<string> carsToLoad = new List<string>() {"Subaru", "Pickle", "Beemer", "Mustang"};
    public List<CarLoadEntity> sucessfullyLoadedCars = new List<CarLoadEntity>();

    public GameObject CarSpawnPosition;

    public CarLoadEntity currentLoadedCar;
    private int currentLoadedIndex = 0;
    private int totalLoadedCars;

    private bool initialized = true;

    private void Start()
    {
        foreach (string carName in carsToLoad) 
        {
            GameObject obj = Resources.Load<GameObject>("Prefabs/Cars/" + carName);

            if (obj != null)
            {
                CarLoadEntity cle = new CarLoadEntity 
                {
                    name = carName,
                    reference = obj,
                    spawnInstance = null
                };

                sucessfullyLoadedCars.Add(cle);
            }            
        }
        totalLoadedCars = sucessfullyLoadedCars.Count;

        //if the total loaded cars is 0 then set the UI to be un-initialized
        if (totalLoadedCars == 0) initialized = false;

        SpawnCar();
    }

    private void SpawnCar() 
    {
        LoadCar(currentLoadedIndex);
        currentLoadedCar.spawnInstance = GameObject.Instantiate(currentLoadedCar.reference, CarSpawnPosition.transform);
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

}
