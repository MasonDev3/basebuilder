using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

// InstalledObjects are things like walls, doors, furniture, etc

public class Furniture : IXmlSerializable
{
    // This represents the BASE tile of an object -- but in practice, large objects may actually occupy 
    // multiple tiles.

    /// <summary>
    /// Holds custom furniture parameters
    /// </summary>
    protected Dictionary<string, float> furnParameters;
    /// <summary>
    /// An array of actions to call in the update of each frame
    /// Passed a piece of furniture and deltatime
    /// </summary>
    protected Action<Furniture, float> updateActions;

    public Func<Furniture, ENTERABILITY> isEnterable;

    List<Job> jobs;

    /// <summary>
    /// Update the door.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Update(float deltaTime)
    {
        updateActions?.Invoke(this, deltaTime);
    }

    

    public Tile Tile { get; protected set; }

    // This objectType will be queried by the visual system to know what sprite to render for this object.
    public string ObjectType { get; protected set; }

    // This is a multiplier. So a value of "2.0f" here, means you would move at half speed.
    // Tile types and other environmental effects may further act as multiplier
    // For example, a "rough" tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    // would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
    // SPECIAL: If movementCost = 0, then this tile is impassible. (e.g. a wall)
    public float movementCost { get; protected set; }

    public bool roomEnclosure { get; protected set; }

    // For example, a sofa might be 3x2 (actual graphics only appear to cover the 3x1 area, the extra row is for leg room)
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public Color tint = Color.white;

    public bool linksToNeighbor { get; protected set; }
    public Action<Furniture> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

    //TODO: Implement larger objects
    //TODO: Implement object rotation

    // This is basically used by the object factory to create the prototypical object
    // Note that is DOESN'T ask for a TILE
    /// <summary>
    /// Empty constructor used for serialization
    /// </summary>
    public Furniture()
    {
        furnParameters = new Dictionary<string, float>();
        jobs = new List<Job>();
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="other">The other piece of furniture for the copy.</param>
    protected Furniture(Furniture other)
    {
        ObjectType = other.ObjectType;
        movementCost = other.movementCost;
        roomEnclosure = other.roomEnclosure;
        Width = other.Width;
        Height = other.Height;
        tint = other.tint;
        linksToNeighbor = other.linksToNeighbor;
        furnParameters = new Dictionary<string, float>(other.furnParameters);
        jobs = new List<Job>();

        if (other.updateActions != null)
        {
            updateActions = (Action<Furniture, float>)other.updateActions.Clone();

        }

        if (other.funcPositionValidation != null)
        {
            funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();

        }

        isEnterable = other.isEnterable;
    }
    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

    /// <summary>
    /// Create a new piece of <see cref="Furniture"/>.
    /// Only ever used for prototypes
    /// </summary>
    /// <param name="objectType">String of the object type</param>
    /// <param name="movementCost">The movement cost of the furniture, default of 1.0f</param>
    /// <param name="width">The width of the furniture, default of 1</param>
    /// <param name="height">The height of the furniture, defualt of 1</param>
    /// <param name="linksToNeighbor">Does this piece of furniture connect to a neighbor? Default of false</param>
    public Furniture (string objectType, float movementCost = 1.0f, int width = 1, int height = 1, bool linksToNeighbor = false, bool roomEnclosure = false)
    {
        ObjectType = objectType;
        this.movementCost = movementCost;
        this.roomEnclosure = roomEnclosure;
        this.Width = width;
        this.Height = height;
        this.linksToNeighbor = linksToNeighbor;

        funcPositionValidation = DEFAULT__IsValidPosition;

        furnParameters = new Dictionary<string, float>();
    }
    
    static public Furniture PlaceInstance(Furniture prototype, Tile tile)
    {
        if (prototype.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Position validity function returned false");
            return null;
        }

        // We know our placement destination is valid at this point.

        Furniture obj = prototype.Clone(); 
        obj.Tile = tile;

        if (tile.PlaceFurniture(obj) == false)
        {
            // For some reason, we weren't able to place our object in this tile.
            // Probably, it was already occupied

            // Do NOT return our newly instantiated object.
            // Instead, it will be garbage collected.
            return null;
        }

        if (obj.linksToNeighbor)
        {
            Tile t;
            int x = tile.X;
            int y = tile.Y;
            // This type of furniture links itself to its neighbors
            // so we should inform our neighbors that they have a 
            // new buddy. Just trigger their OnChangedCallback
            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.ObjectType == obj.ObjectType)
            {
                // We have a northern neighbor with the same objectType as us so 
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.ObjectType == obj.ObjectType)
            {
                // We have a eastern neighbor with the same objectType as us so 
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.ObjectType == obj.ObjectType)
            {
                // We have a southern neighbor with the same objectType as us so 
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.ObjectType == obj.ObjectType)
            {
                // We have a western neighbor with the same objectType as us so 
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
        }

        return obj;
    }

    public void RegisterOnChangedCallback(Action<Furniture> callbackFunc)
    {
        cbOnChanged += callbackFunc;
    }

    public void UnregisterOnChangedCallback(Action<Furniture> callbackFunc)
    {
        cbOnChanged -= callbackFunc;
    }

    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    // FIXME: These two functions are horrible and need to be worked on
    protected bool DEFAULT__IsValidPosition(Tile t)
    {
        for (int x_offset = t.X; x_offset < (t.X + Width); x_offset++)
        {
            for (int y_offset = t.Y; y_offset < (t.Y + Height); y_offset++)
            {
                Tile t2 = t.world.GetTileAt(x_offset, y_offset);

                // Make sure tile if FLOOR
                // Make sure tile doesn't already have furniture
                if (t2.Type != TileType.Floor)
                {
                    return false;
                }

                if (t2.furniture != null)
                {
                    return false;
                }
            }

        }
        

        return true;
    }

    /// <summary>
    /// Gets the custom furniture parameter from a string key
    /// </summary>
    /// <param name="key">Key string</param>
    /// <param name="defaultValue">Default value</param>
    /// <returns>The parameter value (float)</returns>
    public float GetParameter(string key, float defaultValue = 0)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            return defaultValue;
        }
        return furnParameters[key];
    }

    public void SetParameter(string key, float value)
    {
        furnParameters[key] = value;
    }

    public void ChangeParameter(string key, float value)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            furnParameters[key] = value;
        }
        furnParameters[key] += value;
    }

    /// <summary>
    /// Register a function that will be called every update
    /// Later, this implementation may change with LUA support
    /// </summary>
    public void RegisterUpdateAction(Action<Furniture, float> a)
    {
        updateActions += a;
    }

    /// <summary>
    /// Unregister a function that will be called every update
    /// Later, this implementation may change with LUA support
    /// </summary>
    public void UnregisterUpdateAction(Action<Furniture, float> a)
    {
        updateActions -= a;
    }
    public int JobCount()
    {
        return jobs.Count;
    }

    public void AddJob(Job j)
    {
        jobs.Add(j);
        Tile.world.jobQueue.Enqueue(j);
    }

    public void RemoveJob(Job j)
    {
        jobs.Remove(j);
        j.CancelJob();
        Tile.world.jobQueue.Remove(j);
    }

    public void ClearJobs()
    {
        foreach(Job j in jobs)
        {
            RemoveJob(j);
        }
    }

    public bool IsStockpile()
    {
        return ObjectType == "Stockpile";
    }



    //////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// 
    ///                                 Saving And Loading
    ///
    ///
    //////////////////////////////////////////////////////////////////////////////////////////////


    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        // X, Y, and ObjectType alreay set, we should already be assigned to a tile
        // Just read extra data
        //movementCost = int.Parse(reader.GetAttribute("movementCost"));

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", Tile.X.ToString());
        writer.WriteAttributeString("Y", Tile.Y.ToString());
        writer.WriteAttributeString("ObjectType", ObjectType);
        //writer.WriteAttributeString("movementCost", movementCost.ToString());

        foreach (string k in furnParameters.Keys) {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParameters[k].ToString());
            writer.WriteEndElement();

        }
    }

}
