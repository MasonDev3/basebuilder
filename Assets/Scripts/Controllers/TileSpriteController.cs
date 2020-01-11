using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class TileSpriteController : MonoBehaviour
{
    Dictionary<Tile, GameObject> tileGameObjectMap;
    
    public static TileSpriteController instance { get; protected set; }

    public Sprite floorSprite;
    public Sprite emptySprite;

    World world
    {
        get { return WorldController.instance.world; }

    }

    // Start is called before the first frame update
    void Start()
    {
        tileGameObjectMap = new Dictionary<Tile, GameObject>();
        
        world.RegisterTileChanged(OnTileChanged);

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile_data = world.GetTileAt(x, y);
                // This creates a new GameObject and adds it to the scene
                GameObject tileGameObject = new GameObject();
                // Add to the dictionary
                tileGameObjectMap.Add(tile_data, tileGameObject);

                tileGameObject.name = "Tile_" + x + "_" + y;
                tileGameObject.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tileGameObject.transform.SetParent(this.transform, true);

                // Add SpriteRenderer and add default sprite for empty tiles
                SpriteRenderer sr = tileGameObject.AddComponent<SpriteRenderer>();
                sr.sprite = emptySprite;
                sr.sortingLayerName = "Tiles";

                OnTileChanged(tile_data);

            }
        }
    }
    
    // THIS IS AN EXAMPLE -- NOT IMPLEMENTED YET!!
    void DestroyAllTileGameObjects()
    {
        while (tileGameObjectMap.Count > 0)
        {
            Tile tile_data = tileGameObjectMap.Keys.First();
            GameObject tileGameObject = tileGameObjectMap[tile_data];

            tileGameObjectMap.Remove(tile_data);

            tile_data.UnregisterTileTypeChangedCallback(OnTileChanged);

            Destroy(tileGameObject);
        }
    }

    void OnTileChanged(Tile tile_data)
    {
        if (tileGameObjectMap.ContainsKey(tile_data) == false)
        {
            Debug.LogError("Doesn't contain the tile data. Did you forget to add tile to Dictionary? Or forget to unregister a callback?");
            return;
        }
        GameObject tileGameObject = tileGameObjectMap[tile_data];
        if (tileGameObject == null)
        {
            Debug.LogError("Returned GameObject is null!");
            return;
        }

        if (tile_data.Type == TileType.Floor)
        {
            tileGameObject.GetComponent<SpriteRenderer>().sprite = floorSprite;
            //Debug.Log("Tile type is floor");
        }
        else if (tile_data.Type == TileType.Empty)
        {
            //Debug.Log("Tile type is empty");
            tileGameObject.GetComponent<SpriteRenderer>().sprite = emptySprite;
        }
        else
        {
            Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
        }
    }
}
