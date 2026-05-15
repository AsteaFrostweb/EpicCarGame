using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneGenerator
{
    private const string MainMenuScenePath = "Assets/MainMenu.unity";
    private const string PlayScenePath = "Assets/OutdoorsScene.unity";

    [MenuItem("Tools/Create Main Menu Scene")]
    public static void GenerateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        GameObject controllerObject = new GameObject("MainMenuUIController");
        MainMenuUIController controller = controllerObject.AddComponent<MainMenuUIController>();

        CreateCamera();
        CreateEventSystem();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);

        GameObject mainMenuPanel = CreatePanel(canvas.transform, "Main Menu Panel", new Vector2(520f, 560f));
        GameObject settingsPanel = CreatePanel(canvas.transform, "Settings Panel", new Vector2(720f, 700f));

        CreateTitle(mainMenuPanel.transform, "CAR GAME", new Vector2(0f, 185f), 64);
        Button playButton = CreateButton(mainMenuPanel.transform, "Play Button", "PLAY", new Vector2(0f, 70f));
        Button settingsButton = CreateButton(mainMenuPanel.transform, "Settings Button", "SETTINGS", new Vector2(0f, -20f));
        Button quitButton = CreateButton(mainMenuPanel.transform, "Quit Button", "QUIT", new Vector2(0f, -110f));

        CreateTitle(settingsPanel.transform, "SETTINGS", new Vector2(0f, 275f), 46);
        Slider masterVolume = CreateSettingSlider(settingsPanel.transform, "Master Volume", new Vector2(0f, 185f), 0f, 1f, GameSettings.MasterVolume);
        Slider musicVolume = CreateSettingSlider(settingsPanel.transform, "Music Volume", new Vector2(0f, 105f), 0f, 1f, GameSettings.MusicVolume);
        Slider sfxVolume = CreateSettingSlider(settingsPanel.transform, "SFX Volume", new Vector2(0f, 25f), 0f, 1f, GameSettings.SfxVolume);
        Slider mouseSensitivity = CreateSettingSlider(settingsPanel.transform, "Mouse Sensitivity", new Vector2(0f, -55f), 0.1f, 3f, GameSettings.MouseSensitivity);
        Toggle fullscreen = CreateSettingToggle(settingsPanel.transform, "Fullscreen", new Vector2(-160f, -135f), GameSettings.Fullscreen);
        Toggle invertY = CreateSettingToggle(settingsPanel.transform, "Invert Y", new Vector2(160f, -135f), GameSettings.InvertY);
        Dropdown quality = CreateSettingDropdown(settingsPanel.transform, "Quality", new Vector2(0f, -215f));
        Button resetButton = CreateButton(settingsPanel.transform, "Reset Settings Button", "RESET", new Vector2(-140f, -300f), new Vector2(220f, 58f));
        Button backButton = CreateButton(settingsPanel.transform, "Back Button", "BACK", new Vector2(140f, -300f), new Vector2(220f, 58f));

        UnityEventTools.AddPersistentListener(playButton.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.ShowSettings);
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.Quit);
        UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowMainMenu);
        UnityEventTools.AddPersistentListener(resetButton.onClick, controller.ResetSettings);
        UnityEventTools.AddPersistentListener(masterVolume.onValueChanged, controller.SetMasterVolume);
        UnityEventTools.AddPersistentListener(musicVolume.onValueChanged, controller.SetMusicVolume);
        UnityEventTools.AddPersistentListener(sfxVolume.onValueChanged, controller.SetSfxVolume);
        UnityEventTools.AddPersistentListener(mouseSensitivity.onValueChanged, controller.SetMouseSensitivity);
        UnityEventTools.AddPersistentListener(fullscreen.onValueChanged, controller.SetFullscreen);
        UnityEventTools.AddPersistentListener(invertY.onValueChanged, controller.SetInvertY);
        UnityEventTools.AddPersistentListener(quality.onValueChanged, controller.SetQualityLevel);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        serializedController.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serializedController.FindProperty("masterVolumeSlider").objectReferenceValue = masterVolume;
        serializedController.FindProperty("musicVolumeSlider").objectReferenceValue = musicVolume;
        serializedController.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxVolume;
        serializedController.FindProperty("mouseSensitivitySlider").objectReferenceValue = mouseSensitivity;
        serializedController.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
        serializedController.FindProperty("invertYToggle").objectReferenceValue = invertY;
        serializedController.FindProperty("qualityDropdown").objectReferenceValue = quality;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        settingsPanel.SetActive(false);

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f);
        cameraObject.tag = "MainCamera";
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject background = CreateUIObject(parent, "Background");
        Image image = background.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.03f);

        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = CreateUIObject(parent, name);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.09f, 0.1f, 0.92f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        return panel;
    }

    private static void CreateTitle(Transform parent, string text, Vector2 position, int fontSize)
    {
        Text title = CreateText(parent, "Title", text, fontSize, TextAnchor.MiddleCenter, Color.white);
        RectTransform rect = title.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(460f, 90f);
        rect.anchoredPosition = position;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        return CreateButton(parent, name, label, position, new Vector2(320f, 64f));
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateUIObject(parent, name);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.52f, 0.9f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.24f, 0.62f, 1f);
        colors.pressedColor = new Color(0.11f, 0.35f, 0.65f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = CreateText(buttonObject.transform, "Text", label, 28, TextAnchor.MiddleCenter, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Slider CreateSettingSlider(Transform parent, string label, Vector2 position, float min, float max, float value)
    {
        CreateSettingLabel(parent, label, position + new Vector2(-230f, 0f));

        GameObject sliderObject = CreateUIObject(parent, label + " Slider");
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(330f, 24f);
        sliderRect.anchoredPosition = position + new Vector2(135f, 0f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        GameObject background = CreateUIObject(sliderObject.transform, "Background");
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.22f);
        Stretch(background.GetComponent<RectTransform>());

        GameObject fillArea = CreateUIObject(sliderObject.transform, "Fill Area");
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = CreateUIObject(fillArea.transform, "Fill");
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.18f, 0.52f, 0.9f);
        Stretch(fill.GetComponent<RectTransform>());

        GameObject handleArea = CreateUIObject(sliderObject.transform, "Handle Slide Area");
        Stretch(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUIObject(handleArea.transform, "Handle");
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26f, 30f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static Toggle CreateSettingToggle(Transform parent, string label, Vector2 position, bool value)
    {
        GameObject toggleObject = CreateUIObject(parent, label + " Toggle");
        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 40f);
        rect.anchoredPosition = position;

        Toggle toggle = toggleObject.AddComponent<Toggle>();
        toggle.isOn = value;

        GameObject background = CreateUIObject(toggleObject.transform, "Background");
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.22f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(32f, 32f);
        backgroundRect.anchoredPosition = new Vector2(16f, 0f);

        GameObject checkmark = CreateUIObject(background.transform, "Checkmark");
        Image checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.color = new Color(0.18f, 0.52f, 0.9f);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        Text text = CreateText(toggleObject.transform, "Label", label, 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(48f, 0f);
        textRect.offsetMax = Vector2.zero;

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        return toggle;
    }

    private static Dropdown CreateSettingDropdown(Transform parent, string label, Vector2 position)
    {
        CreateSettingLabel(parent, label, position + new Vector2(-230f, 0f));

        GameObject dropdownObject = CreateUIObject(parent, label + " Dropdown");
        Image image = dropdownObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.22f);

        Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();
        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(330f, 44f);
        rect.anchoredPosition = position + new Vector2(135f, 0f);

        Text caption = CreateText(dropdownObject.transform, "Label", string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        RectTransform captionRect = caption.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(14f, 0f);
        captionRect.offsetMax = new Vector2(-44f, 0f);

        Text arrow = CreateText(dropdownObject.transform, "Arrow", "v", 20, TextAnchor.MiddleCenter, Color.white);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.sizeDelta = new Vector2(34f, 0f);
        arrowRect.anchoredPosition = new Vector2(-18f, 0f);

        GameObject template = CreateDropdownTemplate(dropdownObject.transform);
        dropdown.template = template.GetComponent<RectTransform>();
        dropdown.captionText = caption;
        dropdown.itemText = template.transform.Find("Viewport/Content/Item/Item Label").GetComponent<Text>();
        dropdown.targetGraphic = image;
        dropdown.AddOptions(new List<string>(QualitySettings.names));
        dropdown.value = Mathf.Clamp(GameSettings.QualityLevel, 0, Mathf.Max(0, dropdown.options.Count - 1));
        template.SetActive(false);

        return dropdown;
    }

    private static GameObject CreateDropdownTemplate(Transform parent)
    {
        GameObject template = CreateUIObject(parent, "Template");
        Image image = template.AddComponent<Image>();
        image.color = new Color(0.1f, 0.11f, 0.12f);
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();

        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 180f);
        templateRect.anchoredPosition = new Vector2(0f, -4f);

        GameObject viewport = CreateUIObject(template.transform, "Viewport");
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.white;
        Stretch(viewport.GetComponent<RectTransform>());

        GameObject content = CreateUIObject(viewport.transform, "Content");
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 40f);

        GameObject item = CreateUIObject(content.transform, "Item");
        Toggle itemToggle = item.AddComponent<Toggle>();
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 40f);

        Text itemLabel = CreateText(item.transform, "Item Label", "Option", 20, TextAnchor.MiddleLeft, Color.white);
        RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(12f, 0f);
        itemLabelRect.offsetMax = Vector2.zero;

        itemToggle.targetGraphic = itemLabel;
        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        return template;
    }

    private static void CreateSettingLabel(Transform parent, string label, Vector2 position)
    {
        Text text = CreateText(parent, label + " Label", label, 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 44f);
        rect.anchoredPosition = position;
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = CreateUIObject(parent, name);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static GameObject CreateUIObject(Transform parent, string name)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true)
        };

        if (System.IO.File.Exists(PlayScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(PlayScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
