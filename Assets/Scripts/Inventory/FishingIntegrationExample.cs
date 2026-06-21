using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// This is an example script showing how to integrate your fishing system
    /// with the Inventory UI system we just built.
    /// </summary>
    public class FishingIntegrationExample : MonoBehaviour
    {
        [Header("Fish Item Asset Reference")]
        [Tooltip("Assign the ScriptableObject (.asset) of the fish item here.")]
        [SerializeField] private ItemData fishItemData;

        /// <summary>
        /// Call this method when the player successfully catches a fish.
        /// </summary>
        public void OnFishCaught()
        {
            if (fishItemData == null)
            {
                Debug.LogError("FishingIntegrationExample: Please assign a Fish ItemData asset in the Inspector!");
                return;
            }

            // 1. Find the player's inventory component in the scene
            Inventory playerInventory = FindFirstObjectByType<Inventory>();

            if (playerInventory != null)
            {
                // 2. Add 1 caught fish to the inventory
                // The Inventory script will automatically:
                // - Search for existing identical fish slots to stack them.
                // - Enforce the slot limits (e.g. maximum of 20 items per slot).
                // - Create a new slot if the current stack exceeds the limit or is empty.
                // - Update the UI automatically.
                bool success = playerInventory.AddItem(fishItemData, 1);

                if (success)
                {
                    Debug.Log($"Successfully added caught fish: '{fishItemData.itemName}' to inventory!");
                }
                else
                {
                    Debug.LogWarning("Could not add fish. Player inventory is completely full!");
                }
            }
            else
            {
                Debug.LogError("No Inventory component found in the scene! Ensure you have run Tools > Inventory > Create Inventory UI.");
            }
        }
    }
}
