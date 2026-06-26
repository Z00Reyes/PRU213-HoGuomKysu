using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class BuyFishingRodStoreUI : MonoBehaviour
    {
        public static BuyFishingRodStoreUI Instance { get; private set; }

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 4.0f;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 3.2f, 0f);

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
        private TextMeshProUGUI detailDescText;
        private TextMeshProUGUI detailPriceText;
        private Image detailIcon;
        private Button actionButton; // Dynamic button for Buy / Equip / Equipped
        private TextMeshProUGUI actionButtonText;

        private Sprite slotBgSprite;
        private bool isOpen = false;
        private bool isPlayerInRange = false;

        private List<BuyFishingRodSlotUI> slotUIList = new List<BuyFishingRodSlotUI>();
        private BuyFishingRodSlotUI selectedSlotUI;

        private List<ShopItem> shopItems = new List<ShopItem>();

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
            // Find the fishing rod shop fbx model in the scene
            shopStall = GameObject.Find("tripo_convert_0262afa5-ad99-4da5-af23-b85a7b2158b3");

            if (playerMC != null)
            {
                playerInventory = playerMC.GetComponent<Inventory>();
            }

            if (playerInventory != null)
            {
                playerInventory.onGoldChanged += UpdateGoldDisplay;
            }

            // Load Slot Sprite
#if UNITY_EDITOR
            slotBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ItemSlots/ItemSlots/Item_Slot-6.png");
#endif
            if (slotBgSprite == null)
            {
                Debug.LogWarning("BuyFishingRodStoreUI: Could not load Item_Slot-6.png sprite, using default.");
            }

            // Define items in the shop
            InitializeShopItems();

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
                playerInventory.onGoldChanged -= UpdateGoldDisplay;
            }
        }

        private void InitializeShopItems()
        {
            shopItems.Clear();

            // 6 Rods
            AddShopItem("fishing_rod_bamboo", "Bamboo Rod", "A basic bamboo rod. Lightweight and flexible.", 100, false, 3);
            AddShopItem("fishing_rod_fiberglass", "Fiberglass Rod", "A sturdy fiberglass rod. Durable and reliable.", 300, false, 5);
            AddShopItem("fishing_rod_carbon", "Carbon Rod", "A high-performance carbon rod. Extremely sensitive.", 600, false, 7);
            AddShopItem("fishing_rod_master", "Master Rod", "A legendary rod used by master anglers.", 1200, false, 10);
            AddShopItem("fishing_rod_golden", "Golden Rod", "Crafted from pure gold. Shines brilliantly.", 2500, false, 15);
            AddShopItem("fishing_rod_lava", "Lava Rod", "Forged in volcanic depths with hot magma energy.", 5000, false, 20);

            // 6 Bobbers
            AddShopItem("fish_bobber_standard", "Standard Bobber", "A standard red-and-white plastic bobber.", 50, true, 0);
            AddShopItem("fish_bobber_bluecork", "Blue Cork Bobber", "A blue-painted cork bobber. Floats very well.", 150, true, 1);
            AddShopItem("fish_bobber_clover", "Clover Bobber", "A lucky four-leaf clover bobber. Increases luck!", 300, true, 2);
            AddShopItem("fish_bobber_donut", "Donut Bobber", "A sweet pink glazed donut bobber. Watch out for fish!", 500, true, 3);
            AddShopItem("fish_bobber_rainbow", "Rainbow Bobber", "A colorful rainbow bobber. Makes beautiful reflections.", 1000, true, 4);
            AddShopItem("fish_bobber_crystal", "Crystal Bobber", "A glowing crystal bobber. Illuminates dark water.", 2000, true, 5);
        }

        private void AddShopItem(string id, string name, string desc, int price, bool isBobber, int luckLevel = 0)
        {
            ShopItem item = new ShopItem
            {
                id = id,
                itemName = name,
                description = desc,
                price = price,
                isBobber = isBobber,
                icon = GetItemSprite(id, isBobber),
                luckLevel = luckLevel
            };
            shopItems.Add(item);
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

            promptRoot = new GameObject("FishingShopPrompt");
            promptRoot.transform.SetParent(mainCanvas.transform, false);

            promptRt = promptRoot.AddComponent<RectTransform>();
            promptRt.sizeDelta = new Vector2(170, 50);

            // Background
            Image bg = promptRoot.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.14f, 0.9f); // Slate dark
            
            // Border
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
            tmpText.text = "<color=#FFD700>[E]</color> Fishing Shop";
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

            // Root panel
            uiPanelRoot = new GameObject("BuyFishingRodUI");
            uiPanelRoot.transform.SetParent(mainCanvas.transform, false);
            RectTransform rootRt = uiPanelRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            panelCanvasGroup = uiPanelRoot.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;

            // Overlay Bg
            GameObject overlayGo = new GameObject("OverlayBg");
            overlayGo.transform.SetParent(uiPanelRoot.transform, false);
            RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            Image overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.65f);

            Button overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(CloseShop);

            // Shop Window
            GameObject windowGo = new GameObject("ShopWindow");
            windowGo.transform.SetParent(uiPanelRoot.transform, false);
            RectTransform windowRt = windowGo.AddComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(900, 560);
            windowRt.anchoredPosition = Vector2.zero;

            // Window Border
            GameObject borderGo = new GameObject("WindowBorder");
            borderGo.transform.SetParent(windowGo.transform, false);
            RectTransform borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2);
            borderRt.offsetMax = new Vector2(2, 2);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.95f); // Gold

            // Window Backdrop
            Image windowImg = windowGo.AddComponent<Image>();
            windowImg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);

            // Header Section
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(windowGo.transform, false);
            RectTransform headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 0.86f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.sizeDelta = Vector2.zero;
            headerRt.anchoredPosition = Vector2.zero;

            // Title
            TextMeshProUGUI titleText = CreateText(headerGo.transform, "TitleText", "FISHING MATE", 28, new Color(1f, 0.85f, 0f));
            RectTransform titleRt = titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(30, 0);
            titleRt.sizeDelta = new Vector2(300, 50);

            // Gold count
            goldText = CreateText(headerGo.transform, "GoldText", "Gold: 0g", 20, new Color(1f, 0.85f, 0f), TextAlignmentOptions.Right);
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

            // Body Section
            GameObject bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(windowGo.transform, false);
            RectTransform bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = new Vector2(1f, 0.86f);
            bodyRt.offsetMin = new Vector2(30, 30);
            bodyRt.offsetMax = new Vector2(-30, 0);

            // LEFT SIDE: Grid container (60% width)
            GameObject gridContainerGo = new GameObject("GridContainer");
            gridContainerGo.transform.SetParent(bodyGo.transform, false);
            RectTransform gridContainerRt = gridContainerGo.AddComponent<RectTransform>();
            gridContainerRt.anchorMin = Vector2.zero;
            gridContainerRt.anchorMax = new Vector2(0.6f, 1f);
            gridContainerRt.offsetMin = Vector2.zero;
            gridContainerRt.offsetMax = new Vector2(-15, 0);

            Image gridBackplate = gridContainerGo.AddComponent<Image>();
            gridBackplate.color = new Color(0.06f, 0.06f, 0.08f, 0.85f);

            gridParent = gridContainerGo.transform;
            GridLayoutGroup gridLayout = gridContainerGo.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(18, 18, 18, 18);
            gridLayout.cellSize = new Vector2(74, 74);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            // RIGHT SIDE: Details panel (40% width)
            detailsPanel = new GameObject("DetailsPanel");
            detailsPanel.transform.SetParent(bodyGo.transform, false);
            RectTransform detailsRt = detailsPanel.AddComponent<RectTransform>();
            detailsRt.anchorMin = new Vector2(0.6f, 0f);
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
            detBorderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.6f);

            Image detailsImg = detailsPanel.AddComponent<Image>();
            detailsImg.color = new Color(0.07f, 0.07f, 0.09f, 0.95f);

            // Icon Halo
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
            detailNameText = CreateText(detailsPanel.transform, "ItemNameText", "Select an item", 22, Color.white, TextAlignmentOptions.Center);
            RectTransform nameRt = detailNameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.58f);
            nameRt.anchorMax = new Vector2(1f, 0.68f);
            nameRt.sizeDelta = Vector2.zero;
            nameRt.anchoredPosition = Vector2.zero;

            // Description
            detailDescText = CreateText(detailsPanel.transform, "ItemDescText", "Upgrade your gear to catch bigger and rarer fish!", 14, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center);
            RectTransform descRt = detailDescText.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.08f, 0.32f);
            descRt.anchorMax = new Vector2(0.92f, 0.55f);
            descRt.sizeDelta = Vector2.zero;
            descRt.anchoredPosition = Vector2.zero;

            // Price/Status
            detailPriceText = CreateText(detailsPanel.transform, "ItemPriceText", "", 18, new Color(1f, 0.85f, 0f), TextAlignmentOptions.Center);
            RectTransform priceRt = detailPriceText.GetComponent<RectTransform>();
            priceRt.anchorMin = new Vector2(0f, 0.22f);
            priceRt.anchorMax = new Vector2(1f, 0.32f);
            priceRt.sizeDelta = Vector2.zero;
            priceRt.anchoredPosition = Vector2.zero;

            // Action Button
            GameObject actionBtnGo = new GameObject("ActionButton");
            actionBtnGo.transform.SetParent(detailsPanel.transform, false);
            RectTransform actionBtnRt = actionBtnGo.AddComponent<RectTransform>();
            actionBtnRt.anchorMin = new Vector2(0.1f, 0.1f);
            actionBtnRt.anchorMax = new Vector2(0.9f, 0.2f);
            actionBtnRt.sizeDelta = Vector2.zero;
            actionBtnRt.anchoredPosition = Vector2.zero;

            Image actionBtnImg = actionBtnGo.AddComponent<Image>();
            actionBtnImg.color = new Color(0.85f, 0.65f, 0.15f);
            actionButton = actionBtnGo.AddComponent<Button>();
            actionButton.onClick.AddListener(OnActionButtonClicked);

            actionButtonText = CreateText(actionBtnGo.transform, "Text", "Buy", 16, Color.white, TextAlignmentOptions.Center);
            RectTransform actionTextRt = actionButtonText.GetComponent<RectTransform>();
            actionTextRt.anchorMin = Vector2.zero;
            actionTextRt.anchorMax = Vector2.one;
            actionTextRt.sizeDelta = Vector2.zero;

            SetupButtonColors(actionButton, new Color(0.85f, 0.65f, 0.15f));
            SetupButtonColors(closeBtn, new Color(0.75f, 0.15f, 0.15f));

            // Generate Slots
            for (int i = 0; i < 12; i++)
            {
                GameObject slotGo = new GameObject($"ShopSlot_{i}");
                slotGo.transform.SetParent(gridParent, false);

                Image bgImg = slotGo.AddComponent<Image>();
                bgImg.sprite = slotBgSprite;
                bgImg.color = Color.white;

                BuyFishingRodSlotUI slotUI = slotGo.AddComponent<BuyFishingRodSlotUI>();

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

                // Selection Glow
                GameObject glowGo = new GameObject("SelectionGlow");
                glowGo.transform.SetParent(slotGo.transform, false);
                RectTransform glowRt = glowGo.AddComponent<RectTransform>();
                glowRt.anchorMin = Vector2.zero;
                glowRt.anchorMax = Vector2.one;
                glowRt.offsetMin = new Vector2(-4, -4);
                glowRt.offsetMax = new Vector2(4, 4);
                Image glowImg = glowGo.AddComponent<Image>();
                glowImg.color = new Color(1f, 0.85f, 0f, 0.65f);
                glowGo.SetActive(false);

                // Equipped Badge (E badge in bottom-right)
                GameObject equippedBadgeGo = new GameObject("EquippedBadge");
                equippedBadgeGo.transform.SetParent(slotGo.transform, false);
                RectTransform eqRt = equippedBadgeGo.AddComponent<RectTransform>();
                eqRt.anchorMin = new Vector2(0.6f, 0f);
                eqRt.anchorMax = new Vector2(1f, 0.4f);
                eqRt.offsetMin = new Vector2(-2, 2);
                eqRt.offsetMax = new Vector2(-2, 2);
                Image eqImg = equippedBadgeGo.AddComponent<Image>();
                eqImg.color = new Color(0.18f, 0.75f, 0.3f); // Green
                TextMeshProUGUI eqText = CreateText(equippedBadgeGo.transform, "Text", "E", 10, Color.white, TextAlignmentOptions.Center);
                eqText.fontStyle = FontStyles.Bold;
                RectTransform eqTextRt = eqText.GetComponent<RectTransform>();
                eqTextRt.anchorMin = Vector2.zero;
                eqTextRt.anchorMax = Vector2.one;
                eqTextRt.sizeDelta = Vector2.zero;
                equippedBadgeGo.SetActive(false);

                // Owned Badge (Checkmark or "O" badge in bottom-right)
                GameObject ownedBadgeGo = new GameObject("OwnedBadge");
                ownedBadgeGo.transform.SetParent(slotGo.transform, false);
                RectTransform owRt = ownedBadgeGo.AddComponent<RectTransform>();
                owRt.anchorMin = new Vector2(0.6f, 0f);
                owRt.anchorMax = new Vector2(1f, 0.4f);
                owRt.offsetMin = new Vector2(-2, 2);
                owRt.offsetMax = new Vector2(-2, 2);
                Image owImg = ownedBadgeGo.AddComponent<Image>();
                owImg.color = new Color(0.2f, 0.6f, 0.9f); // Blue
                TextMeshProUGUI owText = CreateText(ownedBadgeGo.transform, "Text", "✓", 11, Color.white, TextAlignmentOptions.Center);
                owText.fontStyle = FontStyles.Bold;
                RectTransform owTextRt = owText.GetComponent<RectTransform>();
                owTextRt.anchorMin = Vector2.zero;
                owTextRt.anchorMax = Vector2.one;
                owTextRt.sizeDelta = Vector2.zero;
                ownedBadgeGo.SetActive(false);

                // Wire slot UI fields using reflection
                slotUI.GetType().GetField("slotBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, bgImg);
                slotUI.GetType().GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, slotIconImg);
                slotUI.GetType().GetField("selectionGlow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, glowImg);
                slotUI.GetType().GetField("equippedBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, equippedBadgeGo);
                slotUI.GetType().GetField("ownedBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, ownedBadgeGo);

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

            // Populate slots
            for (int i = 0; i < slotUIList.Count; i++)
            {
                if (i < shopItems.Count)
                {
                    slotUIList[i].gameObject.SetActive(true);
                    ShopItem item = shopItems[i];
                    bool isOwned = playerInventory.IsItemPurchased(item.id);
                    bool isEquipped = item.isBobber ? (playerInventory.equippedBobberId == item.id) : (playerInventory.equippedRodId == item.id);
                    slotUIList[i].SetItem(item, isOwned, isEquipped);
                }
                else
                {
                    slotUIList[i].gameObject.SetActive(false);
                    slotUIList[i].ClearSlot();
                }
            }

            // Refresh details of selected slot
            if (selectedSlotUI != null && selectedSlotUI.gameObject.activeSelf && selectedSlotUI.CurrentItem != null)
            {
                SelectSlot(selectedSlotUI);
            }
            else
            {
                DeselectAll();
            }
        }

        private void UpdateGoldDisplay(int currentGold)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: <color=#FFD700>{currentGold}</color>g";
            }
        }

        public void SelectSlot(BuyFishingRodSlotUI clickedSlot)
        {
            if (clickedSlot == null || clickedSlot.CurrentItem == null)
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

            UpdateDetailsPanel(selectedSlotUI.CurrentItem);
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

        private void UpdateDetailsPanel(ShopItem item)
        {
            if (item == null || playerInventory == null)
            {
                detailIcon.gameObject.SetActive(false);
                detailNameText.text = "Select an item";
                detailDescText.text = "Upgrade your gear to catch bigger and rarer fish!";
                detailPriceText.text = "";
                actionButton.interactable = false;
                actionButtonText.text = "Select Item";
                return;
            }

            detailIcon.sprite = item.icon;
            detailIcon.gameObject.SetActive(item.icon != null);

            detailNameText.text = item.itemName;
            
            string extraInfo = "";
            if (!item.isBobber) extraInfo = $"\n\n<color=#32CD32>Max Luck: {item.luckLevel} ♣</color>";
            else extraInfo = $"\n\n<color=#32CD32>Bonus Luck: +{item.luckLevel} ♣</color>";
            
            detailDescText.text = item.description + extraInfo;

            bool isOwned = playerInventory.IsItemPurchased(item.id);
            bool isEquipped = item.isBobber ? (playerInventory.equippedBobberId == item.id) : (playerInventory.equippedRodId == item.id);

            if (isEquipped)
            {
                detailPriceText.text = "<color=#32CD32>EQUIPPED</color>";
                actionButton.interactable = false;
                actionButtonText.text = "Equipped";
                SetupButtonColors(actionButton, new Color(0.2f, 0.6f, 0.2f, 0.6f));
            }
            else if (isOwned)
            {
                detailPriceText.text = "<color=#1E90FF>OWNED</color>";
                actionButton.interactable = true;
                actionButtonText.text = "Equip";
                SetupButtonColors(actionButton, new Color(0.18f, 0.49f, 0.9f));
            }
            else
            {
                detailPriceText.text = $"Price: <color=#FFD700>{item.price}</color> Gold";
                actionButtonText.text = $"Buy ({item.price}g)";

                if (playerInventory.Gold >= item.price)
                {
                    actionButton.interactable = true;
                    SetupButtonColors(actionButton, new Color(0.85f, 0.65f, 0.15f));
                }
                else
                {
                    actionButton.interactable = false;
                    SetupButtonColors(actionButton, new Color(0.35f, 0.35f, 0.35f));
                }
            }
        }

        private void OnActionButtonClicked()
        {
            if (selectedSlotUI == null || selectedSlotUI.CurrentItem == null || playerInventory == null) return;

            ShopItem item = selectedSlotUI.CurrentItem;
            bool isOwned = playerInventory.IsItemPurchased(item.id);

            if (isOwned)
            {
                // Equip item
                if (item.isBobber)
                {
                    playerInventory.equippedBobberId = item.id;
                }
                else
                {
                    playerInventory.equippedRodId = item.id;
                }
                PlayEquipSound();
            }
            else
            {
                // Buy item
                if (playerInventory.Gold >= item.price)
                {
                    playerInventory.RemoveGold(item.price);
                    playerInventory.PurchaseItem(item.id);
                    PlayBuySound();
                }
            }

            RefreshShopUI();
        }

        private void PlayBuySound()
        {
            if (playerMC != null)
            {
                AudioSource source = playerMC.GetComponent<AudioSource>();
                if (source != null && source.clip != null)
                {
                    source.PlayOneShot(source.clip, 0.6f);
                }
            }
        }

        private void PlayEquipSound()
        {
            if (playerMC != null)
            {
                AudioSource source = playerMC.GetComponent<AudioSource>();
                if (source != null && source.clip != null)
                {
                    source.PlayOneShot(source.clip, 0.4f);
                }
            }
        }

        private Sprite GetItemSprite(string id, bool isBobber)
        {
#if UNITY_EDITOR
            string path = "";
            if (isBobber)
            {
                switch (id)
                {
                    case "fish_bobber_standard": path = "Assets/Model/Fishes/fish_bobber-standard.png"; break;
                    case "fish_bobber_bluecork": path = "Assets/Model/Fishes/fish_bobber-bluecork.png"; break;
                    case "fish_bobber_clover": path = "Assets/Model/Fishes/fish_bobber-clover.png"; break;
                    case "fish_bobber_donut": path = "Assets/Model/Fishes/fish_bobber-donut.png"; break;
                    case "fish_bobber_rainbow": path = "Assets/Model/Fishes/fish_bobber-rainbow.png"; break;
                    case "fish_bobber_crystal": path = "Assets/Model/Fishes/fish_bobber-crystal.png"; break;
                }
            }
            else
            {
                switch (id)
                {
                    case "fishing_rod_bamboo": path = "Assets/Model/Fishes/fishing_icons_32x32_6.png"; break;
                    case "fishing_rod_fiberglass": path = "Assets/Model/Fishes/fishing_icons_32x32_7.png"; break;
                    case "fishing_rod_carbon": path = "Assets/Model/Fishes/fishing_icons_32x32_8.png"; break;
                    case "fishing_rod_master": path = "Assets/Model/Fishes/fishing_icons_32x32_18.png"; break;
                    case "fishing_rod_golden": path = "Assets/Model/Fishes/fishing_icons_32x32_19.png"; break;
                    case "fishing_rod_lava": path = "Assets/Model/Fishes/fishing_icons_32x32_20.png"; break;
                }
            }
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        private static TMP_FontAsset GetDefaultFontAsset()
        {
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
    }
}
