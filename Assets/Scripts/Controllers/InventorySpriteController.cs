using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySpriteController : MonoBehaviour
{
    Dictionary<Inventory, GameObject> inventoryMap;
    Dictionary<string, Sprite> inventorySprites;

    public GameObject inventoryUIPrefab;

    World world
    {
        get { return WorldController.instance.world; }

    }
    // Start is called before the first frame update
    void Start()
    {
        LoadSprites();
        inventoryMap = new Dictionary<Inventory, GameObject>();
        world.RegisterInventoryCreated(OnInventoryCreated);
        
        // Check for pre-existing characters, which won't do callback
        foreach(string objectType in world.inventoryManager.inventories.Keys)
        {
            foreach(Inventory inv in world.inventoryManager.inventories[objectType])
            {
                OnInventoryCreated(inv);

            }
        }

    }

    void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Inventory/");


        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            inventorySprites[s.name] = s;
        }
    }

    public void OnInventoryCreated(Inventory inv)
    {
        // Create a visual GameObject linked to this data
        GameObject inventory_gameObject = new GameObject();
        // Add to the dictionary

        inventoryMap.Add(inv, inventory_gameObject);

        inventory_gameObject.name = inv.objectType;
        inventory_gameObject.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
        inventory_gameObject.transform.SetParent(this.transform, true);

        if (inv.maxStackSize > 1)
        {
            // This is a stackable object
            // Let's add an InventoryUI component which is text
            // that shows current stack size
            GameObject ui_gameObject = Instantiate(inventoryUIPrefab);
            ui_gameObject.transform.SetParent(inventory_gameObject.transform);
            // If we change the sprite anchor, this may need to be modified
            ui_gameObject.transform.localPosition = Vector3.zero;
            ui_gameObject.GetComponentInChildren<Text>().text = inv.stackSize.ToString();
        }

        // Add SpriteRenderer but don't set a sprite.
        // FIXME: Wall sprite is currently assumed but should not be
        SpriteRenderer sr = inventory_gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = inventorySprites[inv.objectType];
        sr.sortingLayerName = "Inventory";
        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes
        // FIXME: Add onchanged callbacks
        //inv.RegisterCharacterChangedCallback(OnCharacterChanged);
        inv.RegisterInventoryChangedCallback(OnInventoryChanged);
    }

    void OnInventoryChanged(Inventory inventory)
    {
        // FIXME: Still needs to work and get called
        // Make sure the furniture's graphics are correct
        if (inventoryMap.ContainsKey(inventory) == false)
        {
            Debug.LogError("Trying to change visuals for furniture not in our map!");
        }
        GameObject inventory_gameObject = inventoryMap[inventory];
        if (inventory.stackSize > 0)
        {
            Text text = inventory_gameObject.GetComponentInChildren<Text>();
            //TODO: If maxStackSize changed to/from 1, we need to create/destroy the text respectivelly
            if (text != null)
            {
                text.text = inventory.stackSize.ToString();
            }
        }
        else 
        {
            // This stack has gone to zero
            // Remove the sprite
            Destroy(inventory_gameObject);
            inventoryMap.Remove(inventory);
            inventory.UnregisterInventoryChangedCallback(OnInventoryChanged);
        }
        
    }

    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        if (furn.linksToNeighbor == false)
        {
            return inventorySprites[furn.ObjectType];

        }

        string spriteName = furn.ObjectType + "_";

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
        if (inventorySprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("Installed Object Map does not contain an entry with that key name (" + spriteName + ")");
        }
        return inventorySprites[spriteName];
    }


}
