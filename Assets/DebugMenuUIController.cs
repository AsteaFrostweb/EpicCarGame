using UnityEngine;

public class DebugMenuUIController : UserInterfaceMenu
{
    public Transform debugZoneTeleportLocation;
    public Transform circuitTeleportLocation;
    public Transform rampTeleportLocation;
    public Transform track4TeleportLocation;
    
    private void Start()
    {
        Inititalize();
    }


    private void TeleportPlayer(Transform targetLocation) 
    {
        Rigidbody rb = Player.mainPlayer.rb;
        rb.angularVelocity = new Vector3();
        rb.linearVelocity = new Vector3();
        Player.mainPlayer.transform.position = targetLocation.position;
        Player.mainPlayer.transform.rotation = targetLocation.rotation;
    }

    public void TeleportToTrack4() 
    {
        TeleportPlayer(track4TeleportLocation);
    }

    public void TeleportToDebugZone()
    {
        TeleportPlayer(debugZoneTeleportLocation);
    }
    public void TeleportToCircuit() 
    {
        TeleportPlayer(circuitTeleportLocation);
 
    }
    public void TeleportToRamp() 
    {
        TeleportPlayer(rampTeleportLocation);
    }
}
