using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public enum SortType
    {
        Name,
        Quantity,
        Rarity,
        Type
    }

    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int slotCount = 24;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public event Action onInventoryChanged;

        public List<InventorySlot> Slots => slots;
        public int SlotCount => slotCount;

        private void Awake()
        {
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            if (slots.Count == slotCount) return;

            slots.Clear();
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new InventorySlot());
            }
        }

        public bool AddItem(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            if (slots == null || slots.Count == 0)
            {
                InitializeSlots();
            }

            int remaining = quantity;

            // 1. Try to add to existing stacks
            if (item.maxStackSize > 1)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    if (!slots[i].IsEmpty && slots[i].itemData.id == item.id)
                    {
                        int currentQty = slots[i].quantity;
                        if (currentQty < item.maxStackSize)
                        {
                            remaining = slots[i].AddQuantity(remaining);
                            if (remaining == 0)
                            {
                                onInventoryChanged?.Invoke();
                                return true;
                            }
                        }
                    }
                }
            }

            // 2. Try to fill empty slots
            for (int i = 0; i < slotCount; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].itemData = item;
                    remaining = slots[i].AddQuantity(remaining);
                    if (remaining == 0)
                    {
                        onInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            onInventoryChanged?.Invoke();
            return remaining == 0; // returns false if inventory was full and couldn't fit everything
        }

        public void RemoveItem(int index, int amount)
        {
            if (index < 0 || index >= slots.Count) return;
            if (slots[index].IsEmpty) return;

            slots[index].RemoveQuantity(amount);
            onInventoryChanged?.Invoke();
        }

        public void CompressInventory()
        {
            // Gather all items and quantity count, grouped by item ID
            Dictionary<string, (ItemData item, int totalQuantity)> itemTotals = new Dictionary<string, (ItemData, int)>();

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    var data = slots[i].itemData;
                    if (itemTotals.ContainsKey(data.id))
                    {
                        var entry = itemTotals[data.id];
                        entry.totalQuantity += slots[i].quantity;
                        itemTotals[data.id] = entry;
                    }
                    else
                    {
                        itemTotals[data.id] = (data, slots[i].quantity);
                    }
                }
            }

            // Re-populate inventory slots
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Clear();
            }

            int currentSlotIndex = 0;
            foreach (var kvp in itemTotals)
            {
                ItemData item = kvp.Value.item;
                int remaining = kvp.Value.totalQuantity;

                while (remaining > 0 && currentSlotIndex < slotCount)
                {
                    slots[currentSlotIndex].itemData = item;
                    int added = Mathf.Min(remaining, item.maxStackSize);
                    slots[currentSlotIndex].quantity = added;
                    remaining -= added;
                    currentSlotIndex++;
                }
            }

            onInventoryChanged?.Invoke();
        }

        public void SortInventory(SortType sortType)
        {
            // Step 1: Compress first to combine same items
            CompressInventory();

            // Step 2: Separate items from empty slots
            List<InventorySlot> activeSlots = new List<InventorySlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    activeSlots.Add(slots[i]);
                }
            }

            // Step 3: Sort active slots
            activeSlots.Sort((a, b) =>
            {
                switch (sortType)
                {
                    case SortType.Name:
                        return string.Compare(a.itemData.itemName, b.itemData.itemName, StringComparison.OrdinalIgnoreCase);
                    case SortType.Quantity:
                        return b.quantity.CompareTo(a.quantity); // Descending
                    case SortType.Rarity:
                        return b.itemData.rarity.CompareTo(a.itemData.rarity); // Descending (Legendary to Common)
                    case SortType.Type:
                        return a.itemData.type.CompareTo(b.itemData.type);
                    default:
                        return 0;
                }
            });

            // Step 4: Rebuild slots list
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < activeSlots.Count)
                {
                    slots[i] = activeSlots[i];
                }
                else
                {
                    slots[i] = new InventorySlot();
                }
            }

            onInventoryChanged?.Invoke();
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Clear();
            }
            onInventoryChanged?.Invoke();
        }
    }
}
