using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class SellFishStoreUI : MonoBehaviour
    {
        public static SellFishStoreUI Instance { get; private set; }

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 3.5f;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.5f, 0f);

        private GameObject playerMC;
        private GameObject shopStall;
        private Inventory playerInventory;

        // UI GameObjects (Constructed dynamically)
        private GameObject uiPanelRoot;
        private CanvasGroup panelCanvasGroup;
        private GameObject promptRoot;
        private RectTransform promptRt;
        private Transform gridParent;
        private TextMeshProUGUI goldText;

        // Details Panel References
        private GameObject detailsPanel;
        private TextMeshProUGUI detailNameText;
        private TextMeshProUGUI detailRarityText;
        private TextMeshProUGUI detailDescText;
        private TextMeshProUGUI detailPriceText;
        private Image detailIcon;
        private Button sell1Button;
        private Button sellAllButton;

        private Sprite slotBgSprite;
        private bool isOpen = false;
        private bool isPlayerInRange = false;

        private List<SellFishSlotUI> slotUIList = new List<SellFishSlotUI>();
        private SellFishSlotUI selectedSlotUI;

        // Track original inventory index for each displayed slot
        private List<int> slotInventoryIndices = new List<int>();
        private List<ItemData> catalogFishItems = new List<ItemData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find references
            playerMC = GameObject.Find("MC");
            shopStall = GameObject.Find("fishstalllow");

            if (playerMC != null)
            {
                playerInventory = playerMC.GetComponent<Inventory>();
            }

            if (playerInventory != null)
            {
                playerInventory.onInventoryChanged += RefreshShopUI;
                playerInventory.onGoldChanged += UpdateGoldDisplay;
            }

            // Load Slot Sprite
            slotBgSprite = Resources.Load<Sprite>("Sprites/ItemSlots/Item_Slot-5");
            if (slotBgSprite == null)
            {
                Debug.LogWarning("SellFishStoreUI: Could not load Item_Slot-5.png sprite from Resources, using default.");
            }

            // Initialize fish catalog
            InitializeCatalog();

            // Construct UI elements
            CreateInteractionPrompt();
            CreateShopPanelUI();

            // Set initial state
            CloseShop();
        }

        private void OnDestroy()
        {
            if (playerInventory != null)
            {
                playerInventory.onInventoryChanged -= RefreshShopUI;
                playerInventory.onGoldChanged -= UpdateGoldDisplay;
            }
        }

        private void Update()
        {
            if (playerMC == null || shopStall == null) return;

            // Distance Check (ignoring height Y differences for 2.5D layout check)
            Vector3 playerPos = playerMC.transform.position;
            Vector3 shopPos = shopStall.transform.position;
            playerPos.y = 0;
            shopPos.y = 0;

            float dist = Vector3.Distance(playerPos, shopPos);
            bool inRange = dist <= interactionRadius;

            if (inRange != isPlayerInRange)
            {
                isPlayerInRange = inRange;
                if (isPlayerInRange)
                {
                    ShowPrompt();
                }
                else
                {
                    HidePrompt();
                    if (isOpen) CloseShop();
                }
            }

            // Update prompt position above the shop stall
            if (isPlayerInRange && promptRt != null)
            {
                Vector3 worldPos = shopStall.transform.position + promptOffset;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                
                // If camera is behind target, screenPos.z is negative
                if (screenPos.z < 0)
                {
                    promptRoot.SetActive(false);
                }
                else
                {
                    if (!promptRoot.activeSelf) promptRoot.SetActive(true);
                    promptRt.position = screenPos;
                }
            }

            // Key Handling
            if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
            {
                ToggleShop();
            }

            // Close with Escape if open
            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShop();
            }
        }

        private void ToggleShop()
        {
            if (isOpen) CloseShop();
            else OpenShop();
        }

        public void OpenShop()
        {
            isOpen = true;
            DeselectAll();
            RefreshShopUI();
            UpdateGoldDisplay(playerInventory != null ? playerInventory.Gold : 0);
            StopAllCoroutines();
            StartCoroutine(FadePanel(1f, true));
        }

        public void CloseShop()
        {
            isOpen = false;
            DeselectAll();
            StopAllCoroutines();
            StartCoroutine(FadePanel(0f, false));
        }

        private System.Collections.IEnumerator FadePanel(float targetAlpha, bool interactable)
        {
            if (panelCanvasGroup == null) yield break;

            panelCanvasGroup.blocksRaycasts = interactable;
            panelCanvasGroup.interactable = interactable;

            float startAlpha = panelCanvasGroup.alpha;
            float elapsed = 0f;
            float fadeTime = 0.2f;

            while (elapsed < fadeTime)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            panelCanvasGroup.alpha = targetAlpha;
        }

        private void CreateInteractionPrompt()
        {
            Canvas mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas == null) return;

            promptRoot = new GameObject("FishShopPrompt");
            promptRoot.transform.SetParent(mainCanvas.transform, false);

            promptRt = promptRoot.AddComponent<RectTransform>();
            promptRt.sizeDelta = new Vector2(160, 50);

            // Background
            Image bg = promptRoot.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.14f, 0.9f); // Slate dark
            
            // Add a subtle border
            GameObject borderGo = new GameObject("Border");
            borderGo.transform.SetParent(promptRoot.transform, false);
            RectTransform borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.8f); // Gold tint
            borderGo.transform.SetAsFirstSibling();

            // Text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(promptRoot.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = "<color=#FFD700>[E]</color> Sell Fish";
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.font = GetDefaultFontAsset();

            promptRoot.SetActive(false);
        }

        private void CreateShopPanelUI()
        {
            Canvas mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas == null) return;

            // 1. Root shop UI panel
            uiPanelRoot = new GameObject("SellFishStoreUI");
            uiPanelRoot.transform.SetParent(mainCanvas.transform, false);
            RectTransform rootRt = uiPanelRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            panelCanvasGroup = uiPanelRoot.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;

            // Blur/glassmorphic backdrop overlay
            GameObject overlayGo = new GameObject("OverlayBg");
            overlayGo.transform.SetParent(uiPanelRoot.transform, false);
            RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            Image overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.65f); // Semi-transparent dark overlay

            // Close shop by clicking backdrop
            Button overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(CloseShop);

            // 2. Main Shop Window Panel (Gold/Slate Theme)
            GameObject windowGo = new GameObject("ShopWindow");
            windowGo.transform.SetParent(uiPanelRoot.transform, false);
            RectTransform windowRt = windowGo.AddComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(900, 560);
            windowRt.anchoredPosition = Vector2.zero;

            // Border Outline
            GameObject borderGo = new GameObject("WindowBorder");
            borderGo.transform.SetParent(windowGo.transform, false);
            RectTransform borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2);
            borderRt.offsetMax = new Vector2(2, 2);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.95f); // Premium Gold

            // Backdrop
            Image windowImg = windowGo.AddComponent<Image>();
            windowImg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f); // Dark Slate Grey

            // Header Section
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(windowGo.transform, false);
            RectTransform headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 0.86f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.sizeDelta = Vector2.zero;
            headerRt.anchoredPosition = Vector2.zero;

            // Title text
            TextMeshProUGUI titleText = CreateText(headerGo.transform, "TitleText", "FISH STALL", 28, new Color(0.08f, 0.08f, 0.1f));
            RectTransform titleRt = titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(30, 0);
            titleRt.sizeDelta = new Vector2(300, 50);

            // Gold display text
            goldText = CreateText(headerGo.transform, "GoldText", "Total Gold: 0g", 20, new Color(0.08f, 0.08f, 0.1f), TextAlignmentOptions.Right);
            RectTransform goldRt = goldText.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(1f, 0.5f);
            goldRt.anchorMax = new Vector2(1f, 0.5f);
            goldRt.pivot = new Vector2(1f, 0.5f);
            goldRt.anchoredPosition = new Vector2(-90, 0);
            goldRt.sizeDelta = new Vector2(300, 50);

            // Close button (X)
            GameObject closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(headerGo.transform, false);
            RectTransform closeRt = closeGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-30, 0);
            closeRt.sizeDelta = new Vector2(36, 36);

            Image closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.75f, 0.15f, 0.15f);
            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseShop);

            TextMeshProUGUI closeX = CreateText(closeGo.transform, "X", "X", 18, Color.white, TextAlignmentOptions.Center);
            RectTransform closeXRt = closeX.GetComponent<RectTransform>();
            closeXRt.anchorMin = Vector2.zero;
            closeXRt.anchorMax = Vector2.one;
            closeXRt.sizeDelta = Vector2.zero;

            // 3. Body Section - Splits Left Grid and Right Details
            GameObject bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(windowGo.transform, false);
            RectTransform bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = new Vector2(1f, 0.86f);
            bodyRt.offsetMin = new Vector2(30, 30);
            bodyRt.offsetMax = new Vector2(-30, 0);

            // LEFT SIDE: Grid container (now scrollable)
            GameObject gridContainerGo = new GameObject("GridContainer");
            gridContainerGo.transform.SetParent(bodyGo.transform, false);
            RectTransform gridContainerRt = gridContainerGo.AddComponent<RectTransform>();
            gridContainerRt.anchorMin = Vector2.zero;
            gridContainerRt.anchorMax = new Vector2(0.6f, 1f); // 60% width
            gridContainerRt.offsetMin = Vector2.zero;
            gridContainerRt.offsetMax = new Vector2(-15, 0);

            // Grid Backplate
            Image gridBackplate = gridContainerGo.AddComponent<Image>();
            gridBackplate.color = new Color(0.06f, 0.06f, 0.08f, 0.85f); // Soft frame backplate

            // Add Scroll View Components
            gridContainerGo.AddComponent<RectMask2D>();
            ScrollRect scrollRect = gridContainerGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Grid Content GameObject
            GameObject contentGo = new GameObject("GridContent");
            contentGo.transform.SetParent(gridContainerGo.transform, false);
            RectTransform contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 300f);

            scrollRect.content = contentRt;

            // Setup Grid Layout
            gridParent = contentGo.transform;
            GridLayoutGroup gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(18, 18, 18, 18);
            gridLayout.cellSize = new Vector2(74, 74);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // RIGHT SIDE: Details panel
            detailsPanel = new GameObject("DetailsPanel");
            detailsPanel.transform.SetParent(bodyGo.transform, false);
            RectTransform detailsRt = detailsPanel.AddComponent<RectTransform>();
            detailsRt.anchorMin = new Vector2(0.6f, 0f); // 40% width
            detailsRt.anchorMax = new Vector2(1f, 1f);
            detailsRt.offsetMin = new Vector2(15, 0);
            detailsRt.offsetMax = Vector2.zero;

            // Details Border
            GameObject detailsBorderGo = new GameObject("DetailsBorder");
            detailsBorderGo.transform.SetParent(detailsPanel.transform, false);
            RectTransform detBorderRt = detailsBorderGo.AddComponent<RectTransform>();
            detBorderRt.anchorMin = Vector2.zero;
            detBorderRt.anchorMax = Vector2.one;
            detBorderRt.offsetMin = new Vector2(-1, -1);
            detBorderRt.offsetMax = new Vector2(1, 1);
            Image detBorderImg = detailsBorderGo.AddComponent<Image>();
            detBorderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.6f); // Soft gold

            // Details Backdrop
            Image detailsImg = detailsPanel.AddComponent<Image>();
            detailsImg.color = new Color(0.07f, 0.07f, 0.09f, 0.95f); // S ligeramente más oscuro que la ventana principal

            // Details elements: Icon backdrop halo
            GameObject iconHaloGo = new GameObject("IconHalo");
            iconHaloGo.transform.SetParent(detailsPanel.transform, false);
            RectTransform haloRt = iconHaloGo.AddComponent<RectTransform>();
            haloRt.anchorMin = new Vector2(0.5f, 0.8f);
            haloRt.anchorMax = new Vector2(0.5f, 0.8f);
            haloRt.pivot = new Vector2(0.5f, 0.5f);
            haloRt.sizeDelta = new Vector2(110, 110);
            haloRt.anchoredPosition = Vector2.zero;
            Image haloImg = iconHaloGo.AddComponent<Image>();
            haloImg.color = new Color(1f, 0.85f, 0.3f, 0.08f);

            // Icon
            GameObject iconGo = new GameObject("ItemIcon");
            iconGo.transform.SetParent(iconHaloGo.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(10, 10);
            iconRt.offsetMax = new Vector2(-10, -10);
            detailIcon = iconGo.AddComponent<Image>();
            detailIcon.preserveAspect = true;

            // Name
            detailNameText = CreateText(detailsPanel.transform, "ItemNameText", "Select a fish", 22, Color.white, TextAlignmentOptions.Center);
            RectTransform nameRt = detailNameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.58f);
            nameRt.anchorMax = new Vector2(1f, 0.68f);
            nameRt.sizeDelta = Vector2.zero;
            nameRt.anchoredPosition = Vector2.zero;

            // Rarity
            detailRarityText = CreateText(detailsPanel.transform, "ItemRarityText", "", 14, Color.gray, TextAlignmentOptions.Center);
            RectTransform rarityRt = detailRarityText.GetComponent<RectTransform>();
            rarityRt.anchorMin = new Vector2(0f, 0.52f);
            rarityRt.anchorMax = new Vector2(1f, 0.58f);
            rarityRt.sizeDelta = Vector2.zero;
            rarityRt.anchoredPosition = Vector2.zero;

            // Description
            detailDescText = CreateText(detailsPanel.transform, "ItemDescText", "Choose any fish from your inventory to sell it for gold.", 14, new Color(0.95f, 0.95f, 0.95f), TextAlignmentOptions.Center);
            RectTransform descRt = detailDescText.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.08f, 0.32f);
            descRt.anchorMax = new Vector2(0.92f, 0.52f);
            descRt.sizeDelta = Vector2.zero;
            descRt.anchoredPosition = Vector2.zero;

            // Price text
            detailPriceText = CreateText(detailsPanel.transform, "ItemPriceText", "", 18, new Color(1f, 0.85f, 0f), TextAlignmentOptions.Center);
            RectTransform priceRt = detailPriceText.GetComponent<RectTransform>();
            priceRt.anchorMin = new Vector2(0f, 0.22f);
            priceRt.anchorMax = new Vector2(1f, 0.32f);
            priceRt.sizeDelta = Vector2.zero;
            priceRt.anchoredPosition = Vector2.zero;

            // Sell 1 Button
            GameObject sell1Go = new GameObject("Sell1Button");
            sell1Go.transform.SetParent(detailsPanel.transform, false);
            RectTransform sell1Rt = sell1Go.AddComponent<RectTransform>();
            sell1Rt.anchorMin = new Vector2(0.08f, 0.1f);
            sell1Rt.anchorMax = new Vector2(0.48f, 0.2f);
            sell1Rt.sizeDelta = Vector2.zero;
            sell1Rt.anchoredPosition = Vector2.zero;

            Image sell1Img = sell1Go.AddComponent<Image>();
            sell1Img.color = new Color(0.18f, 0.49f, 0.9f); // Blue normal color
            sell1Button = sell1Go.AddComponent<Button>();
            sell1Button.onClick.AddListener(SellOneSelected);
            
            TextMeshProUGUI sell1Text = CreateText(sell1Go.transform, "Text", "Sell 1", 16, Color.white, TextAlignmentOptions.Center);
            RectTransform sell1TextRt = sell1Text.GetComponent<RectTransform>();
            sell1TextRt.anchorMin = Vector2.zero;
            sell1TextRt.anchorMax = Vector2.one;
            sell1TextRt.sizeDelta = Vector2.zero;

            // Sell All Button
            GameObject sellAllGo = new GameObject("SellAllButton");
            sellAllGo.transform.SetParent(detailsPanel.transform, false);
            RectTransform sellAllRt = sellAllGo.AddComponent<RectTransform>();
            sellAllRt.anchorMin = new Vector2(0.52f, 0.1f);
            sellAllRt.anchorMax = new Vector2(0.92f, 0.2f);
            sellAllRt.sizeDelta = Vector2.zero;
            sellAllRt.anchoredPosition = Vector2.zero;

            Image sellAllImg = sellAllGo.AddComponent<Image>();
            sellAllImg.color = new Color(0.85f, 0.65f, 0.15f); // Gold normal color
            sellAllButton = sellAllGo.AddComponent<Button>();
            sellAllButton.onClick.AddListener(SellAllSelected);
            
            TextMeshProUGUI sellAllText = CreateText(sellAllGo.transform, "Text", "Sell All", 16, Color.white, TextAlignmentOptions.Center);
            RectTransform sellAllTextRt = sellAllText.GetComponent<RectTransform>();
            sellAllTextRt.anchorMin = Vector2.zero;
            sellAllTextRt.anchorMax = Vector2.one;
            sellAllTextRt.sizeDelta = Vector2.zero;

            // Add navigation transition colors to buttons for dynamic feedback
            SetupButtonColors(sell1Button, new Color(0.18f, 0.49f, 0.9f));
            SetupButtonColors(sellAllButton, new Color(0.85f, 0.65f, 0.15f));
            SetupButtonColors(closeBtn, new Color(0.75f, 0.15f, 0.15f));

            // Generate slots
            int totalSlots = catalogFishItems.Count;
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotGo = new GameObject($"ShopSlot_{i}");
                slotGo.transform.SetParent(gridParent, false);

                Image bgImg = slotGo.AddComponent<Image>();
                bgImg.sprite = slotBgSprite;
                bgImg.color = Color.white;

                SellFishSlotUI slotUI = slotGo.AddComponent<SellFishSlotUI>();

                // Icon
                GameObject slotIconGo = new GameObject("Icon");
                slotIconGo.transform.SetParent(slotGo.transform, false);
                RectTransform slotIconRt = slotIconGo.AddComponent<RectTransform>();
                slotIconRt.anchorMin = Vector2.zero;
                slotIconRt.anchorMax = Vector2.one;
                slotIconRt.offsetMin = new Vector2(6, 6);
                slotIconRt.offsetMax = new Vector2(-6, -6);
                Image slotIconImg = slotIconGo.AddComponent<Image>();
                slotIconImg.preserveAspect = true;
                slotIconGo.SetActive(false);

                // Quantity Text
                TextMeshProUGUI slotQtyText = CreateText(slotGo.transform, "Quantity", "1", 14, Color.white, TextAlignmentOptions.BottomRight);
                RectTransform slotQtyRt = slotQtyText.GetComponent<RectTransform>();
                slotQtyRt.anchorMin = Vector2.zero;
                slotQtyRt.anchorMax = Vector2.one;
                slotQtyRt.offsetMin = Vector2.zero;
                slotQtyRt.offsetMax = new Vector2(-6, 2);
                slotQtyText.fontStyle = FontStyles.Bold;
                slotQtyText.gameObject.SetActive(false);

                // Price Text
                TextMeshProUGUI slotPriceText = CreateText(slotGo.transform, "Price", "0g", 12, new Color(1f, 0.85f, 0f), TextAlignmentOptions.BottomLeft);
                RectTransform slotPriceRt = slotPriceText.GetComponent<RectTransform>();
                slotPriceRt.anchorMin = Vector2.zero;
                slotPriceRt.anchorMax = Vector2.one;
                slotPriceRt.offsetMin = new Vector2(6, 2);
                slotPriceRt.offsetMax = Vector2.zero;
                slotPriceText.fontStyle = FontStyles.Bold;
                slotPriceText.gameObject.SetActive(false);

                // Selection Glow
                GameObject glowGo = new GameObject("SelectionGlow");
                glowGo.transform.SetParent(slotGo.transform, false);
                RectTransform glowRt = glowGo.AddComponent<RectTransform>();
                glowRt.anchorMin = Vector2.zero;
                glowRt.anchorMax = Vector2.one;
                glowRt.offsetMin = new Vector2(-4, -4);
                glowRt.offsetMax = new Vector2(4, 4);
                Image glowImg = glowGo.AddComponent<Image>();
                glowImg.color = new Color(1f, 0.85f, 0f, 0.65f); // Neon Gold Selection
                glowGo.SetActive(false);

                // Wire slot UI fields using reflection
                slotUI.GetType().GetField("slotBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, bgImg);
                slotUI.GetType().GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, slotIconImg);
                slotUI.GetType().GetField("quantityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, slotQtyText);
                slotUI.GetType().GetField("selectionGlow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, glowImg);
                slotUI.GetType().GetField("priceText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, slotPriceText);

                slotUI.Initialize(i, this);
                slotUIList.Add(slotUI);
            }
        }

        private void SetupButtonColors(Button btn, Color normalColor)
        {
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = Color.Lerp(normalColor, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(normalColor, Color.black, 0.25f);
            cb.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            btn.colors = cb;
        }

        private void ShowPrompt()
        {
            if (promptRoot != null) promptRoot.SetActive(true);
        }

        private void HidePrompt()
        {
            if (promptRoot != null) promptRoot.SetActive(false);
        }

        private void RefreshShopUI()
        {
            if (!isOpen || playerInventory == null) return;

            // Bind values to Slot UI instances from the catalog
            for (int i = 0; i < slotUIList.Count; i++)
            {
                if (i < catalogFishItems.Count)
                {
                    ItemData fishItem = catalogFishItems[i];
                    int qty = GetOwnedQuantityOfFish(fishItem.id);
                    slotUIList[i].gameObject.SetActive(true);
                    slotUIList[i].SetItem(fishItem, qty);
                }
                else
                {
                    slotUIList[i].gameObject.SetActive(false);
                    slotUIList[i].ClearSlot();
                }
            }

            // If selected slot is now empty or inactive, deselect
            if (selectedSlotUI != null && (!selectedSlotUI.gameObject.activeSelf || selectedSlotUI.CurrentItemData == null))
            {
                DeselectAll();
            }
            else if (selectedSlotUI != null)
            {
                // Refresh detail info of selection
                SelectSlot(selectedSlotUI);
            }
            else
            {
                UpdateDetailsPanel(null);
            }
        }

        private void UpdateGoldDisplay(int currentGold)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: <b>{currentGold}</b>g";
            }
        }

        private bool IsFish(ItemData item)
        {
            if (item == null) return false;
            // Fish items start with fish_ prefix as per dynamic creation in PlayerController25D
            return item.id.StartsWith("fish_") || item.itemName.ToLower().Contains("fish") || item.description.ToLower().Contains("caught");
        }

        public void SelectSlot(SellFishSlotUI clickedSlot)
        {
            if (clickedSlot == null || clickedSlot.CurrentItemData == null)
            {
                DeselectAll();
                return;
            }

            if (selectedSlotUI != null)
            {
                selectedSlotUI.SetSelected(false);
            }

            selectedSlotUI = clickedSlot;
            selectedSlotUI.SetSelected(true);

            UpdateDetailsPanel(selectedSlotUI.CurrentItemData);
        }

        public void DeselectAll()
        {
            if (selectedSlotUI != null)
            {
                selectedSlotUI.SetSelected(false);
                selectedSlotUI = null;
            }
            UpdateDetailsPanel(null);
        }

        private void UpdateDetailsPanel(ItemData item)
        {
            if (item == null)
            {
                detailIcon.gameObject.SetActive(false);
                detailNameText.text = "Select a fish";
                detailNameText.color = Color.white;
                detailRarityText.text = "";
                detailDescText.text = "Choose any fish from your inventory to sell it for gold.";
                detailPriceText.text = "";
                sell1Button.interactable = false;
                sellAllButton.interactable = false;
                return;
            }

            detailIcon.sprite = item.icon;
            detailIcon.gameObject.SetActive(item.icon != null);

            detailNameText.text = item.itemName;
            Color rarityColor = GetRarityColor(item.rarity);
            detailNameText.color = rarityColor;

            detailRarityText.text = $"{item.rarity.ToString().ToUpper()} FISH";
            detailRarityText.color = rarityColor;

            detailDescText.text = item.description + $"\n\n<color=#32CD32>Luck: {item.luckScore} ♣</color>";
            detailPriceText.text = $"Price: <color=#FFD700>{item.sellPrice}</color> Gold";

            int quantityOwned = GetOwnedQuantityOfFish(item.id);
            if (quantityOwned > 0)
            {
                sell1Button.interactable = true;
                sellAllButton.interactable = true;
            }
            else
            {
                sell1Button.interactable = false;
                sellAllButton.interactable = false;
            }
        }

        private void SellOneSelected()
        {
            if (selectedSlotUI == null || selectedSlotUI.CurrentItemData == null || playerInventory == null) return;

            ItemData fish = selectedSlotUI.CurrentItemData;
            int quantityOwned = GetOwnedQuantityOfFish(fish.id);
            if (quantityOwned <= 0) return;

            RemoveFishFromInventory(fish.id, 1);
            playerInventory.AddGold(fish.sellPrice);

            PlaySellSound();
            RefreshShopUI();
        }

        private void SellAllSelected()
        {
            if (selectedSlotUI == null || selectedSlotUI.CurrentItemData == null || playerInventory == null) return;

            ItemData fish = selectedSlotUI.CurrentItemData;
            int quantityOwned = GetOwnedQuantityOfFish(fish.id);
            if (quantityOwned <= 0) return;

            RemoveFishFromInventory(fish.id, quantityOwned);
            playerInventory.AddGold(fish.sellPrice * quantityOwned);

            PlaySellSound();
            RefreshShopUI();
        }

        private void PlaySellSound()
        {
            // Try to play coin sound or system beep
            if (playerMC != null)
            {
                AudioSource source = playerMC.GetComponent<AudioSource>();
                if (source != null && source.clip != null)
                {
                    // Fallback to playing character audio source at higher pitch
                    source.PlayOneShot(source.clip, 0.5f);
                }
            }
        }

        private Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return new Color(0.2f, 0.6f, 1.0f);
                case Rarity.Epic: return new Color(0.7f, 0.3f, 0.9f);
                case Rarity.Legendary: return new Color(1.0f, 0.6f, 0.0f);
                default: return new Color(0.95f, 0.95f, 0.95f);
            }
        }

        private static TMP_FontAsset GetDefaultFontAsset()
        {
            // Default TMPro fallback
            return TMP_Settings.defaultFontAsset;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.font = GetDefaultFontAsset();

            return tmp;
        }

        private void InitializeCatalog()
        {
            catalogFishItems.Clear();

            // Load all fish sprites from Resources folder (works in both Editor and builds)
            Sprite[] fishSprites = Resources.LoadAll<Sprite>("Sprites/Fishes");
            foreach (Sprite sprite in fishSprites)
            {
                string[] files = System.IO.Directory.GetFiles(folderPath, "fish_fishing-*.png");
                foreach (string filePath in files)
                {
                    string filename = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    string rawName = filename.Replace("fish_fishing-", "");
                    string fishName = FormatFishName(rawName);
                    
                    // Relative path for AssetDatabase
                    string relativePath = "Assets/Model/Fishes/" + System.IO.Path.GetFileName(filePath);
                    Sprite sprite = null;
#if UNITY_EDITOR
                    sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
#endif
                    
                    // Create ItemData
                    ItemData fishItem = ScriptableObject.CreateInstance<ItemData>();
                    fishItem.id = "fish_" + fishName.Replace(" ", "_").ToLower();
                    fishItem.itemName = fishName;
                    fishItem.description = $"A fresh caught {fishName}. Can be used for cooking or trading.";
                    fishItem.type = ItemType.Material;
                    
                    // Determine rarity & price
                    if (fishName.Contains("Shark") || fishName.Contains("Ray") || fishName.Contains("Dinosaur"))
                    {
                        fishItem.rarity = Rarity.Legendary;
                        fishItem.sellPrice = 500;
                    }
                    else if (fishName.Contains("Salmon") || fishName.Contains("Trout") || fishName.Contains("Eel") || fishName.Contains("Pike"))
                    {
                        fishItem.rarity = Rarity.Epic;
                        fishItem.sellPrice = 150;
                    }
                    else if (fishName.Contains("Bass") || fishName.Contains("Gar") || fishName.Contains("Porgy") || fishName.Contains("Snapper") || fishName.Contains("Perch"))
                    {
                        fishItem.rarity = Rarity.Rare;
                        fishItem.sellPrice = 50;
                    }
                    else
                    {
                        fishItem.rarity = Rarity.Common;
                        fishItem.sellPrice = 15;
                    }

                fishItem.icon = sprite;
                catalogFishItems.Add(fishItem);
            }

            // Sort by sell price ascending (and alphabetically by name if prices are equal)
            catalogFishItems.Sort((a, b) => {
                int priceCompare = a.sellPrice.CompareTo(b.sellPrice);
                if (priceCompare != 0) return priceCompare;
                return string.Compare(a.itemName, b.itemName, System.StringComparison.OrdinalIgnoreCase);
            });

            if (catalogFishItems.Count == 0)
            {
                Debug.LogWarning("SellFishStoreUI: No fish sprites found in Resources/Sprites/Fishes/. Make sure fish PNGs are in the Resources folder.");
            }
        }

        private string FormatFishName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "";
            
            string[] words = rawName.Split(new char[] { '-', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            string formatted = string.Join(" ", words);

            if (formatted.Equals("Bigmouthbass", System.StringComparison.OrdinalIgnoreCase)) return "Bigmouth Bass";
            if (formatted.Equals("Blackspottedeel", System.StringComparison.OrdinalIgnoreCase)) return "Black Spotted Eel";
            if (formatted.Equals("Brooktrout", System.StringComparison.OrdinalIgnoreCase)) return "Brook Trout";
            if (formatted.Equals("Brownray", System.StringComparison.OrdinalIgnoreCase)) return "Brown Ray";
            if (formatted.Equals("Kingsalmon", System.StringComparison.OrdinalIgnoreCase)) return "King Salmon";
            if (formatted.Equals("Longnosegar", System.StringComparison.OrdinalIgnoreCase)) return "Longnose Gar";
            if (formatted.Equals("Northernpike", System.StringComparison.OrdinalIgnoreCase)) return "Northern Pike";
            if (formatted.Equals("Pinksalmon", System.StringComparison.OrdinalIgnoreCase)) return "Pink Salmon";
            if (formatted.Equals("Pufferfish", System.StringComparison.OrdinalIgnoreCase)) return "Puffer Fish";
            if (formatted.Equals("Rainbowtrout", System.StringComparison.OrdinalIgnoreCase)) return "Rainbow Trout";
            if (formatted.Equals("Redlionfish", System.StringComparison.OrdinalIgnoreCase)) return "Red Lionfish";
            if (formatted.Equals("Redporgy", System.StringComparison.OrdinalIgnoreCase)) return "Red Porgy";
            if (formatted.Equals("Redsnapper", System.StringComparison.OrdinalIgnoreCase)) return "Red Snapper";
            if (formatted.Equals("Sandbarshark", System.StringComparison.OrdinalIgnoreCase)) return "Sandbar Shark";
            if (formatted.Equals("Sharptoothcatfish", System.StringComparison.OrdinalIgnoreCase)) return "Sharptooth Catfish";
            if (formatted.Equals("Sockeyesalmon", System.StringComparison.OrdinalIgnoreCase)) return "Sockeye Salmon";
            if (formatted.Equals("Spadefish", System.StringComparison.OrdinalIgnoreCase)) return "Spade Fish";
            if (formatted.Equals("Spotcroacker", System.StringComparison.OrdinalIgnoreCase)) return "Spot Croacker";
            if (formatted.Equals("Yellowperch", System.StringComparison.OrdinalIgnoreCase)) return "Yellow Perch";

            return formatted;
        }

        private int GetOwnedQuantityOfFish(string fishId)
        {
            if (playerInventory == null) return 0;
            int total = 0;
            foreach (var slot in playerInventory.Slots)
            {
                if (!slot.IsEmpty && slot.itemData.id == fishId)
                {
                    total += slot.quantity;
                }
            }
            return total;
        }

        private void RemoveFishFromInventory(string fishId, int amount)
        {
            if (playerInventory == null) return;
            int remainingToRemove = amount;

            for (int i = 0; i < playerInventory.Slots.Count; i++)
            {
                var slot = playerInventory.Slots[i];
                if (!slot.IsEmpty && slot.itemData.id == fishId)
                {
                    int toRemove = Mathf.Min(remainingToRemove, slot.quantity);
                    playerInventory.RemoveItem(i, toRemove);
                    remainingToRemove -= toRemove;

                    if (remainingToRemove <= 0)
                        break;
                }
            }
        }
    }
}
