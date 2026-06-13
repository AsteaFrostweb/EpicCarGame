using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarSpawnController : MonoBehaviour
{
    
    public List<Transform> carSpawnPoints;
    public bool[] assignedSpawnPoints;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       carSpawnPoints = transform.GetComponentsInChildren<Transform>().Where(t => t.gameObject.name.Contains("Spawn_")).ToList();
       assignedSpawnPoints = new bool[carSpawnPoints.Count];
    
    }

    public Transform GetSpawnPoint(int maxCars) 
    {       
            return GetAvailableSpawnPoint(maxCars);       
    }
    private Transform GetAvailableSpawnPoint(int maxCars)
    {
        // To ensure even distribution of cars across spawn points, we can calculate a step based on the number of spawn points and the maximum number of cars.
        // This way, we can skip some spawn points if there are more spawn points than cars.
        int spawnStep = carSpawnPoints.Count / maxCars;
        for (int i = 0; i < assignedSpawnPoints.Length; i+= spawnStep)
        {
            if (i > assignedSpawnPoints.Length - 1)
                return null;

            if (!assignedSpawnPoints[i])
            {
                assignedSpawnPoints[i] = true;
                return carSpawnPoints[i];
            }
        }
        return null; // No available spawn points
    }
}
