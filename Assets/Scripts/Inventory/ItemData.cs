using UnityEngine;

namespace InventorySystem
{
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material,
        Other
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string itemName;
        [TextArea(3, 10)]
        public string description;
        public Sprite icon;

        [Header("Properties")]
        public ItemType type;
        public Rarity rarity;
        public int maxStackSize = 99;
        
        [Header("Stats (Optional)")]
        public string statLabel;
        public int statValue;

        [Header("Economy")]
        public int sellPrice = 10;
        
        [Header("Luck")]
        public int luckScore;

        public virtual void Use()
        {
            Debug.Log($"Using item: {itemName}");
        }
    }
}
