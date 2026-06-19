using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace InventorySystem
{
    public class SetupInventoryUI : EditorWindow
    {
        [MenuItem("Tools/Inventory/Create Inventory UI")]
        public static void CreateInventoryUIScene()
        {
            // 1. Locate Player (MC)
            GameObject playerGo = GameObject.Find("MC");
            if (playerGo == null)
            {
                Debug.LogError("SetupInventoryUI: Could not find MC GameObject in the scene!");
                return;
            }

            // Ensure MC has Inventory component
            Inventory inventory = playerGo.GetComponent<Inventory>();
            if (inventory == null)
            {
                inventory = Undo.AddComponent<Inventory>(playerGo);
                Debug.Log("Added Inventory component to MC player.");
            }

            // 2. Find Canvas or Create one
            Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("UICanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                
                // Add EventSystem if missing
                if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventGo = new GameObject("EventSystem");
                    eventGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // Clean up any existing Inventory UI
            Transform existingUI = canvas.transform.Find("InventoryUI");
            if (existingUI != null)
            {
                DestroyImmediate(existingUI.gameObject);
            }

            // Configure CanvasScaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // 3. Create root InventoryUI GameObject
            GameObject inventoryUIGo = new GameObject("InventoryUI");
            inventoryUIGo.transform.SetParent(canvas.transform, false);
            
            RectTransform inventoryUIRt = inventoryUIGo.AddComponent<RectTransform>();
            inventoryUIRt.anchorMin = Vector2.zero;
            inventoryUIRt.anchorMax = Vector2.one;
            inventoryUIRt.sizeDelta = Vector2.zero;
            
            CanvasGroup canvasGroup = inventoryUIGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Hidden by default
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            InventoryUI inventoryUI = inventoryUIGo.AddComponent<InventoryUI>();

            // Load Slot Image
            Sprite slotBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ItemSlots/ItemSlots/Item_Slot-2.png");
            if (slotBgSprite == null)
            {
                Debug.LogWarning("SetupInventoryUI: Could not find Item_Slot-2.png at Assets/ItemSlots/ItemSlots/Item_Slot-2.png");
            }

            // 4. Create Background overlay (dark glassmorphism)
            GameObject overlayGo = new GameObject("OverlayBg");
            overlayGo.transform.SetParent(inventoryUIGo.transform, false);
            RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            Image overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

            // 5. Create Main Panel (Slate & Gold Theme)
            GameObject panelGo = new GameObject("MainPanel");
            panelGo.transform.SetParent(inventoryUIGo.transform, false);
            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(600, 500);
            
            // Gold border outline
            GameObject borderGo = new GameObject("PanelBorder");
            borderGo.transform.SetParent(panelGo.transform, false);
            RectTransform borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2);
            borderRt.offsetMax = new Vector2(2, 2);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.65f, 0.15f, 0.95f); // Gold highlight

            // Panel Background
            Image panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f); // Dark slate grey

            // 6. Header Elements
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(panelGo.transform, false);
            RectTransform headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 0.85f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.sizeDelta = Vector2.zero;
            headerRt.anchoredPosition = Vector2.zero;

            // Title
            TextMeshProUGUI titleText = CreateTMPText(headerGo.transform, "TitleText", "INVENTORY", 28, new Color(1f, 0.85f, 0f));
            RectTransform titleRt = titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(30, 0);
            titleRt.sizeDelta = new Vector2(200, 50);

            // Search Field
            TMP_InputField searchField = CreateTMPInputField(headerGo.transform, "SearchField");
            RectTransform searchRt = searchField.GetComponent<RectTransform>();
            searchRt.anchorMin = new Vector2(0.5f, 0.5f);
            searchRt.anchorMax = new Vector2(0.5f, 0.5f);
            searchRt.pivot = new Vector2(0.5f, 0.5f);
            searchRt.anchoredPosition = new Vector2(0, 0);
            searchRt.sizeDelta = new Vector2(200, 36);
            
            TMP_Text searchPlaceholder = searchField.placeholder as TMP_Text;
            if (searchPlaceholder != null) searchPlaceholder.text = "Search items...";

            // Close Button
            GameObject closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(headerGo.transform, false);
            RectTransform closeBtnRt = closeBtnGo.AddComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRt.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRt.pivot = new Vector2(1f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-30, 0);
            closeBtnRt.sizeDelta = new Vector2(36, 36);
            Image closeBtnImg = closeBtnGo.AddComponent<Image>();
            closeBtnImg.color = new Color(0.75f, 0.15f, 0.15f);
            Button closeButton = closeBtnGo.AddComponent<Button>();

            TextMeshProUGUI closeText = CreateTMPText(closeBtnGo.transform, "CloseText", "X", 16, Color.white, TextAlignmentOptions.Center);
            RectTransform closeTextRt = closeText.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.sizeDelta = Vector2.zero;

            // 7. Grid Container (Centered Content)
            GameObject gridParentGo = new GameObject("GridParent");
            gridParentGo.transform.SetParent(panelGo.transform, false);
            RectTransform gridParentRt = gridParentGo.AddComponent<RectTransform>();
            gridParentRt.anchorMin = new Vector2(0f, 0f);
            gridParentRt.anchorMax = new Vector2(1f, 0.85f);
            gridParentRt.offsetMin = new Vector2(30, 30);
            gridParentRt.offsetMax = new Vector2(-30, 0);

            // Backplate for grid area
            Image gridAreaBg = gridParentGo.AddComponent<Image>();
            gridAreaBg.color = new Color(0.06f, 0.06f, 0.08f, 0.8f); // Soft frame backplate

            // Setup Grid Layout
            GridLayoutGroup gridLayout = gridParentGo.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(16, 16, 16, 16);
            gridLayout.cellSize = new Vector2(70, 70);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // 8. Generate Slot instances inside Grid Parent
            for (int i = 0; i < 24; i++)
            {
                GameObject slotGo = new GameObject($"Slot_{i}");
                slotGo.transform.SetParent(gridParentGo.transform, false);
                RectTransform slotRt = slotGo.AddComponent<RectTransform>();
                
                Image bgImg = slotGo.AddComponent<Image>();
                bgImg.sprite = slotBgSprite;
                bgImg.color = Color.white;

                InventorySlotUI slotUI = slotGo.AddComponent<InventorySlotUI>();

                // Item Icon
                GameObject slotIconGo = new GameObject("Icon");
                slotIconGo.transform.SetParent(slotGo.transform, false);
                RectTransform slotIconRt = slotIconGo.AddComponent<RectTransform>();
                slotIconRt.anchorMin = Vector2.zero;
                slotIconRt.anchorMax = Vector2.one;
                slotIconRt.offsetMin = new Vector2(6, 6);
                slotIconRt.offsetMax = new Vector2(-6, -6);
                Image slotIconImg = slotIconGo.AddComponent<Image>();
                slotIconImg.preserveAspect = true;
                slotIconGo.SetActive(false); // Hidden by default

                // Quantity Text
                TextMeshProUGUI slotQtyText = CreateTMPText(slotGo.transform, "Quantity", "1", 14, Color.white, TextAlignmentOptions.BottomRight);
                RectTransform slotQtyRt = slotQtyText.GetComponent<RectTransform>();
                slotQtyRt.anchorMin = Vector2.zero;
                slotQtyRt.anchorMax = Vector2.one;
                slotQtyRt.offsetMin = new Vector2(0, 0);
                slotQtyRt.offsetMax = new Vector2(-6, 2);
                slotQtyText.fontStyle = FontStyles.Bold;
                slotQtyText.gameObject.SetActive(false); // Hidden by default

                // Selection Glow
                GameObject glowGo = new GameObject("SelectionGlow");
                glowGo.transform.SetParent(slotGo.transform, false);
                RectTransform glowRt = glowGo.AddComponent<RectTransform>();
                glowRt.anchorMin = Vector2.zero;
                glowRt.anchorMax = Vector2.one;
                glowRt.offsetMin = new Vector2(-4, -4);
                glowRt.offsetMax = new Vector2(4, 4);
                Image glowImg = glowGo.AddComponent<Image>();
                glowImg.color = new Color(1f, 0.85f, 0f, 0.6f); // Yellow selection frame glow
                glowGo.SetActive(false); // Hidden by default

                // Wire slot UI fields using reflection
                slotUI.GetType().GetField("slotBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, bgImg);
                slotUI.GetType().GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, slotIconImg);
                slotUI.GetType().GetField("quantityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, slotQtyText);
                slotUI.GetType().GetField("selectionGlow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(slotUI, glowImg);
            }

            // 9. Wire InventoryUI fields
            inventoryUI.GetType().GetField("gridParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, gridParentRt);
            inventoryUI.GetType().GetField("tooltipUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);
            inventoryUI.GetType().GetField("mainPanelGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, canvasGroup);
            inventoryUI.GetType().GetField("searchField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, searchField);
            inventoryUI.GetType().GetField("sortButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);
            inventoryUI.GetType().GetField("sortButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);
            inventoryUI.GetType().GetField("tabAllButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);
            inventoryUI.GetType().GetField("tabWeaponsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);
            inventoryUI.GetType().GetField("tabConsumablesButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);
            inventoryUI.GetType().GetField("tabMaterialsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, null);

            // Connect inventory script references to InventoryUI
            inventoryUI.GetType().GetField("inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, inventory);
            inventoryUI.GetType().GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventoryUI, closeButton);

            // Mark dirty to persist references
            EditorUtility.SetDirty(inventoryUI);
            if (inventory != null)
            {
                EditorUtility.SetDirty(inventory);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            // Register Undo and select the UI
            Undo.RegisterCreatedObjectUndo(inventoryUIGo, "Create Inventory UI");
            Selection.activeGameObject = inventoryUIGo;

            Debug.Log("Successfully created premium Inventory UI with slot bg texture!");
        }

        private static Button CreateTabButton(Transform parent, string name, string text)
        {
            GameObject tabGo = new GameObject(name);
            tabGo.transform.SetParent(parent, false);
            
            RectTransform tabRt = tabGo.AddComponent<RectTransform>();
            tabRt.sizeDelta = new Vector2(140, 36);

            Image img = tabGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f); // Dark Slate Gray by default

            Button btn = tabGo.AddComponent<Button>();
            
            // Text
            TextMeshProUGUI btnText = CreateTMPText(tabGo.transform, "Text", text, 13, Color.white, TextAlignmentOptions.Center);
            RectTransform textRt = btnText.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static TMP_FontAsset GetDefaultFontAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            // Fallback: try loading from Resources
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null)
            {
                // Try finding any asset with LiberationSans in name
                string[] guidsAlt = AssetDatabase.FindAssets("LiberationSans");
                if (guidsAlt != null && guidsAlt.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guidsAlt[0]);
                    font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }
            return font;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
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

        private static TMP_InputField CreateTMPInputField(Transform parent, string name)
        {
            // 1. Root InputField GameObject
            GameObject rootGo = new GameObject(name);
            rootGo.transform.SetParent(parent, false);
            RectTransform rootRt = rootGo.AddComponent<RectTransform>();
            Image bgImage = rootGo.AddComponent<Image>();
            bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.15f, 0.15f, 0.17f, 1f); // Dark input background

            TMP_InputField inputField = rootGo.AddComponent<TMP_InputField>();

            // 2. Text Area (viewport/mask)
            GameObject textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(rootGo.transform, false);
            RectTransform textAreaRt = textAreaGo.AddComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(10, 6);
            textAreaRt.offsetMax = new Vector2(-10, -6);
            textAreaGo.AddComponent<RectMask2D>();

            // 3. Placeholder Text
            GameObject placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(textAreaGo.transform, false);
            RectTransform placeholderRt = placeholderGo.AddComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Search...";
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            placeholderText.font = GetDefaultFontAsset();

            // 4. Input Text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(textAreaGo.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI textComponent = textGo.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.font = GetDefaultFontAsset();

            // Wire input field properties
            inputField.textViewport = textAreaRt;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;

            return inputField;
        }
    }
}
