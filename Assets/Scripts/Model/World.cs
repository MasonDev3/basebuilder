using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable
{
    Tile[,] tiles;
    public List<Character> characters;
    public List<Furniture> furnitures;
    public List<Room>      rooms;
    public InventoryManager inventoryManager;
    /// <summary>
    /// The pathfinding graph used to navigate in the world map
    /// </summary>
    public Path_TileGraph tileGraph;


    public Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> furnitureJobPrototypes;
    public int Width { get; protected set; }

    public int Height { get; protected set; }

    Action<Furniture> cbFurnitureCreated;
    Action<Tile> cbTileChanged;
    Action<Character> cbCharacterCreated;
    Action<Inventory> cbInventoryCreated;

    /// <summary>
    /// Most likely this will be replaced with a dedicated
    /// class for managing job queue's that might also
    /// be semi-static or self initializing
    /// </summary>
    public JobQueue jobQueue;

    /// <summary>
    /// Initialize new instance of <see cref="World"/> class
    /// </summary>
    /// <param name="width">Width of world in tiles</param>
    /// <param name="height">Height of world in tiles</param>
    public World(int width, int height)
    {
        // Creates empty world
        SetupWorld(width, height);

        // Make one character
        CreateCharacter(GetTileAt(Width / 2, Height / 2));

    }
    /// <summary>
    /// Default constructor used when loading a world from file.
    /// </summary>
    public World()
    {

    }

    /// <summary>
    /// World setup function to be called in <see cref="World"/>
    /// </summary>
    /// <param name="width">Width of the world in tiles</param>
    /// <param name="height">Height of the world in tiles</param>
    void SetupWorld(int width, int height)
    {
        jobQueue = new JobQueue();
        Width = width;
        Height = height;
        characters  = new List<Character>();
        furnitures  = new List<Furniture>();
        inventoryManager = new InventoryManager();
        rooms       = new List<Room>();
        rooms.Add(new Room()); // Create the outside

        tiles = new Tile[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
                tiles[x, y].room = GetOutsideRoom(); // Rooms 0 is always going to be "outside"
                                             // that is our default room
            }
        }

        CreateFurniturePrototypes();

    }

    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public void AddRoom(Room r)
    {
        rooms.Add(r);
    }

    public void DeleteRoom(Room r)
    {
        if (r == GetOutsideRoom())
        {
            Debug.LogError("Tried to delete outside room");
            return;
        }
        // Remove this room from our rooms list
        rooms.Remove(r);

        // All tiles that belong to this room should be reassigned to
        // the outside
        r.UnAssignAllTiles();

    }

    /// <summary>
    /// Create prototypes of furniture pieces.
    /// This will be replaced by a function later that reads all
    /// furniture data from a text file (XML/JSON)
    /// </summary>
    void CreateFurniturePrototypes()
    {
        furniturePrototypes = new Dictionary<string, Furniture>();
        furnitureJobPrototypes = new Dictionary<string, Job>();

        furniturePrototypes.Add("Wall",
            new Furniture(
                "Wall",
                0,      // Impassable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                true    // Enclose rooms
            )
        );
        furnitureJobPrototypes.Add("Wall",
            new Job(
                null,
                "Wall",
                FurnitureActions.JobComplete_FurnitureBuilding,
                1.0f,
                new Inventory[] { new Inventory("Steel Plate", 5, 0) }
            )
        );

        furniturePrototypes.Add("Door",
            new Furniture(
                "Door",
                1,      // Door pathfinding cost, normal speed
                1,      // Width
                1,      // Height
                false,  // Links to neighbors and "sort of" becomes part of a large object
                true    // Enclose rooms
            )
        );

        // What if the object behaviors were scriptable? And then were part of the text file
        // we are reading in?
        furniturePrototypes["Door"].SetParameter("openness", 0);
        furniturePrototypes["Door"].SetParameter("is_opening", 0);
        furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction);
        furniturePrototypes["Door"].isEnterable = FurnitureActions.Door_IsEnterable;


        furniturePrototypes.Add("Stockpile",
            new Furniture(
                "Stockpile",
                1,      // Passable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                false    // Enclose rooms
            )
        );
        furniturePrototypes["Stockpile"].RegisterUpdateAction(FurnitureActions.Stockpile_UpdateAction);
        furniturePrototypes["Stockpile"].tint = new Color32(229, 95, 94, 255);
        furnitureJobPrototypes.Add("Stockpile",
            new Job(
                null,
                "Stockpile",
                FurnitureActions.JobComplete_FurnitureBuilding,
                -1,
                null
            )
        );

        furniturePrototypes.Add("Oxygen Generator",
            new Furniture(
                "Oxygen Generator",
                10,      // Door pathfinding cost, normal speed
                2,      // Width
                2,      // Height
                false,  // Links to neighbors and "sort of" becomes part of a large object
                false    // Enclose rooms
            )
        );
    }

    void OnTileChanged(Tile t)
    {
        if (cbTileChanged == null)
        {
            return;
        }
        cbTileChanged(t);
        InvalidateTileGraph();
    }

    public void Update(float deltaTime)
    {
        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }
        foreach (Furniture f in furnitures)
        {
            f.Update(deltaTime);
        }
    }

    public Character CreateCharacter(Tile t)
    {
        Character c = new Character(t);
        characters.Add(c);

        cbCharacterCreated?.Invoke(c);
        return c;
    }

    /// <summary>
    ///  Call this whenever a change to the world
    /// means that old pathfinding info is now invalid
    /// </summary>
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }
    /// <summary>
    /// A test function only used for debugging
    /// </summary>
    public void RandomizeTiles()
    {
        Debug.Log("RandomizeTiles!");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    tiles[x, y].Type = TileType.Empty;
                }
                else
                {
                    tiles[x, y].Type = TileType.Floor;
                }
            }
        }
    }

    /// <summary>
    /// Sample function to make a quick test map for pathfinding
    /// </summary>
    public void SetupPathfindingExample()
    {
        Debug.Log("SetupPathfindingExample -- Loaded");

        // Make floors and walls to test pathfinding with
        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;

                if (x == 1 || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceFurniture("Wall", tiles[x, y]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the x, y coords of a tile
    /// </summary>
    /// <param name="x">X position of tile</param>
    /// <param name="y">Y position of tile</param>
    /// <returns>Tile[x, y]</returns>
    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0)
        {
            return null;
        }
        return tiles[x, y];
    }

    public Furniture PlaceFurniture(string objectType, Tile t)
    {

        //TODO: This function assumes 1x1 tiles -- change this later!

        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("installedObjectPrototypes doesn't contain a prototype for key: " + objectType);
            return null;
        }

        // Check to see if the TileType of the current tile is empty
        // If it is empty, it is not a legal building tile and we bail out of the function

        Furniture furn = Furniture.PlaceInstance(furniturePrototypes[objectType], t);

        if (furn == null)
        {
            // Failed to place object -- bail out of creating tile
            return null;
        }

        furnitures.Add(furn);

        // Do we need to recalculate our rooms?
        if (furn.roomEnclosure)
        {
            Room.RoomFloodFill(furn);
        }

        if (cbFurnitureCreated != null)
        {
            cbFurnitureCreated(furn);
            if (furn.movementCost != 1)
            {
                // Tiles return movementCost as their base cost multiplied by
                // furniture movement cost, a furniture movement cose
                // of exactly 1 doesn't impact pathfinding
                InvalidateTileGraph();
            }

        }

        return furn;
    }

    public void RegisterFurnitureCreated(Action<Furniture> callbackFunc)
    {
        cbFurnitureCreated += callbackFunc;
    }

    public void UnRegisterFurnitureCreated(Action<Furniture> callbackFunc)
    {
        cbFurnitureCreated -= callbackFunc;
    }

    public void RegisterCharacterCreated(Action<Character> callbackFunc)
    {
        cbCharacterCreated += callbackFunc;
    }

    public void UnRegisterCharacterCreated(Action<Character> callbackFunc)
    {
        cbCharacterCreated -= callbackFunc;
    }

    public void RegisterInventoryCreated(Action<Inventory> callbackFunc)
    {
        cbInventoryCreated += callbackFunc;
    }

    public void UnRegisterInventoryCreated(Action<Inventory> callbackFunc)
    {
        cbInventoryCreated -= callbackFunc;
    }

    public void RegisterTileChanged(Action<Tile> callbackFunc)
    {
        cbTileChanged += callbackFunc;
    }

    public void UnRegisterTileChanged(Action<Tile> callbackFunc)
    {
        cbTileChanged -= callbackFunc;
    }

    public bool IsFurniturePlacementValid(string furnitureType, Tile t)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(t);
    }

    public Furniture GetFurniturePrototype(string furnitureType)
    {
        if (furniturePrototypes.ContainsKey(furnitureType) == false)
        {
            Debug.LogError("No furniture of " + furnitureType);
        }
        return furniturePrototypes[furnitureType];
    }

    public void OnInventoryCreated(Inventory inv)
    {
        cbInventoryCreated?.Invoke(inv);
    }

    //////////////////////////////////////////////////////////////////////////////
    ///
    ///                         SAVING & LOADING
    ///
    //////////////////////////////////////////////////////////////////////////////

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        // Save info here
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x,y].Type != TileType.Empty)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Furnitures");
        foreach(Furniture furn in furnitures)
        {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        writer.WriteStartElement("Characters");
        foreach (Character c in characters)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        //writer.WriteStartElement("Width");
        //writer.WriteValue(Width);
        //writer.WriteEndElement();



    }

    public void ReadXml(XmlReader reader)
    {
        Debug.Log("World::ReadXml");
        // Load info here

        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));

        SetupWorld(Width, Height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Tiles":
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures":
                    ReadXml_Furnitures(reader);
                    break;
                case "Characters":
                    ReadXml_Characters(reader);
                    break;
            }
        }
        // DEBUGGING ONLY! REMOVE ME
        // Create inventory item.
        Tile placementTile = GetTileAt(Width / 2, Height / 2);
        Inventory inv = new Inventory("Steel Plate", 50, 50);
        inventoryManager.PlaceInventory(placementTile, inv);
        cbInventoryCreated?.Invoke(placementTile.inventory);

        placementTile = GetTileAt((Width / 2) + 2, Height / 2);
        inv = new Inventory("Steel Plate", 50, 50);
        inventoryManager.PlaceInventory(placementTile, inv);
        cbInventoryCreated?.Invoke(placementTile.inventory);

        placementTile = GetTileAt((Width / 2) + 1, (Height / 2) - 2);
        inv = new Inventory("Steel Plate", 50, 50);
        inventoryManager.PlaceInventory(placementTile, inv);
        cbInventoryCreated?.Invoke(placementTile.inventory);

    }

    void ReadXml_Tiles(XmlReader reader)
    {
        // We are in the "Tiles" elemenet, so read elements until
        // we run out of "Tile" nodes.

        if (reader.ReadToDescendant("Tile"))
        {
            do {
                // We have at least one tile, DO SOMETHING
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                // Read XML for any other data
                tiles[x, y].ReadXml(reader);
            } while (reader.ReadToNextSibling("Tile"));
        }


    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        // We are in the "Furnitures" element, so read elements until
        // we run out of "Furniture" nodes.

        if (reader.ReadToDescendant("Furniture"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));


                Furniture furn = PlaceFurniture(reader.GetAttribute("ObjectType"), tiles[x, y]);
                // Read XML for any other data
                furn.ReadXml(reader);
            } while (reader.ReadToNextSibling("Furniture"));
        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        // We are in the "Characters" element, so read elements until
        // we run out of "Character" nodes.

        if (reader.ReadToDescendant("Character"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Character character = CreateCharacter(tiles[x, y]);
                // Read XML for any other data
                character.ReadXml(reader);
            } while (reader.ReadToNextSibling("Character"));
        }
    }
}
