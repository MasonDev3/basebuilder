using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum TileType { Empty, Floor };

public enum ENTERABILITY { Yes, Never, Soon };
public class Tile : IXmlSerializable
{
    #region Variable Declarations;
    TileType type = TileType.Empty;
    Action<Tile> cbTileChanged;
    public Inventory inventory;
    
    public World world { get; protected set; }

    public TileType Type
    {
        get
        {
            return type;
        }
        set
        {
            TileType oldType = type;
            type = value;
            if (cbTileChanged != null && oldType != type)
            {
                cbTileChanged(this);
            }
        }
    }
    public int X { get; protected set; }
    public int Y { get; protected set; }
    public Furniture furniture { get; protected set; }

    public Room room;

    public Job pendingFurnitureJob;
    /// <summary>
    ///  FIXME: Hardcoded for now
    ///  Just a reminder of something we might want to do
    ///  in the future
    /// </summary>
    const float baseTileMovementCost = 1; 
    public float movementCost
    {
        get
        {
            if (Type == TileType.Empty)
            {
                return 0; // Unwalkable
            }
            if (furniture == null)
            {
                return baseTileMovementCost;
            }
            return baseTileMovementCost * furniture.movementCost;
        }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="Tile"/> class.
    /// </summary>
    /// <param name="world">World instance</param>
    /// <param name="x">X coordinate of tile</param>
    /// <param name="y">Y coordinate of tile</param>
    public Tile(World world, int x, int y)
    {
        this.world = world;
        X = x;
        Y = y;
    }
    #endregion

    #region Private Method Definitions

    #endregion

    #region Public Method Definitions

    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }

    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public bool UninstallFurniture()
    {
        //! Just uninstalling. 
        //TODO: What if we have a multi-tile furniture?
        furniture = null;
        return true;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            return UninstallFurniture();
        }
        if (objInstance.IsValidPosition(this) == false)
        {
            Debug.LogError("Trying to assign a furniture to a non-valid tile");
            return false;
        }

        for (int x_offset = X; x_offset < (X + objInstance.Width); x_offset++)
        {
            for (int y_offset = Y; y_offset < (Y + objInstance.Height); y_offset++)
            {
                Tile t = world.GetTileAt(x_offset, y_offset);
                t.furniture = objInstance;
            }
        }
        
        return true;
    }

    public bool PlaceInventory(Inventory inv)
    {
        if (inv == null)
        {
            inventory = null;
            return true;
        }
        if (inventory != null)
        {
            if (inventory.objectType != inv.objectType)
            {
                Debug.LogError("Trying to assign inventory of different types to the same tile");
                return false;
            }

            int numToMove = inv.stackSize;
            if (inventory.stackSize + numToMove > inventory.maxStackSize)
            {
                numToMove = inventory.maxStackSize - inventory.stackSize;
            }

            inventory.stackSize += numToMove;
            inv.stackSize -= numToMove;
            
            return true;
        }
        // At this point, we know our current inventory is actually
        // null. We can't do a direct assignment becuase the inventory manager
        // needs to know the old stack is now empty and must be removed from the 
        // previous lists

        inventory = inv.Clone();
        inventory.tile = this;
        inv.stackSize = 0;

        return true;
    }

    // Tells us if two tiles are adjacent
    public bool IsNeighbor(Tile tile, bool diagOkay = false)
    {
        // One liner solution for checking neighbors below
        return Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 || // Check hori/vert adjacentcy
            (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1)); // Check diag adjacentcy

    }
    /// <summary>
    /// Gets the neighbor tiles.
    /// </summary>
    /// <param name="diagOkay">Is diaganol movement okay?</param>
    /// <returns> The Neighbor tiles.</returns>
    public Tile[] GetNeighbors(bool diagOkay = false)
    {
        Tile[] neighbors;

        if (diagOkay == false)
        {
            neighbors = new Tile[4]; // N E S W
        }
        else
        {
            neighbors = new Tile[8]; // N E S W NE SE SW NW
        }

        Tile potentialNeighbor;

        potentialNeighbor = world.GetTileAt(X, Y + 1);
        neighbors[0] = potentialNeighbor; // Could be null but thats ok

        potentialNeighbor = world.GetTileAt(X + 1, Y);
        neighbors[1] = potentialNeighbor; // Could be null but thats ok

        potentialNeighbor = world.GetTileAt(X, Y - 1);
        neighbors[2] = potentialNeighbor; // Could be null but thats ok

        potentialNeighbor = world.GetTileAt(X - 1, Y);
        neighbors[3] = potentialNeighbor; // Could be null but thats ok

        if (diagOkay == true)
        {
            
            potentialNeighbor = world.GetTileAt(X + 1, Y + 1); // NE
            neighbors[4] = potentialNeighbor; // Could be null but thats ok

            potentialNeighbor = world.GetTileAt(X + 1, Y - 1); // SE
            neighbors[5] = potentialNeighbor; // Could be null but thats ok

            potentialNeighbor = world.GetTileAt(X - 1, Y - 1); // SW
            neighbors[6] = potentialNeighbor; // Could be null but thats ok

            potentialNeighbor = world.GetTileAt(X - 1, Y + 1); // NW
            neighbors[7] = potentialNeighbor; // Could be null but thats ok
        }

        return neighbors;
    }

    public Tile NorthTile()
    {
        return world.GetTileAt(X, Y + 1);
    }
    public Tile EastTile()
    {
        return world.GetTileAt(X + 1, Y);
    }
    public Tile SouthTile()
    {
        return world.GetTileAt(X, Y - 1);
    }
    public Tile WestTile()
    {
        return world.GetTileAt(X - 1, Y);
    }

    /// <summary>
    /// Determines if you are allowed to enter this tile at this specific time.
    /// Returns Yes, Never, Soon. See <see cref="ENTERABILITY"/>
    /// </summary>
    /// <returns></returns>
    public ENTERABILITY IsEnterable()
    {
        // Returns true if you can enter this tile right now
        if (movementCost == 0)
        {
            return ENTERABILITY.Never;
        }

        // check furniture to see if it has a special block on how enterable it is
        if (furniture != null && furniture.isEnterable != null)
        {
            return furniture.isEnterable(furniture);
        }

        return ENTERABILITY.Yes;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }

    

    #endregion
}
