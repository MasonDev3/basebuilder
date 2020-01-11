﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// LooseObjects are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

public class Inventory
{

    public string objectType = "Steel Plate";
    public int maxStackSize = 50;

    protected int _stackSize = 1;
    public int stackSize
    {
        get { return _stackSize; }
        set 
        { if (_stackSize != value)
            {
                _stackSize = value;
                if (tile != null && cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            } 
        }
    }

    Action<Inventory> cbInventoryChanged;

    public Tile tile;
    public Character character;

    public Inventory()
    {

    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType     = objectType;
        this.maxStackSize   = maxStackSize;
        this.stackSize      = stackSize;
    }

    protected Inventory(Inventory other)
    {
        objectType = other.objectType;
        maxStackSize = other.maxStackSize;
        stackSize = other.stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    public void RegisterInventoryChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged += callback;
    }

    public void UnregisterInventoryChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged -= callback;
    }
}
