using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class InventoryDemo : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private Button toggleButton;

        private void Start()
        {
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }

            if (inventoryUI == null)
            {
                inventoryUI = FindFirstObjectByType<InventoryUI>();
            }

            if (toggleButton != null && inventoryUI != null)
            {
                toggleButton.onClick.AddListener(inventoryUI.ToggleInventory);
            }

            if (inventory != null)
            {
                PopulateDemoItems();
            }
        }

        private void PopulateDemoItems()
        {
            inventory.Clear();

            // Create procedural item assets at runtime
            ItemData healthPotion = CreateMockItem("pot_health", "Health Potion", "A crimson flask that heals 50 HP upon consumption.", ItemType.Consumable, Rarity.Common, 15, "Heal HP", 50, CreateProceduralTexture(Color.red, "potion"));
            ItemData manaPotion = CreateMockItem("pot_mana", "Mana Potion", "A glowing blue brew that restores 30 MP.", ItemType.Consumable, Rarity.Rare, 15, "Restore MP", 30, CreateProceduralTexture(Color.cyan, "potion"));
            ItemData ironOre = CreateMockItem("mat_iron", "Iron Ore", "Raw unrefined iron ore. Used for crafting weapons.", ItemType.Material, Rarity.Common, 99, null, 0, CreateProceduralTexture(new Color(0.4f, 0.4f, 0.4f), "ore"));
            ItemData dragonScale = CreateMockItem("mat_dragon", "Dragon Scale", "An extremely durable scale from an ancient red dragon.", ItemType.Material, Rarity.Epic, 99, null, 0, CreateProceduralTexture(new Color(0.8f, 0.2f, 0.1f), "scale"));
            ItemData ironSword = CreateMockItem("wpn_sword", "Iron Sword", "A standard soldier's sword. Sturdy and reliable.", ItemType.Weapon, Rarity.Common, 1, "Attack", 12, CreateProceduralTexture(new Color(0.7f, 0.7f, 0.75f), "sword"));
            ItemData sunfireBlade = CreateMockItem("wpn_sunfire", "Sunfire Greatsword", "An ancient blade forged in the heart of a dying star.", ItemType.Weapon, Rarity.Legendary, 1, "Attack", 75, CreateProceduralTexture(new Color(1f, 0.5f, 0f), "sword"));
            ItemData chestplate = CreateMockItem("arm_chest", "Steel Chestplate", "Provides heavy physical protection.", ItemType.Armor, Rarity.Rare, 1, "Defense", 25, CreateProceduralTexture(new Color(0.5f, 0.5f, 0.6f), "armor"));
            ItemData ruby = CreateMockItem("mat_ruby", "Ruby Gem", "A valuable gem that glimmers under sunlight.", ItemType.Material, Rarity.Legendary, 99, "Value", 1000, CreateProceduralTexture(new Color(0.9f, 0f, 0.3f), "gem"));

            // Add to inventory with counts
            inventory.AddItem(healthPotion, 5);
            inventory.AddItem(ironOre, 24);
            inventory.AddItem(ironSword, 1);
            inventory.AddItem(manaPotion, 2);
            inventory.AddItem(dragonScale, 3);
            inventory.AddItem(sunfireBlade, 1);
            inventory.AddItem(chestplate, 1);
            inventory.AddItem(ruby, 8);
            inventory.AddItem(healthPotion, 3); // Test stacking: should stack onto existing Health Potions (total 8)
            inventory.AddItem(ironOre, 80);      // Test stack overflow: should fill one slot to 99, create new slot with remainder
        }

        private ItemData CreateMockItem(string id, string itemName, string desc, ItemType type, Rarity rarity, int maxStack, string statName, int statVal, Sprite icon)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.id = id;
            item.itemName = itemName;
            item.description = desc;
            item.type = type;
            item.rarity = rarity;
            item.maxStackSize = maxStack;
            item.statLabel = statName;
            item.statValue = statVal;
            item.icon = icon;
            return item;
        }

        private Sprite CreateProceduralTexture(Color baseColor, string shape)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // Clear to transparent
            Color transparent = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            // Draw shape simple procedures
            if (shape == "potion")
            {
                // Draw flask bottle shape
                for (int y = 10; y < 45; y++)
                {
                    for (int x = 16; x < 48; x++)
                    {
                        // Flask belly
                        float distanceToCenter = Vector2.Distance(new Vector2(x, y), new Vector2(32, 25));
                        if (distanceToCenter <= 16)
                        {
                            tex.SetPixel(x, y, baseColor);
                        }
                    }
                }
                // Flask neck
                for (int y = 35; y < 54; y++)
                {
                    for (int x = 27; x < 37; x++)
                    {
                        tex.SetPixel(x, y, new Color(0.9f, 0.9f, 0.9f, 0.8f)); // Glass neck
                    }
                }
                // Liquid inside flask neck
                for (int y = 35; y < 43; y++)
                {
                    for (int x = 28; x < 36; x++)
                    {
                        tex.SetPixel(x, y, baseColor);
                    }
                }
            }
            else if (shape == "ore")
            {
                // Draw lump shape
                for (int y = 14; y < 50; y++)
                {
                    for (int x = 14; x < 50; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32 + Mathf.Sin(y * 0.2f) * 4, 32 + Mathf.Cos(x * 0.2f) * 4));
                        if (dist <= 18)
                        {
                            tex.SetPixel(x, y, baseColor);
                        }
                    }
                }
            }
            else if (shape == "gem")
            {
                // Diamond shape
                for (int y = 10; y < 54; y++)
                {
                    for (int x = 10; x < 54; x++)
                    {
                        int halfSize = size / 2;
                        int dy = Mathf.Abs(y - halfSize);
                        int dx = Mathf.Abs(x - halfSize);
                        if (dx + dy <= 22)
                        {
                            // Shiny gradient
                            float shine = 1.0f - (dx + dy) / 22.0f;
                            tex.SetPixel(x, y, Color.Lerp(baseColor, Color.white, shine * 0.5f));
                        }
                    }
                }
            }
            else if (shape == "sword")
            {
                // Diagonal sword line
                for (int i = 12; i < 52; i++)
                {
                    tex.SetPixel(i, i, baseColor);
                    tex.SetPixel(i + 1, i, baseColor);
                    tex.SetPixel(i, i + 1, baseColor);
                }
                // Hilt
                for (int i = 12; i < 18; i++)
                {
                    tex.SetPixel(i, 30 - i, new Color(0.6f, 0.4f, 0.1f));
                }
            }
            else // armor or general
            {
                // Shield / chest shape
                for (int y = 12; y < 52; y++)
                {
                    for (int x = 18; x < 46; x++)
                    {
                        int halfSize = size / 2;
                        int dx = Mathf.Abs(x - halfSize);
                        if (dx <= 14 - (52 - y) * 0.2f)
                        {
                            tex.SetPixel(x, y, baseColor);
                        }
                    }
                }
            }

            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return sprite;
        }
    }
}
