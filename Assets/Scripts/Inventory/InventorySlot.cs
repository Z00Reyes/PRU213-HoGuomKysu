using System;

namespace InventorySystem
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int quantity;

        public InventorySlot()
        {
            Clear();
        }

        public InventorySlot(ItemData itemData, int quantity)
        {
            this.itemData = itemData;
            this.quantity = quantity;
        }

        public bool IsEmpty => itemData == null || quantity <= 0;

        public void Clear()
        {
            itemData = null;
            quantity = 0;
        }

        public bool CanStack(ItemData item, int amountToAdd)
        {
            if (IsEmpty) return true;
            if (itemData.id != item.id) return false;
            return quantity + amountToAdd <= itemData.maxStackSize;
        }

        public int AddQuantity(int amount)
        {
            if (itemData == null) return amount; // Cannot add to empty slot without assigning ItemData first
            
            int potentialNewQty = quantity + amount;
            if (potentialNewQty <= itemData.maxStackSize)
            {
                quantity = potentialNewQty;
                return 0;
            }
            else
            {
                int overflow = potentialNewQty - itemData.maxStackSize;
                quantity = itemData.maxStackSize;
                return overflow;
            }
        }

        public void RemoveQuantity(int amount)
        {
            quantity -= amount;
            if (quantity <= 0)
            {
                Clear();
            }
        }
    }
}
