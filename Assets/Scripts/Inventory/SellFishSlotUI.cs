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

        [Header("Aesthetics & Animation")]
        [SerializeField] private float hoverScaleAmount = 1.06f;
        [SerializeField] private float scaleTransitionTime = 0.1f;
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        [SerializeField] private Color epicColor = new Color(0.6f, 0.2f, 0.8f, 1.0f);
        [SerializeField] private Color legendaryColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);

        private int slotIndex = -1;
        private SellFishStoreUI parentStoreUI;
        private InventorySlot currentSlot;
        private Coroutine scaleCoroutine;
        private Vector3 originalScale;

        public int SlotIndex => slotIndex;
        public InventorySlot CurrentSlot => currentSlot;

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

        public void SetItem(InventorySlot slot)
        {
            currentSlot = slot;

            if (slot == null || slot.IsEmpty)
            {
                ClearSlot();
                return;
            }

            // Set icon
            itemIcon.sprite = slot.itemData.icon;
            itemIcon.gameObject.SetActive(slot.itemData.icon != null);

            // Set quantity text
            if (slot.quantity > 1)
            {
                quantityText.text = slot.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }

            // Rarity color highlight on background (subtle tint)
            if (slotBackground != null)
            {
                Color tint = GetRarityColor(slot.itemData.rarity);
                slotBackground.color = Color.Lerp(Color.white, tint, 0.15f);
            }
        }

        public void ClearSlot()
        {
            currentSlot = null;
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            if (quantityText != null) quantityText.gameObject.SetActive(false);
            if (slotBackground != null)
            {
                slotBackground.color = Color.white;
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
