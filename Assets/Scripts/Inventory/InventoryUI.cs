using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private Transform gridParent;
        [SerializeField] private ItemTooltipUI tooltipUI;
        [SerializeField] private CanvasGroup mainPanelGroup;
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("Prefab References (Fallback)")]
        [SerializeField] private GameObject slotPrefab;

        [Header("Filter & Search Controls")]
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private Button sortButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI sortButtonText;
        [SerializeField] private Button tabAllButton;
        [SerializeField] private Button tabWeaponsButton;
        [SerializeField] private Button tabConsumablesButton;
        [SerializeField] private Button tabMaterialsButton;

        [Header("Aesthetics")]
        [SerializeField] private float fadeTime = 0.2f;

        private List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
        private InventorySlotUI selectedSlotUI;
        private string searchQuery = "";
        private string activeFilter = "ALL"; // ALL, WEAPONS, CONSUMABLES, MATERIALS
        private bool isOpen = false;

        private void Start()
        {
            // Ensure UI starts closed
            isOpen = false;
            if (mainPanelGroup != null)
            {
                mainPanelGroup.alpha = 0f;
                mainPanelGroup.blocksRaycasts = false;
                mainPanelGroup.interactable = false;
            }

            // Subscribe to inventory changes
            if (inventory != null)
            {
                inventory.onInventoryChanged += RefreshUI;
                inventory.onGoldChanged += UpdateGoldDisplay;
                UpdateGoldDisplay(inventory.Gold);
            }

            // Bind filters
            if (tabAllButton != null) tabAllButton.onClick.AddListener(() => SetFilter("ALL"));
            if (tabWeaponsButton != null) tabWeaponsButton.onClick.AddListener(() => SetFilter("WEAPONS"));
            if (tabConsumablesButton != null) tabConsumablesButton.onClick.AddListener(() => SetFilter("CONSUMABLES"));
            if (tabMaterialsButton != null) tabMaterialsButton.onClick.AddListener(() => SetFilter("MATERIALS"));

            // Bind Search, Sort & Close
            if (searchField != null) searchField.onValueChanged.AddListener(OnSearchChanged);
            if (sortButton != null) sortButton.onClick.AddListener(CycleSort);
            if (closeButton != null) closeButton.onClick.AddListener(ToggleInventory);

            // Initialize slot UIs from children if already present
            InitializeSlotUIs();

            // Refresh UI once
            RefreshUI();
            
            // Set active visuals for tabs
            UpdateTabVisuals();
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.onInventoryChanged -= RefreshUI;
                inventory.onGoldChanged -= UpdateGoldDisplay;
            }
        }

        private void UpdateGoldDisplay(int currentGold)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: <color=#FFD700>{currentGold}</color>g";
            }
        }

        private void InitializeSlotUIs()
        {
            slotUIList.Clear();
            
            // First look at children
            InventorySlotUI[] existingSlots = gridParent.GetComponentsInChildren<InventorySlotUI>(true);
            if (existingSlots.Length > 0)
            {
                for (int i = 0; i < existingSlots.Length; i++)
                {
                    existingSlots[i].Initialize(i, this);
                    slotUIList.Add(existingSlots[i]);
                }
            }
            else if (slotPrefab != null && inventory != null)
            {
                // Dynamic generation if empty
                for (int i = 0; i < inventory.SlotCount; i++)
                {
                    GameObject go = Instantiate(slotPrefab, gridParent);
                    InventorySlotUI slotUI = go.GetComponent<InventorySlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.Initialize(i, this);
                        slotUIList.Add(slotUI);
                    }
                }
            }
        }

        public void SelectSlot(InventorySlotUI clickedSlot)
        {
            // If clicking an empty slot, or same slot, deselect
            if (clickedSlot == null || clickedSlot.CurrentSlot == null || clickedSlot.CurrentSlot.IsEmpty)
            {
                DeselectAll();
                return;
            }

            // Deselect previous slot
            if (selectedSlotUI != null)
            {
                selectedSlotUI.SetSelected(false);
            }

            selectedSlotUI = clickedSlot;
            selectedSlotUI.SetSelected(true);

            // Display tooltip
            if (tooltipUI != null)
            {
                tooltipUI.DisplayItem(selectedSlotUI);
            }
        }

        public void DeselectAll()
        {
            if (selectedSlotUI != null)
            {
                selectedSlotUI.SetSelected(false);
                selectedSlotUI = null;
            }
            if (tooltipUI != null)
            {
                tooltipUI.Hide();
            }
        }

        public void RefreshUI()
        {
            if (inventory == null || slotUIList.Count == 0) return;

            // Deselect if active item is modified/cleared
            if (selectedSlotUI != null && (selectedSlotUI.CurrentSlot == null || selectedSlotUI.CurrentSlot.IsEmpty))
            {
                DeselectAll();
            }

            bool hasFilterOrSearch = activeFilter != "ALL" || !string.IsNullOrEmpty(searchQuery);

            if (!hasFilterOrSearch)
            {
                // Simple 1-to-1 match of all inventory slots (including empty slots)
                for (int i = 0; i < slotUIList.Count; i++)
                {
                    if (i < inventory.Slots.Count)
                    {
                        slotUIList[i].gameObject.SetActive(true);
                        slotUIList[i].SetItem(inventory.Slots[i]);
                    }
                    else
                    {
                        slotUIList[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Gather items matching filter and search
                List<InventorySlot> matchingSlots = new List<InventorySlot>();
                for (int i = 0; i < inventory.Slots.Count; i++)
                {
                    InventorySlot slot = inventory.Slots[i];
                    if (slot.IsEmpty) continue;

                    // Apply filter
                    bool matchesFilter = false;
                    switch (activeFilter)
                    {
                        case "ALL":
                            matchesFilter = true;
                            break;
                        case "WEAPONS":
                            matchesFilter = slot.itemData.type == ItemType.Weapon || slot.itemData.type == ItemType.Armor;
                            break;
                        case "CONSUMABLES":
                            matchesFilter = slot.itemData.type == ItemType.Consumable;
                            break;
                        case "MATERIALS":
                            matchesFilter = slot.itemData.type == ItemType.Material;
                            break;
                    }

                    // Apply search query
                    bool matchesSearch = true;
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        matchesSearch = slot.itemData.itemName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    if (matchesFilter && matchesSearch)
                    {
                        matchingSlots.Add(slot);
                    }
                }

                // Show matching slots, hide the rest
                for (int i = 0; i < slotUIList.Count; i++)
                {
                    if (i < matchingSlots.Count)
                    {
                        slotUIList[i].gameObject.SetActive(true);
                        // Pass slot references
                        slotUIList[i].SetItem(matchingSlots[i]);
                    }
                    else
                    {
                        slotUIList[i].gameObject.SetActive(false);
                    }
                }
            }

            // Re-highlight selected slot if it exists and remains active
            if (selectedSlotUI != null)
            {
                selectedSlotUI.SetSelected(true);
            }
        }

        private void SetFilter(string filter)
        {
            activeFilter = filter;
            UpdateTabVisuals();
            DeselectAll();
            RefreshUI();
        }

        private void OnSearchChanged(string text)
        {
            searchQuery = text.Trim();
            RefreshUI();
        }

        private SortType currentSortType = SortType.Name;

        private void CycleSort()
        {
            if (inventory == null) return;

            // Cycle through SortType values
            int nextSort = ((int)currentSortType + 1) % Enum.GetValues(typeof(SortType)).Length;
            currentSortType = (SortType)nextSort;

            UpdateSortButtonText();
            inventory.SortInventory(currentSortType);
            DeselectAll();
        }

        private void UpdateSortButtonText()
        {
            if (sortButtonText != null)
            {
                sortButtonText.text = $"SORT: {currentSortType.ToString().ToUpper()}";
            }
        }

        private void UpdateTabVisuals()
        {
            // Style active tab button to highlight it
            SetTabActive(tabAllButton, activeFilter == "ALL");
            SetTabActive(tabWeaponsButton, activeFilter == "WEAPONS");
            SetTabActive(tabConsumablesButton, activeFilter == "CONSUMABLES");
            SetTabActive(tabMaterialsButton, activeFilter == "MATERIALS");
        }

        private void SetTabActive(Button tabButton, bool isActive)
        {
            if (tabButton == null) return;
            
            ColorBlock cb = tabButton.colors;
            if (isActive)
            {
                cb.normalColor = new Color(0.18f, 0.49f, 0.9f); // Bright Accent Blue
                cb.selectedColor = cb.normalColor;
            }
            else
            {
                cb.normalColor = new Color(0.12f, 0.12f, 0.12f); // Dark Slate Gray
                cb.selectedColor = cb.normalColor;
            }
            tabButton.colors = cb;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }
        }

        // Open & Close logic
        public void ToggleInventory()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            isOpen = true;
            StopAllCoroutines();
            StartCoroutine(FadePanel(1f, true));
        }

        public void Close()
        {
            isOpen = false;
            DeselectAll();
            StopAllCoroutines();
            StartCoroutine(FadePanel(0f, false));
        }

        private System.Collections.IEnumerator FadePanel(float targetAlpha, bool interactable)
        {
            if (mainPanelGroup == null) yield break;

            mainPanelGroup.blocksRaycasts = interactable;
            mainPanelGroup.interactable = interactable;

            float startAlpha = mainPanelGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                mainPanelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainPanelGroup.alpha = targetAlpha;
        }
    }
}
