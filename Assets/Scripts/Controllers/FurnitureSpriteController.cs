using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureSpriteController : MonoBehaviour
{
    #region Variable Declarations

    Dictionary<Furniture, GameObject> furnitureMap;
    Dictionary<string, Sprite> furnitureSprites;
    public static FurnitureSpriteController instance { get; protected set; }


    World world
    {
        get { return WorldController.instance.world; }

    }
    #endregion

    #region Startup Calls
    // Start is called before the first frame update
    void Start()
    {
        LoadSprites();
        // Initialize dictionary to track which GameObject is rendering which Tile data.
        furnitureMap = new Dictionary<Furniture, GameObject>();
        // Register the callback so the GameObject gets updated whenever the tile's type changes
        world.RegisterFurnitureCreated(OnFurnitureCreated);

        // Go through any existing furniture (from a save that was loaded) and call the OnCreated event manually.
        foreach(Furniture furn in world.furnitures)
        {
            OnFurnitureCreated(furn);
        }
    }
    /// <summary>
    /// Load all furniture sprites into the "sprites" array
    /// </summary>
    void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");


        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
           // Debug.Log(s);
            furnitureSprites[s.name] = s;
        }
    }

    #endregion


    

    #region Public Method Definitions

    public void OnFurnitureCreated(Furniture furn)
    {

        // FIXME: Does not consider multi-tile objects nor rotated objects

        // Create a visual GameObject linked to this data
        GameObject furn_gameObject = new GameObject();

        

        // Add to the dictionary
        
        furnitureMap.Add(furn, furn_gameObject);

        furn_gameObject.name = furn.ObjectType + "_" + furn.Tile.X + "_" + furn.Tile.Y;
        furn_gameObject.transform.position = new Vector3(furn.Tile.X + ((furn.Width - 1) / 2f), furn.Tile.Y + ((furn.Height - 1) / 2f), 0);
        furn_gameObject.transform.SetParent(this.transform, true);

        //TODO: HARDCODING
        if (furn.ObjectType == "Door")
        {
            // by default, door graphic is meant for wall to be east and west
            // check to see if walls are north south
            // if so, rotate by 90 degrees
            Tile southTile = world.GetTileAt(furn.Tile.X, furn.Tile.Y - 1);
            Tile northTile = world.GetTileAt(furn.Tile.X, furn.Tile.Y + 1);

            if (northTile != null && southTile != null && northTile.furniture != null && southTile.furniture != null &&
                northTile.furniture.ObjectType == "Wall" && southTile.furniture.ObjectType == "Wall")
            {
                furn_gameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        // Add SpriteRenderer but don't set a sprite.
        // FIXME: Wall sprite is currently assumed but should not be
        SpriteRenderer sr = furn_gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteForFurniture(furn);
        sr.sortingLayerName = "Furniture";
        sr.color = furn.tint;

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes
        furn.RegisterOnChangedCallback(OnFurnitureChanged);

        
    }

    void OnFurnitureChanged(Furniture furn)
    {
        // Make sure the furniture's graphics are correct
        if (furnitureMap.ContainsKey(furn) == false)
        {
            Debug.LogError("Trying to change visuals for furniture not in our map!");
        }
        GameObject furn_gameObject = furnitureMap[furn];
        furn_gameObject.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
        furn_gameObject.GetComponent<SpriteRenderer>().color = furn.tint;
    }

    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        string spriteName = furn.ObjectType;
        if (furn.linksToNeighbor == false)
        {
            // If this is a door, lets check openness
            // FIXME: All hard coding will need to be generalized
            if (furn.ObjectType == "Door")
            {
                if (furn.GetParameter("openness") < 0.1f)
                {
                    // Door is closed
                    spriteName = "Door";
                }
                else if (furn.GetParameter("openness") < 0.5f)
                {
                    // Door is a bit open
                    spriteName = "Door_openness_1";
                }
                else if (furn.GetParameter("openness") < 0.9f)
                {
                    // Door is almost all the way open
                    spriteName = "Door_openness_2";
                }
                else
                {
                    // Door is fully open
                    spriteName = "Door_openness_3";
                }
            }

            return furnitureSprites[spriteName];

        }

        spriteName = furn.ObjectType + "_";

        int x = furn.Tile.X;
        int y = furn.Tile.Y;
        // Check for neighbors, N E S W
        Tile t;
        t = world.GetTileAt(x, y + 1);
        if (t != null && t.furniture != null && t.furniture.ObjectType == furn.ObjectType)
        {
            spriteName += "N";
        }
        t = world.GetTileAt(x + 1, y);
        if (t != null && t.furniture != null && t.furniture.ObjectType == furn.ObjectType)
        {
            spriteName += "E";
        }
        t = world.GetTileAt(x, y - 1);
        if (t != null && t.furniture != null && t.furniture.ObjectType == furn.ObjectType)
        {
            spriteName += "S";
        }
        t = world.GetTileAt(x - 1, y);
        if (t != null && t.furniture != null && t.furniture.ObjectType == furn.ObjectType)
        {
            spriteName += "W";
        }

        // If this object has all four neighbors of same type, then the string 
        // will look like: Wall_NESW


        // Otherwise the sprite name is more complicated
        if (furnitureSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("Installed Object Map does not contain an entry with that key name (" + spriteName + ")");
        }

        

        return furnitureSprites[spriteName];
    }

    public Sprite GetSpriteForFurniture(string objectType)
    {
        if (furnitureSprites.ContainsKey(objectType))
        {
            return furnitureSprites[objectType];
        }

        if (furnitureSprites.ContainsKey(objectType + "_"))
        {
            return furnitureSprites[objectType + "_"];
        }

        return null;

    }


    #endregion
}
