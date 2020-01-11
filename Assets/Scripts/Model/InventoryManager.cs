using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager
{
    /// <summary>
    /// This is a list of all live inventories.
    /// (Later on this will likely be organized by rooms
    /// instead of a single master list. [Or in addition to])
    /// </summary>
    public Dictionary<string, List<Inventory>> inventories;
    public InventoryManager()
    {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    void CleanupInventory(Inventory inv)
    {
        if (inv.stackSize <= 0)
        {
            if (inventories.ContainsKey(inv.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }
            if (inv.tile != null)
            {
                inv.tile.inventory = null;
                inv.tile = null;
            }
            if (inv.character != null)
            {
                inv.character.inventory = null;
                inv.character = null;
            }
        }
    }

    /// <summary>
    /// Takes a stack of inventory and places it on the appropriate tile
    /// </summary>
    /// <param name="tile">The target tile</param>
    /// <param name="inv">The piece of inventory</param>
    /// <returns>Returns false if the tile did not accept the deposit, true otherwise</returns>
    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        bool tileWasEmpty = tile.inventory == null;

        if (tile.PlaceInventory(inv) == false)
        {
            // The tile did not accept inventory. STOP
            return false;
        }
        // At this point, "inv" might be an empty stack if it was merged to another stack
        CleanupInventory(inv);

        // We may have created a new stack on the tile, if the tile was previously empty
        if (tileWasEmpty)
        {
            if (inventories.ContainsKey(tile.inventory.objectType) == false)
            {
                inventories[tile.inventory.objectType] = new List<Inventory>();
            }
            inventories[tile.inventory.objectType].Add(tile.inventory);

            tile.world.OnInventoryCreated(tile.inventory);
        }
        return true;
    }

    public bool PlaceInventory(Job job, Inventory inv)
    {
        if (job.inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            Debug.LogError("Trying to add inventory to a job that it doesn't want");
            return false; 
        }

        job.inventoryRequirements[inv.objectType].stackSize += inv.stackSize;

        if(job.inventoryRequirements[inv.objectType].maxStackSize < job.inventoryRequirements[inv.objectType].stackSize)
        {
            inv.stackSize = job.inventoryRequirements[inv.objectType].stackSize - job.inventoryRequirements[inv.objectType].maxStackSize;
            job.inventoryRequirements[inv.objectType].stackSize = job.inventoryRequirements[inv.objectType].maxStackSize;
        }
        else
        {
            inv.stackSize = 0;

        }
        // At this point, "inv" might be an empty stack if it was merged to another stack
        CleanupInventory(inv);

        return true;
    }
    public bool PlaceInventory(Character character, Inventory source, int amount = -1)
    {
        if (amount < 0)
        {
            amount = source.stackSize;
        }
        else
        {
            amount = Mathf.Min(amount, source.stackSize);
        }

        if (character.inventory == null)
        {
            character.inventory = source.Clone();
            character.inventory.stackSize = 0;
            inventories[character.inventory.objectType].Add(character.inventory);
        }
        else if (character.inventory.objectType != source.objectType)
        {
            Debug.LogError("Character is trying to pickup mismatched inventory object type");
            return false;
        }

        character.inventory.stackSize += amount;

        if (character.inventory.maxStackSize < character.inventory.stackSize)
        {
            source.stackSize = character.inventory.stackSize - character.inventory.maxStackSize;
            character.inventory.stackSize = character.inventory.maxStackSize;
        }
        else
        {
            source.stackSize -= amount;

        }
        // At this point, "inv" might be an empty stack if it was merged to another stack
        CleanupInventory(source);

        return true;
    }


    /// <summary>
    /// Gets the closest inventory type with desired amount
    /// </summary>
    /// <param name="objectType">The objectType of the inventory</param>
    /// <param name="t">The tile on which the inventory is</param>
    /// <param name="desiredAmount">If no stack has enough, this will return the largest</param>
    /// <returns></returns>
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount, bool canTakeFromStockpile)
    {
        //! A) We are lying about returning the closest item
        //! B) There is no way to return the closest item in an optimal manner until our database is more sophisticated
        //!     (i.e. seperates tile inventory from character inventory and room optimization
        if (inventories.ContainsKey(objectType) == false)
        {
            Debug.LogError("GetClosestIn ventoryOfType: No items of desired type");
            return null;
        }
        foreach (Inventory inv in inventories[objectType])
        {
            if (inv.tile != null && (canTakeFromStockpile || 
            inv.tile.furniture == null || inv.tile.furniture.IsStockpile() == false))
            {
                return inv;
            }
        }

        return null;
    }
}
