using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string playSceneName = "OutdoorsScene";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Dropdown qualityDropdown;

    private void Start()
    {
        PopulateQualityDropdown();
        RefreshSettingsUI();
        ShowMainMenu();
        GameSettings.Apply();
    }

    public void Configure(
        string playSceneName,
        GameObject mainMenuPanel,
        GameObject settingsPanel,
        Slider masterVolumeSlider,
        Slider musicVolumeSlider,
        Slider sfxVolumeSlider,
        Slider mouseSensitivitySlider,
        Toggle fullscreenToggle,
        Toggle invertYToggle,
        Dropdown qualityDropdown)
    {
        this.playSceneName = playSceneName;
        this.mainMenuPanel = mainMenuPanel;
        this.settingsPanel = settingsPanel;
        this.masterVolumeSlider = masterVolumeSlider;
        this.musicVolumeSlider = musicVolumeSlider;
        this.sfxVolumeSlider = sfxVolumeSlider;
        this.mouseSensitivitySlider = mouseSensitivitySlider;
        this.fullscreenToggle = fullscreenToggle;
        this.invertYToggle = invertYToggle;
        this.qualityDropdown = qualityDropdown;

        PopulateQualityDropdown();
        RefreshSettingsUI();
        ShowMainMenu();
    }

    public void Play()
    {
        LoadScene(playSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("MainMenuUIController was asked to load an empty scene name.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(settingsPanel, false);
    }

    public void ShowSettings()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(settingsPanel, true);
        RefreshSettingsUI();
    }

    public void ToggleSettings()
    {
        bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;

        if (settingsOpen)
        {
            ShowMainMenu();
        }
        else
        {
            ShowSettings();
        }
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetMasterVolume(float value)
    {
        GameSettings.MasterVolume = Mathf.Clamp01(value);
        GameSettings.Apply();
    }

    public void SetMusicVolume(float value)
    {
        GameSettings.MusicVolume = Mathf.Clamp01(value);
    }

    public void SetSfxVolume(float value)
    {
        GameSettings.SfxVolume = Mathf.Clamp01(value);
    }

    public void SetMouseSensitivity(float value)
    {
        GameSettings.MouseSensitivity = Mathf.Max(0.1f, value);
    }

    public void SetFullscreen(bool value)
    {
        GameSettings.Fullscreen = value;
        GameSettings.Apply();
    }

    public void SetInvertY(bool value)
    {
        GameSettings.InvertY = value;
    }

    public void SetQualityLevel(int value)
    {
        GameSettings.QualityLevel = value;
        GameSettings.Apply();
    }

    public void ResetSettings()
    {
        GameSettings.ResetDefaults();
        RefreshSettingsUI();
    }

    private void PopulateQualityDropdown()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
    }

    private void RefreshSettingsUI()
    {
        SetSliderValueWithoutNotify(masterVolumeSlider, GameSettings.MasterVolume);
        SetSliderValueWithoutNotify(musicVolumeSlider, GameSettings.MusicVolume);
        SetSliderValueWithoutNotify(sfxVolumeSlider, GameSettings.SfxVolume);
        SetSliderValueWithoutNotify(mouseSensitivitySlider, GameSettings.MouseSensitivity);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        }

        if (invertYToggle != null)
        {
            invertYToggle.SetIsOnWithoutNotify(GameSettings.InvertY);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(GameSettings.QualityLevel, 0, qualityDropdown.options.Count - 1));
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(value);
        }
    }
}
