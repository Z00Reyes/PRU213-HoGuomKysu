using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem
{
    public class SellFishSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image selectionGlow;
        [SerializeField] private TextMeshProUGUI priceText;

        [Header("Aesthetics & Animation")]
        [SerializeField] private float hoverScaleAmount = 1.06f;
        [SerializeField] private float scaleTransitionTime = 0.1f;
        [SerializeField] private Color commonColor = new Color(0.95f, 0.95f, 0.95f, 1.0f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        [SerializeField] private Color epicColor = new Color(0.6f, 0.2f, 0.8f, 1.0f);
        [SerializeField] private Color legendaryColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);

        private int slotIndex = -1;
        private SellFishStoreUI parentStoreUI;
        private ItemData currentItemData;
        private int currentQuantity;
        private Coroutine scaleCoroutine;
        private Vector3 originalScale;

        public int SlotIndex => slotIndex;
        public ItemData CurrentItemData => currentItemData;
        public int CurrentQuantity => currentQuantity;

        private void Awake()
        {
            originalScale = transform.localScale;
            if (selectionGlow != null)
            {
                selectionGlow.gameObject.SetActive(false);
            }
        }

        public void Initialize(int index, SellFishStoreUI store)
        {
            slotIndex = index;
            parentStoreUI = store;
            ClearSlot();
        }

        public void SetItem(ItemData itemData, int qty)
        {
            currentItemData = itemData;
            currentQuantity = qty;

            if (itemData == null)
            {
                ClearSlot();
                return;
            }

            // Set icon
            itemIcon.sprite = itemData.icon;
            itemIcon.gameObject.SetActive(itemData.icon != null);

            // Set quantity text
            quantityText.text = qty.ToString();
            quantityText.gameObject.SetActive(true);

            // Set icon alpha and text color depending on owned quantity
            if (qty == 0)
            {
                itemIcon.color = new Color(1f, 1f, 1f, 0.3f);
                quantityText.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            }
            else
            {
                itemIcon.color = Color.white;
                quantityText.color = Color.white;
            }

            // Rarity color highlight on background (subtle tint)
            if (slotBackground != null)
            {
                Color tint = GetRarityColor(itemData.rarity);
                slotBackground.color = Color.Lerp(Color.white, tint, 0.15f);
            }

            // Set price text
            if (priceText != null)
            {
                priceText.text = $"{itemData.sellPrice}g";
                priceText.gameObject.SetActive(true);
            }
        }

        public void ClearSlot()
        {
            currentItemData = null;
            currentQuantity = 0;
            if (itemIcon != null)
            {
                itemIcon.gameObject.SetActive(false);
                itemIcon.color = Color.white;
            }
            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(false);
                quantityText.color = Color.white;
            }
            if (slotBackground != null)
            {
                slotBackground.color = Color.white;
            }
            if (priceText != null)
            {
                priceText.gameObject.SetActive(false);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionGlow != null)
            {
                selectionGlow.gameObject.SetActive(isSelected);
            }
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartScaleAnimation(originalScale * hoverScaleAmount);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartScaleAnimation(originalScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentStoreUI != null)
            {
                parentStoreUI.SelectSlot(this);
            }
        }

        private void StartScaleAnimation(Vector3 targetScale)
        {
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            if (gameObject.activeInHierarchy)
            {
                scaleCoroutine = StartCoroutine(AnimateScale(targetScale));
            }
        }

        private IEnumerator AnimateScale(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float time = 0;
            while (time < scaleTransitionTime)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, time / scaleTransitionTime);
                time += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
            if (selectionGlow != null)
            {
                selectionGlow.gameObject.SetActive(false);
            }
        }
    }
}
