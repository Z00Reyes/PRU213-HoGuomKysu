using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class ItemTooltipUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject contentParent;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemRarityText;
        [SerializeField] private TextMeshProUGUI itemTypeText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemStatsText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button discardButton;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1.0f);
        [SerializeField] private Color epicColor = new Color(0.7f, 0.3f, 0.9f);
        [SerializeField] private Color legendaryColor = new Color(1.0f, 0.6f, 0.0f);

        private InventorySlotUI selectedSlotUI;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
            if (discardButton != null) discardButton.onClick.AddListener(OnDiscardClicked);

            Hide();
        }

        public void DisplayItem(InventorySlotUI slotUI)
        {
            selectedSlotUI = slotUI;

            if (slotUI == null || slotUI.CurrentSlot == null || slotUI.CurrentSlot.IsEmpty)
            {
                Hide();
                return;
            }

            ItemData item = slotUI.CurrentSlot.itemData;

            // Set icon
            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.gameObject.SetActive(item.icon != null);
            }

            // Set text values
            if (itemNameText != null)
            {
                itemNameText.text = item.itemName;
                itemNameText.color = GetRarityColor(item.rarity);
            }

            if (itemRarityText != null)
            {
                itemRarityText.text = $"{item.rarity} Item";
                itemRarityText.color = GetRarityColor(item.rarity);
            }

            if (itemTypeText != null)
            {
                itemTypeText.text = item.type.ToString();
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = item.description;
            }

            // Set optional stats text
            if (itemStatsText != null)
            {
                if (!string.IsNullOrEmpty(item.statLabel) && item.statValue != 0)
                {
                    itemStatsText.text = $"{item.statLabel}: +{item.statValue}";
                    itemStatsText.gameObject.SetActive(true);
                }
                else
                {
                    itemStatsText.gameObject.SetActive(false);
                }
            }

            // Update button states
            if (useButton != null)
            {
                // Can only use consumables (or custom logic)
                useButton.gameObject.SetActive(item.type == ItemType.Consumable || item.type == ItemType.Weapon || item.type == ItemType.Armor);
                
                // Customize text based on type
                var btnText = useButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    if (item.type == ItemType.Weapon || item.type == ItemType.Armor)
                        btnText.text = "EQUIP";
                    else
                        btnText.text = "USE";
                }
            }

            Show();
        }

        public void Show()
        {
            if (contentParent != null) contentParent.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (contentParent != null) contentParent.SetActive(false);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            selectedSlotUI = null;
        }

        private void OnUseClicked()
        {
            if (selectedSlotUI == null || selectedSlotUI.CurrentSlot == null) return;

            ItemData item = selectedSlotUI.CurrentSlot.itemData;
            item.Use();

            // Notify inventory to consume 1 item
            Inventory inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null)
            {
                inventory.RemoveItem(selectedSlotUI.SlotIndex, 1);
            }

            // Update or close tooltip
            if (selectedSlotUI.CurrentSlot == null || selectedSlotUI.CurrentSlot.IsEmpty)
            {
                Hide();
            }
            else
            {
                DisplayItem(selectedSlotUI);
            }
        }

        private void OnDiscardClicked()
        {
            if (selectedSlotUI == null || selectedSlotUI.CurrentSlot == null) return;

            Inventory inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null)
            {
                // Discard all in stack
                inventory.RemoveItem(selectedSlotUI.SlotIndex, selectedSlotUI.CurrentSlot.quantity);
            }

            Hide();
        }

        private Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return rareColor;
                case Rarity.Epic: return epicColor;
                case Rarity.Legendary: return legendaryColor;
                default: return commonColor;
            }
        }
    }
}
