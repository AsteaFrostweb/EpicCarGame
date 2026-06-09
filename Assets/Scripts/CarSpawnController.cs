using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarSpawnController : MonoBehaviour
{
    public static CarSpawnController instance;
    public List<Transform> carSpawnPoints;
    public bool[] assignedSpawnPoints;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       carSpawnPoints = transform.GetComponentsInChildren<Transform>().Where(t => t.gameObject.name.Contains("Spawn_")).ToList();
       assignedSpawnPoints = new bool[carSpawnPoints.Count];
       instance = this;
    }

    public static Transform GetSpawnPoint() 
    {
        if (instance != null)
        {
            return instance.GetAvailableSpawnPoint();
        }
        return null; // No instance available
    }
    private Transform GetAvailableSpawnPoint()
    {
        for (int i = 0; i < assignedSpawnPoints.Length; i++)
        {
            if (!assignedSpawnPoints[i])
            {
                assignedSpawnPoints[i] = true;
                return carSpawnPoints[i];
            }
        }
        return null; // No available spawn points
    }
}
