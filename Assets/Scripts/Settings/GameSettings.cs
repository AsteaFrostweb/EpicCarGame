using UnityEngine;

public class GameSettings
{
    public static float MasterVolume = 1f;
    public static float MusicVolume = 1f;
    public static float SfxVolume = 1f;
    public static float MouseSensitivity = 1f;
    public static bool Fullscreen = true;
    public static bool InvertY = false;
    public static int QualityLevel = 2;

    public static void Apply()
    {
        AudioListener.volume = Mathf.Clamp01(MasterVolume);
        Screen.fullScreen = Fullscreen;

        if (QualitySettings.names.Length > 0)
        {
            int quality = Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(quality);
            QualityLevel = quality;
        }
    }

    public static void ResetDefaults()
    {
        MasterVolume = 1f;
        MusicVolume = 1f;
        SfxVolume = 1f;
        MouseSensitivity = 1f;
        Fullscreen = true;
        InvertY = false;
        QualityLevel = Mathf.Clamp(2, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        Apply();
    }
}
