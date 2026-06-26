using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenu : MonoBehaviour
{
    private static PauseMenu instance;

    private GameObject pauseCanvasGo;
    private GameObject pausePanel;
    private GameObject settingsPanel;

    private bool isPaused = false;

    // Settings
    private Slider volumeSlider;
    private Text volumeText;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        CreatePauseUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ngăn mở menu Tạm Dừng khi đang ở MainMenu, nhưng vẫn cho phép đóng Settings bằng ESC
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                return;
            }

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseCanvasGo.SetActive(true);
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseCanvasGo.SetActive(false);
    }

    public void OpenSettings()
    {
        pauseCanvasGo.SetActive(true);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
        
        // Load current settings
        if (volumeSlider != null) {
            volumeSlider.value = AudioListener.volume;
        }
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        if (isPaused) {
            pausePanel.SetActive(true);
        } else {
            pauseCanvasGo.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f; // Ensure time is running before quitting
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetVolume(float vol)
    {
        AudioListener.volume = vol;
        if (volumeText != null) {
            volumeText.text = $"Âm lượng: {Mathf.RoundToInt(vol * 100)}%";
        }
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    // Helper to create UI programmatically
    private void CreatePauseUI()
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Canvas
        pauseCanvasGo = new GameObject("PauseCanvas");
        Canvas canvas = pauseCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = pauseCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        pauseCanvasGo.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(pauseCanvasGo);

        // --- PAUSE PANEL ---
        pausePanel = CreatePanel(pauseCanvasGo.transform, "PausePanel", new Color(0, 0, 0, 0.85f));
        
        Text titleText = CreateText(pausePanel.transform, "Title", "TẠM DỪNG", uiFont, 70, new Vector2(0, 300));
        titleText.color = new Color(1f, 0.85f, 0f); // Gold title
        
        Button btnResume = CreateButton(pausePanel.transform, "BtnResume", "Tiếp tục", uiFont, new Vector2(0, 100), new Vector2(300, 80));
        btnResume.onClick.AddListener(ResumeGame);

        Button btnSettings = CreateButton(pausePanel.transform, "BtnSettings", "Cài đặt", uiFont, new Vector2(0, 0), new Vector2(300, 80));
        btnSettings.onClick.AddListener(OpenSettings);

        Button btnQuit = CreateButton(pausePanel.transform, "BtnQuit", "Thoát", uiFont, new Vector2(0, -100), new Vector2(300, 80));
        btnQuit.onClick.AddListener(QuitGame);

        // --- SETTINGS PANEL ---
        settingsPanel = CreatePanel(pauseCanvasGo.transform, "SettingsPanel", new Color(0.05f, 0.05f, 0.1f, 0.95f));
        
        Text settingsTitle = CreateText(settingsPanel.transform, "Title", "CÀI ĐẶT", uiFont, 60, new Vector2(0, 350));
        settingsTitle.color = new Color(0.4f, 0.8f, 1f); // Blueish title

        // Volume Slider
        volumeText = CreateText(settingsPanel.transform, "VolumeText", "Âm lượng: 100%", uiFont, 30, new Vector2(0, 200));
        volumeSlider = CreateSlider(settingsPanel.transform, "VolumeSlider", new Vector2(0, 150), new Vector2(400, 40));
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(SetVolume);
        
        // Initialize volume text
        SetVolume(AudioListener.volume);

        // Quality Settings
        Text qualityText = CreateText(settingsPanel.transform, "QualityText", "Chất lượng Đồ họa", uiFont, 30, new Vector2(0, 50));
        
        Button btnLow = CreateButton(settingsPanel.transform, "BtnLow", "Thấp", uiFont, new Vector2(-150, -20), new Vector2(120, 60));
        btnLow.onClick.AddListener(() => SetQuality(0)); // Usually Low

        Button btnMed = CreateButton(settingsPanel.transform, "BtnMed", "Vừa", uiFont, new Vector2(0, -20), new Vector2(120, 60));
        btnMed.onClick.AddListener(() => SetQuality(2)); // Usually Medium/High

        Button btnHigh = CreateButton(settingsPanel.transform, "BtnHigh", "Cao", uiFont, new Vector2(150, -20), new Vector2(120, 60));
        btnHigh.onClick.AddListener(() => SetQuality(5)); // Usually Ultra

        // Back Button
        Button btnBack = CreateButton(settingsPanel.transform, "BtnBack", "Quay lại", uiFont, new Vector2(0, -300), new Vector2(300, 80));
        btnBack.onClick.AddListener(CloseSettings);

        // Hide initially
        pauseCanvasGo.SetActive(false);
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private Text CreateText(Transform parent, string name, string textStr, Font font, int fontSize, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 100);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = textStr;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private Button CreateButton(Transform parent, string name, string textStr, Font font, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark gray

        Button btn = go.AddComponent<Button>();
        
        // Hover colors
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        cb.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        // Button text
        Text text = CreateText(go.transform, "Text", textStr, font, 24, Vector2.zero);
        text.GetComponent<RectTransform>().sizeDelta = size;

        return btn;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Background
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.25f);
        bgRt.anchorMax = new Vector2(1, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(go.transform, false);
        RectTransform fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1, 0.75f);
        fillAreaRt.sizeDelta = new Vector2(-20, 0);

        // Fill
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.7f, 1f, 1f); // Blue fill

        // Handle Slide Area
        GameObject handleAreaGo = new GameObject("Handle Slide Area");
        handleAreaGo.transform.SetParent(go.transform, false);
        RectTransform handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.sizeDelta = new Vector2(-20, 0);

        // Handle
        GameObject handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        RectTransform handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 0);
        Image handleImg = handleGo.AddComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;

        return slider;
    }
}
