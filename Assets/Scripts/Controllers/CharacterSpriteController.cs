using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{
    Dictionary<Character, GameObject> characterMap;
    Dictionary<string, Sprite> characterSprites;

    World world
    {
        get { return WorldController.instance.world; }

    }
    // Start is called before the first frame update
    void Start()
    {
        LoadSprites();
        characterMap = new Dictionary<Character, GameObject>();
        world.RegisterCharacterCreated(OnCharacterCreated);
        
        // Check for pre-existing characters, which won't do callback
        foreach(Character character in world.characters)
        {
            OnCharacterCreated(character);
        }

    }

    void LoadSprites()
    {
        characterSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/");


        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            characterSprites[s.name] = s;
        }
    }

    public void OnCharacterCreated(Character character)
    {
        // Create a visual GameObject linked to this data
        GameObject character_gameObject = new GameObject();
        // Add to the dictionary

        characterMap.Add(character, character_gameObject);

        character_gameObject.name = "Character";
        character_gameObject.transform.position = new Vector3(character.X, character.Y, 0);
        character_gameObject.transform.SetParent(this.transform, true);

        // Add SpriteRenderer but don't set a sprite.
        // FIXME: Wall sprite is currently assumed but should not be
        SpriteRenderer sr = character_gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = characterSprites["p1_front"];
        sr.sortingLayerName = "Characters";
        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes
        character.RegisterCharacterChangedCallback(OnCharacterChanged);
    }

    void OnCharacterChanged(Character character)
    {
        // Make sure the furniture's graphics are correct
        if (characterMap.ContainsKey(character) == false)
        {
            Debug.LogError("Trying to change visuals for furniture not in our map!");
        }
        GameObject character_gameObject = characterMap[character];
        character_gameObject.transform.position = new Vector3(character.X, character.Y, 0);
        //character_gameObject.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(character);
        //character_gameObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
    }

    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        if (furn.linksToNeighbor == false)
        {
            return characterSprites[furn.ObjectType];

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
        if (characterSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("Installed Object Map does not contain an entry with that key name (" + spriteName + ")");
        }
        return characterSprites[spriteName];
    }


}
