using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
    public static WorldController instance { get; protected set; }

    public World world { get; protected set; }
    static bool loadWorld = false;


    // Start is called before the first frame update
    void OnEnable()
    {        
        if (instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        instance = this;
        // Create world with empty tiles
        if (loadWorld)
        {
            loadWorld = false;
            CreateWorldFromSave();
        }
        else
        {
            CreateEmptyWorld();
        }
    }

    void Update()
    {
        //TODO: Add pause/resume, speed controls, etc...
        world.Update(Time.deltaTime);
    }
    
    /// <summary>
    /// Creates a new EMPTY world
    /// </summary>
    void CreateEmptyWorld()
    {
        // Create a new world, with all EMPTY tiles
        world = new World(100, 100);

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    void CreateWorldFromSave()
    {
        Debug.Log("CreateWorldFromSave");
        // Create a world from save file data


        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(PlayerPrefs.GetString("SaveGame00"));

        world = (World)serializer.Deserialize(reader);
        reader.Close();

        

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return WorldController.instance.world.GetTileAt(x, y);
    }

    public void SaveWorld()
    {
        Debug.Log("Save World button clicked");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, world);
        writer.Close();

        Debug.Log(writer.ToString());

        PlayerPrefs.SetString("SaveGame00", writer.ToString());
    }

    public void LoadWorld()
    {
        Debug.Log("Load World button clicked");
        // Reload the scene to reset all data (purge old references)
        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }

    public void NewWorld()
    {
        Debug.Log("New World button clicked");
        // Implement destruction of old world
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);        
    }
    
}
