using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player mainPlayer { get; private set; }
    public Rigidbody rb => GetComponent<Rigidbody>();
    public bool isMainPlayer = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(mainPlayer == null)
            mainPlayer = this;         
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
