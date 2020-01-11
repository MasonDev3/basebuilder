using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Job
{
    // This class holds info for a queued job, which can include
    // things like placing furniture, moving stored inventory, 
    // working at a desk, etc.

    public Tile tile; // { get; protected set; }
    // FIXME: Hard-coding a parameter for furniture. DO NOT LIKE
    public string jobObjectType 
    { 
        get; 
        protected set; 
    }

    public bool acceptsAnyInventoryItem = false;
    public float jobTime {
        get;
        protected set;
    }

    public Furniture furniturePrototype;

    Action<Job> cbJobComplete;
    Action<Job> cbJobCancel;

    Action<Job> cbJobWorked;

    public bool canTakeFromStockpile = true;

    public Dictionary<string, Inventory> inventoryRequirements;

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, 
        float jobTime, Inventory[] inventoryRequirements)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete += cbJobComplete;
        this.jobTime = jobTime;

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null)
        {
            foreach (Inventory inv in inventoryRequirements)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
                
            }
        }
        
    }

    protected Job(Job other)
    {
        tile = other.tile;
        jobObjectType = other.jobObjectType;
        cbJobComplete += other.cbJobComplete;
        jobTime = other.jobTime;

        inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null)
        {
            foreach (Inventory inv in other.inventoryRequirements.Values)
            {
                inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    virtual public Job Clone()
    {
        return new Job(this);
    }

    public void RegisterJobCompleteCallback(Action<Job> callback)
    {
        cbJobComplete += callback;
    }

    public void RegisterJobCancelCallback(Action<Job> callback)
    {
        cbJobCancel += callback;
    }

    public void UnregisterJobCompleteCallback(Action<Job> callback)
    {
        cbJobComplete -= callback;
    }

    public void UnregisterJobCancelCallback(Action<Job> callback)
    {
        cbJobCancel -= callback;
    }

    public void RegisterJobWorkedCallback(Action<Job> callback)
    {
        cbJobWorked += callback;
    }

    public void UnregisterJobWorkedCallback(Action<Job> callback)
    {
        cbJobWorked -= callback;
    }

    public void DoWork(float workTime)
    {
        // Check to make sure we actually have everything we need
        // If not, don't register the work time
        if (HasAllMaterial() == false)
        {
            //Debug.LogError("Tried to do work on a job that doesn't have all material");

            // Job cant actually be worked, but still call the callbacks for animations and whatnot

            cbJobWorked?.Invoke(this);
            return;
        }
        jobTime -= workTime;

        cbJobWorked?.Invoke(this);


        if (jobTime <= 0)
        {
            cbJobComplete?.Invoke(this);
        }
    }
    public void CancelJob()
    {
        cbJobCancel?.Invoke(this);

        tile.world.jobQueue.Remove(this);
    }

    public bool HasAllMaterial()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return false;
            }

        }
        return true;
    }

    public int DesiredInventoryAmount(Inventory inv)
    {
        if (acceptsAnyInventoryItem) 
        {
            return inv.maxStackSize;
        }

        if (inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            return 0;
        }

        if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize)
        {
            // We already have all we need
            return 0;
        }

        // The inventory is of a type we want, and still need more
        return inventoryRequirements[inv.objectType].maxStackSize - inventoryRequirements[inv.objectType].stackSize;
    }

    public Inventory GetFirstDesiredInventory()
    {
        foreach(Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return inv;
            }
                
        }

        return null;
    }
}
