using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class FishingMinigameUI : MonoBehaviour
{
    public static FishingMinigameUI Instance { get; private set; }

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    
    // UI GameObjects
    private GameObject rootCanvasGo;
    private GameObject biteAlertGo;
    private GameObject minigameGroupGo;
    private GameObject minigamePanelGo;
    private GameObject trophyPanelGo;

    // Mini-game references
    private RectTransform catchBarRt;
    private RectTransform fishIconRt;
    private RectTransform progressFillRt;
    private Text timerText;

    // Trophy references
    private Image trophyFishImage;
    private Text trophyFishNameText;

    // Alert references
    private Text biteAlertText;

    private Font uiFont;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        uiFont = GetDefaultFont();
        CreateUIElements();
    }

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }

    private void CreateUIElements()
    {
        // 1. Create root Canvas GameObject
        rootCanvasGo = new GameObject("FishingMinigameCanvas");
        rootCanvasGo.transform.SetParent(transform);
        
        canvas = rootCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Draw on top of everything

        canvasScaler = rootCanvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        rootCanvasGo.AddComponent<GraphicRaycaster>();

        // 2. Create Bite Alert Prompt
        CreateBiteAlertUI();

        // 3. Create Minigame Panel
        CreateMinigamePanelUI();

        // 4. Create Trophy Success Panel
        CreateTrophyPanelUI();

        // Hide all panels by default
        HideAll();
    }

    private void CreateBiteAlertUI()
    {
        biteAlertGo = new GameObject("BiteAlertPanel");
        biteAlertGo.transform.SetParent(rootCanvasGo.transform, false);
        
        RectTransform rt = biteAlertGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.6f);
        rt.anchorMax = new Vector2(0.5f, 0.6f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 100);
        rt.anchoredPosition = Vector2.zero;

        // Background box for text
        Image bg = biteAlertGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // Alert Text
        GameObject textGo = new GameObject("AlertText");
        textGo.transform.SetParent(biteAlertGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        biteAlertText = textGo.AddComponent<Text>();
        biteAlertText.font = uiFont;
        biteAlertText.text = "🎣 BITE!\nPress LEFT CLICK or SPACE!";
        biteAlertText.color = new Color(1f, 0.8f, 0f); // Gold
        biteAlertText.fontSize = 24;
        biteAlertText.alignment = TextAnchor.MiddleCenter;
        biteAlertText.lineSpacing = 1.2f;
    }

    private void CreateMinigamePanelUI()
    {
        // Create master Minigame Group Container
        minigameGroupGo = new GameObject("MinigameGroup");
        minigameGroupGo.transform.SetParent(rootCanvasGo.transform, false);
        RectTransform groupRt = minigameGroupGo.AddComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(1f, 0.5f); // Right Center
        groupRt.anchorMax = new Vector2(1f, 0.5f);
        groupRt.pivot = new Vector2(1f, 0.5f);
        groupRt.sizeDelta = new Vector2(180, 500);
        groupRt.anchoredPosition = new Vector2(-100, 0);

        // 1. Sleek Drop Shadow for panel
        GameObject shadowGo = new GameObject("MinigamePanelShadow");
        shadowGo.transform.SetParent(minigameGroupGo.transform, false);
        RectTransform shadowRt = shadowGo.AddComponent<RectTransform>();
        shadowRt.anchorMin = Vector2.zero;
        shadowRt.anchorMax = Vector2.one;
        shadowRt.offsetMin = new Vector2(6, -6);
        shadowRt.offsetMax = new Vector2(6, -6);
        Image shadowImg = shadowGo.AddComponent<Image>();
        shadowImg.color = new Color(0f, 0f, 0f, 0.4f);

        // 2. Gold Border behind main panel (2px margins)
        GameObject outerBorderGo = new GameObject("MinigamePanelBorder");
        outerBorderGo.transform.SetParent(minigameGroupGo.transform, false);
        RectTransform borderRt = outerBorderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-2, -2);
        borderRt.offsetMax = new Vector2(2, 2);
        Image borderImg = outerBorderGo.AddComponent<Image>();
        borderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.9f); // Gold highlight border

        // 3. Main Panel Content
        minigamePanelGo = new GameObject("MinigamePanel");
        minigamePanelGo.transform.SetParent(minigameGroupGo.transform, false);
        RectTransform panelRt = minigamePanelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image bg = minigamePanelGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f); // Nice dark slate grey

        // Title/Instruction inside panel
        GameObject titleGo = new GameObject("InstructionText");
        titleGo.transform.SetParent(minigamePanelGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.9f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.sizeDelta = Vector2.zero;
        titleRt.anchoredPosition = Vector2.zero;

        Text titleText = titleGo.AddComponent<Text>();
        titleText.font = uiFont;
        titleText.text = "FISHING";
        titleText.color = new Color(1f, 0.85f, 0f); // Gold title
        titleText.fontSize = 18;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Tracks Border Highlight
        GameObject trackBorderGo = new GameObject("TrackBorder");
        trackBorderGo.transform.SetParent(minigamePanelGo.transform, false);
        RectTransform trackBorderRt = trackBorderGo.AddComponent<RectTransform>();
        trackBorderRt.anchorMin = new Vector2(0.25f, 0.1f);
        trackBorderRt.anchorMax = new Vector2(0.55f, 0.88f);
        trackBorderRt.offsetMin = new Vector2(-2, -2);
        trackBorderRt.offsetMax = new Vector2(2, 2);
        Image trackBorderImg = trackBorderGo.AddComponent<Image>();
        trackBorderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.6f); // Gold track border

        // Tracks container
        GameObject trackGo = new GameObject("Track");
        trackGo.transform.SetParent(minigamePanelGo.transform, false);
        RectTransform trackRt = trackGo.AddComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.25f, 0.1f);
        trackRt.anchorMax = new Vector2(0.55f, 0.88f);
        trackRt.sizeDelta = Vector2.zero;
        trackRt.anchoredPosition = Vector2.zero;

        Image trackBg = trackGo.AddComponent<Image>();
        trackBg.color = new Color(0.04f, 0.04f, 0.05f, 0.95f); // Solid dark track

        // Catch Bar (Green Zone)
        GameObject catchBarGo = new GameObject("CatchBar");
        catchBarGo.transform.SetParent(trackGo.transform, false);
        catchBarRt = catchBarGo.AddComponent<RectTransform>();
        catchBarRt.anchorMin = new Vector2(0f, 0.2f);
        catchBarRt.anchorMax = new Vector2(1f, 0.4f);
        catchBarRt.sizeDelta = Vector2.zero;
        catchBarRt.anchoredPosition = Vector2.zero;

        Image catchBarImg = catchBarGo.AddComponent<Image>();
        catchBarImg.color = new Color(0.05f, 0.82f, 0.35f, 0.45f); // Bright semi-transparent green

        // Catch Bar top neon border
        GameObject topHighlight = new GameObject("TopHighlight");
        topHighlight.transform.SetParent(catchBarGo.transform, false);
        RectTransform topRt = topHighlight.AddComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.sizeDelta = new Vector2(0, 3);
        topRt.anchoredPosition = Vector2.zero;
        Image topImg = topHighlight.AddComponent<Image>();
        topImg.color = new Color(1f, 1f, 1f, 0.85f);

        // Catch Bar bottom neon border
        GameObject bottomHighlight = new GameObject("BottomHighlight");
        bottomHighlight.transform.SetParent(catchBarGo.transform, false);
        RectTransform botRt = bottomHighlight.AddComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0f, 0f);
        botRt.anchorMax = new Vector2(1f, 0f);
        botRt.pivot = new Vector2(0.5f, 0f);
        botRt.sizeDelta = new Vector2(0, 3);
        botRt.anchoredPosition = Vector2.zero;
        Image botImg = bottomHighlight.AddComponent<Image>();
        botImg.color = new Color(1f, 1f, 1f, 0.85f);

        // Fish Icon
        GameObject fishIconGo = new GameObject("FishIcon");
        fishIconGo.transform.SetParent(trackGo.transform, false);
        fishIconRt = fishIconGo.AddComponent<RectTransform>();
        fishIconRt.anchorMin = new Vector2(0.5f, 0.5f);
        fishIconRt.anchorMax = new Vector2(0.5f, 0.5f);
        fishIconRt.pivot = new Vector2(0.5f, 0.5f);
        fishIconRt.sizeDelta = new Vector2(30, 30);
        fishIconRt.anchoredPosition = Vector2.zero;

        Image fishIconImg = fishIconGo.AddComponent<Image>();
        fishIconImg.color = new Color(1f, 0.65f, 0.0f); // Gold/Orange

        // Load standard bobber/fish sprite if available
        Sprite defaultFishSprite = LoadDefaultFishIcon();
        if (defaultFishSprite != null)
        {
            fishIconImg.sprite = defaultFishSprite;
            fishIconImg.color = Color.white; // reset to original colors of sprite
        }

        // Progress Bar Border Highlight
        GameObject progressBorderGo = new GameObject("ProgressBarBorder");
        progressBorderGo.transform.SetParent(minigamePanelGo.transform, false);
        RectTransform progressBorderRt = progressBorderGo.AddComponent<RectTransform>();
        progressBorderRt.anchorMin = new Vector2(0.68f, 0.1f);
        progressBorderRt.anchorMax = new Vector2(0.82f, 0.88f);
        progressBorderRt.offsetMin = new Vector2(-1, -1);
        progressBorderRt.offsetMax = new Vector2(1, 1);
        Image progressBorderImg = progressBorderGo.AddComponent<Image>();
        progressBorderImg.color = new Color(0.4f, 0.4f, 0.45f, 0.8f);

        // Progress Bar Background
        GameObject progressBgGo = new GameObject("ProgressBarBg");
        progressBgGo.transform.SetParent(minigamePanelGo.transform, false);
        RectTransform progressBgRt = progressBgGo.AddComponent<RectTransform>();
        progressBgRt.anchorMin = new Vector2(0.68f, 0.1f);
        progressBgRt.anchorMax = new Vector2(0.82f, 0.88f);
        progressBgRt.sizeDelta = Vector2.zero;
        progressBgRt.anchoredPosition = Vector2.zero;

        Image progressBg = progressBgGo.AddComponent<Image>();
        progressBg.color = new Color(0.2f, 0.2f, 0.22f, 1f);

        // Progress Bar Fill
        GameObject progressFillGo = new GameObject("ProgressBarFill");
        progressFillGo.transform.SetParent(progressBgGo.transform, false);
        progressFillRt = progressFillGo.AddComponent<RectTransform>();
        progressFillRt.anchorMin = Vector2.zero;
        progressFillRt.anchorMax = new Vector2(1f, 0.5f); // variable
        progressFillRt.sizeDelta = Vector2.zero;
        progressFillRt.anchoredPosition = Vector2.zero;

        Image progressFill = progressFillGo.AddComponent<Image>();
        progressFill.color = new Color(0f, 0.6f, 0.95f, 1f); // Sky blue

        // Timer/Label Text
        GameObject timerGo = new GameObject("TimerText");
        timerGo.transform.SetParent(minigamePanelGo.transform, false);
        RectTransform timerRt = timerGo.AddComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0f, 0f);
        timerRt.anchorMax = new Vector2(1f, 0.08f);
        timerRt.sizeDelta = Vector2.zero;
        timerRt.anchoredPosition = Vector2.zero;

        timerText = timerGo.AddComponent<Text>();
        timerText.font = uiFont;
        timerText.text = "Time: 15.0s";
        timerText.color = Color.yellow;
        timerText.fontSize = 11;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.lineSpacing = 1.1f;
    }

    private void CreateTrophyPanelUI()
    {
        // Outermost container - blocks raycasts and dims screen
        trophyPanelGo = new GameObject("TrophyPanel");
        trophyPanelGo.transform.SetParent(rootCanvasGo.transform, false);
        
        RectTransform rt = trophyPanelGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image overlayBg = trophyPanelGo.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.75f); // Dim backdrop

        // Gold border behind the modal
        GameObject borderGo = new GameObject("ModalBorder");
        borderGo.transform.SetParent(trophyPanelGo.transform, false);
        RectTransform borderRt = borderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = new Vector2(0.5f, 0.5f);
        borderRt.anchorMax = new Vector2(0.5f, 0.5f);
        borderRt.pivot = new Vector2(0.5f, 0.5f);
        borderRt.sizeDelta = new Vector2(404, 304);
        borderRt.anchoredPosition = Vector2.zero;

        Image borderImg = borderGo.AddComponent<Image>();
        borderImg.color = new Color(0.85f, 0.65f, 0.15f, 1f); // Gold border

        // Dialogue Box
        GameObject dialogGo = new GameObject("ModalDialog");
        dialogGo.transform.SetParent(borderGo.transform, false);
        RectTransform dialogRt = dialogGo.AddComponent<RectTransform>();
        dialogRt.anchorMin = Vector2.zero;
        dialogRt.anchorMax = Vector2.one;
        dialogRt.offsetMin = new Vector2(2, 2);
        dialogRt.offsetMax = new Vector2(-2, -2); // 2px margin inside border

        Image dialogBg = dialogGo.AddComponent<Image>();
        dialogBg.color = new Color(0.13f, 0.13f, 0.16f, 1f); // Dark background

        // Title: YOU CAUGHT A FISH!
        GameObject titleGo = new GameObject("TrophyTitle");
        titleGo.transform.SetParent(dialogGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.8f);
        titleRt.anchorMax = new Vector2(1f, 0.95f);
        titleRt.sizeDelta = Vector2.zero;
        titleRt.anchoredPosition = Vector2.zero;

        Text titleText = titleGo.AddComponent<Text>();
        titleText.font = uiFont;
        titleText.text = "⭐ NEW FISH CAUGHT! ⭐";
        titleText.color = new Color(1f, 0.85f, 0f);
        titleText.fontSize = 24;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Glowing Backdrop Halo behind the fish image
        GameObject glowGo = new GameObject("TrophyGlow");
        glowGo.transform.SetParent(dialogGo.transform, false);
        RectTransform glowRt = glowGo.AddComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0.5f, 0.5f);
        glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(170, 170);
        glowRt.anchoredPosition = new Vector2(0, 15);
        Image glowImg = glowGo.AddComponent<Image>();
        glowImg.color = new Color(1f, 0.85f, 0.3f, 0.12f); // Soft golden glow card backdrop

        // Fish Sprite Image
        GameObject fishImgGo = new GameObject("TrophyFishImage");
        fishImgGo.transform.SetParent(dialogGo.transform, false);
        RectTransform fishImgRt = fishImgGo.AddComponent<RectTransform>();
        fishImgRt.anchorMin = new Vector2(0.5f, 0.5f);
        fishImgRt.anchorMax = new Vector2(0.5f, 0.5f);
        fishImgRt.pivot = new Vector2(0.5f, 0.5f);
        fishImgRt.sizeDelta = new Vector2(100, 100);
        fishImgRt.anchoredPosition = new Vector2(0, 15);

        trophyFishImage = fishImgGo.AddComponent<Image>();
        trophyFishImage.preserveAspect = true;

        // Fish Name
        GameObject fishNameGo = new GameObject("TrophyFishName");
        fishNameGo.transform.SetParent(dialogGo.transform, false);
        RectTransform fishNameRt = fishNameGo.AddComponent<RectTransform>();
        fishNameRt.anchorMin = new Vector2(0f, 0.2f);
        fishNameRt.anchorMax = new Vector2(1f, 0.35f);
        fishNameRt.sizeDelta = Vector2.zero;
        fishNameRt.anchoredPosition = Vector2.zero;

        trophyFishNameText = fishNameGo.AddComponent<Text>();
        trophyFishNameText.font = uiFont;
        trophyFishNameText.text = "Bigmouth Bass";
        trophyFishNameText.color = Color.white;
        trophyFishNameText.fontSize = 20;
        trophyFishNameText.alignment = TextAnchor.MiddleCenter;

        // Subtitle instructions
        GameObject subtitleGo = new GameObject("TrophySubtitle");
        subtitleGo.transform.SetParent(dialogGo.transform, false);
        RectTransform subtitleRt = subtitleGo.AddComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0f, 0.05f);
        subtitleRt.anchorMax = new Vector2(1f, 0.18f);
        subtitleRt.sizeDelta = Vector2.zero;
        subtitleRt.anchoredPosition = Vector2.zero;

        Text subtitleText = subtitleGo.AddComponent<Text>();
        subtitleText.font = uiFont;
        subtitleText.text = "Press SPACE to continue";
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f);
        subtitleText.fontSize = 13;
        subtitleText.alignment = TextAnchor.MiddleCenter;
    }

    private Sprite LoadDefaultFishIcon()
    {
        // Try to load fish_bobber-standard.png as a default icon
        string path = "Assets/Model/Fishes/fish_bobber-standard.png";
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    // --- Control Methods ---

    public void ShowBiteAlert()
    {
        biteAlertGo.SetActive(true);
    }

    public void HideBiteAlert()
    {
        biteAlertGo.SetActive(false);
    }

    public void ShowMinigame()
    {
        HideBiteAlert();
        if (minigameGroupGo != null) minigameGroupGo.SetActive(true);
    }

    public void HideMinigame()
    {
        if (minigameGroupGo != null) minigameGroupGo.SetActive(false);
    }

    public void UpdateMinigame(float fishPos, float barPos, float barSize, float progress, float timeLeft)
    {
        // 1. Update Fish position
        fishIconRt.anchorMin = new Vector2(0.5f, fishPos);
        fishIconRt.anchorMax = new Vector2(0.5f, fishPos);
        fishIconRt.anchoredPosition = Vector2.zero;

        // 2. Update Catch Bar position & height
        float halfSize = barSize / 2f;
        catchBarRt.anchorMin = new Vector2(0f, Mathf.Clamp01(barPos - halfSize));
        catchBarRt.anchorMax = new Vector2(1f, Mathf.Clamp01(barPos + halfSize));
        catchBarRt.offsetMin = Vector2.zero;
        catchBarRt.offsetMax = Vector2.zero;

        // 3. Update Progress Bar
        progressFillRt.anchorMax = new Vector2(1f, Mathf.Clamp01(progress));
        progressFillRt.offsetMin = Vector2.zero;
        progressFillRt.offsetMax = Vector2.zero;

        // 4. Update Timer Text
        timerText.text = string.Format("Time: {0:F1}s", timeLeft);
    }

    public void ShowTrophy(Sprite fishSprite, string fishName)
    {
        trophyFishImage.sprite = fishSprite;
        trophyFishNameText.text = fishName;
        trophyPanelGo.SetActive(true);
    }

    public void HideTrophy()
    {
        trophyPanelGo.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        // Shows general info message via bite alert
        biteAlertText.text = message;
        biteAlertGo.SetActive(true);
    }

    public void HideAll()
    {
        if (biteAlertGo != null) biteAlertGo.SetActive(false);
        if (minigameGroupGo != null) minigameGroupGo.SetActive(false);
        if (trophyPanelGo != null) trophyPanelGo.SetActive(false);
    }
}
