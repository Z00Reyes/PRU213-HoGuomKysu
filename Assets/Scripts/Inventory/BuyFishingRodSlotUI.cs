using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem
{
    [System.Serializable]
    public class ShopItem
    {
        public string id;
        public string itemName;
        public string description;
        public int price;
        public Sprite icon;
        public bool isBobber; // true if bobber, false if rod
        public int luckLevel; // Lucky ratio
    }

    public class BuyFishingRodSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image selectionGlow;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private GameObject ownedBadge;

        [Header("Animation")]
        [SerializeField] private float hoverScaleAmount = 1.06f;
        [SerializeField] private float scaleTransitionTime = 0.1f;

        private int slotIndex = -1;
        private BuyFishingRodStoreUI parentStoreUI;
        private ShopItem currentItem;
        private Coroutine scaleCoroutine;
        private Vector3 originalScale;

        public int SlotIndex => slotIndex;
        public ShopItem CurrentItem => currentItem;

        private void Awake()
        {
            originalScale = transform.localScale;
            if (selectionGlow != null) selectionGlow.gameObject.SetActive(false);
            if (equippedBadge != null) equippedBadge.SetActive(false);
            if (ownedBadge != null) ownedBadge.SetActive(false);
        }

        public void Initialize(int index, BuyFishingRodStoreUI store)
        {
            slotIndex = index;
            parentStoreUI = store;
            ClearSlot();
        }

        public void SetItem(ShopItem item, bool isOwned, bool isEquipped)
        {
            currentItem = item;

            if (item == null)
            {
                ClearSlot();
                return;
            }

            itemIcon.sprite = item.icon;
            itemIcon.gameObject.SetActive(item.icon != null);

            if (equippedBadge != null) equippedBadge.SetActive(isEquipped);
            if (ownedBadge != null) ownedBadge.SetActive(isOwned && !isEquipped);
        }

        public void ClearSlot()
        {
            currentItem = null;
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            if (equippedBadge != null) equippedBadge.SetActive(false);
            if (ownedBadge != null) ownedBadge.SetActive(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionGlow != null) selectionGlow.gameObject.SetActive(isSelected);
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
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
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
            if (selectionGlow != null) selectionGlow.gameObject.SetActive(false);
        }
    }
}
