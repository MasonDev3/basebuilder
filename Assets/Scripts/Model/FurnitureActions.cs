using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Rewrite all of this to be part of the LUA interpreter for modding access.

/// <summary>
/// Actions taken by <see cref="Furniture"/> objects.
/// This file will most likely be moved to LUA code in the future
/// and will be parsed at run-time.
/// </summary>
public static class FurnitureActions
{
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        //Debug.Log("FurnitureActions::Door_UpdateAction: " + furn.furnParameters["openness"]);
        if (furn.GetParameter("is_opening") >= 1)
        {
            furn.ChangeParameter("openness", deltaTime * 4);
            if (furn.GetParameter("openness") >= 1)
            {
                furn.SetParameter("is_opening", 0);
            }
        }
        else
        {
            furn.ChangeParameter("openness", deltaTime * -4);
        }

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));

        furn.cbOnChanged?.Invoke(furn);
    }

    public static ENTERABILITY Door_IsEnterable(Furniture furn)
    {
        //Debug.Log("FurnitureActions::Door_IsEnterable");
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return ENTERABILITY.Yes;
        }

        return ENTERABILITY.Soon;
    }

    public static void JobComplete_FurnitureBuilding(Job theJob)
    {
        WorldController.instance.world.PlaceFurniture(theJob.jobObjectType, theJob.tile);
        theJob.tile.pendingFurnitureJob = null;
    }

    static public Inventory[] Stockpile_GetItemsFromFilter()
    {
        //TODO: This should be reading from some kind of UI for this
        // particular stockpile

        // Since jobs copy arrays automatically, we could already 
        // have an Inventory[] prepared and just return that (as a sort of example filter)

        return new Inventory[1] { new Inventory("Steel Plate", 50, 0) };
    }

    public static void Stockpile_UpdateAction(Furniture furn, float deltaTime)
    {
        // We need to ensure that we have a job on the queue asking for either 
        //      if we are empty, that any loose inventory be brought to us
        //      if we have something: then if we are still below the max stack size,
        //                              that more of the same should be brought to us


        //TODO: this function does not need to run each update. Once we get a lot
        // of furniture in a running game, this will run a LOT more than required.
        // Instead, run whenever:
        //              -- It gets created
        //              -- A good gets delivered, at which point we reset the job
        //              -- A good gets picked up
        //              -- The UI's filter of allowed items gets changed

        if (furn.Tile.inventory != null && furn.Tile.inventory.stackSize >= furn.Tile.inventory.maxStackSize)
        {
            // We are full!
            furn.ClearJobs();
            return;
        }

        // Job already queued?
        if (furn.JobCount() > 0)
        {
            // Cool, all done!
            return;
        }

        // We currently are NOT full, but we don't have a job either
        // Two options:
        //      -- Some inventory
        //      -- No inventory 

        if (furn.Tile.inventory != null && furn.Tile.inventory.stackSize == 0)
        {
            Debug.LogError("Stockpile has a zero size stack!!");
            furn.ClearJobs();
            return;
        }

        // TODO: In the future, stockpiles -- rather than being a bunch of individual 1x1 tiles,
        // they should manifest themselves as a single large object.
        // This would represent our first and probably only variable size furniture.
        // what happens if there's a "hole" in our stockpile becuase we have an acutal piece
        // of furniture installed in the middle of our stockpile?
        // In any case, once we implement "MEGA stockpiles", the job-creation system 
        // could be a lot smarter, in that even if the stockpile has some stuff in it, it can
        // also still be requisitioning different object types in its job creation.

        Inventory[] itemsDesired;

        if (furn.Tile.inventory == null)
        {
            Debug.Log("Creating job for new stack");
            itemsDesired = Stockpile_GetItemsFromFilter();
        }
        else
        {
            Debug.Log("Creating job for existing stack.");
            Inventory desiredInventory = furn.Tile.inventory.Clone();

            desiredInventory.maxStackSize -= desiredInventory.stackSize;
            desiredInventory.stackSize = 0;
            // We are empty -- ask for anything to be brought here
            itemsDesired = new Inventory[] {desiredInventory};
        }
        Job j = new Job(
                furn.Tile,
                null, // ""
                null,
                0,
                itemsDesired
        );
        j.canTakeFromStockpile = false;
        j.RegisterJobWorkedCallback(Stockpile_JobWorked);
        furn.AddJob(j);
        
        
    }

    static void Stockpile_JobWorked(Job j)
    {
        Debug.Log("StockPile_JobWorked called");
        j.tile.furniture.RemoveJob(j);
        // TODO: Change this when we figure out what we're doing for the all/any pickup job

        foreach(Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                j.tile.world.inventoryManager.PlaceInventory(j.tile, inv);
                return; // There should be no way that we ever end up with more than one inventory requirement with stackSize > 0
            }
        }
    }


}
