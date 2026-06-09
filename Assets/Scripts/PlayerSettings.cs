using UnityEngine;

public enum CameraStyle
{
    Responsive,
    CinematicDrift
}

public class PlayerSettings : MonoBehaviour
{
    [SerializeField] private CameraStyle cameraStyle = CameraStyle.Responsive;

    public CameraStyle CameraStyle => cameraStyle;

    public void SetCameraStyle(CameraStyle style)
    {
        cameraStyle = style;
    }
}
