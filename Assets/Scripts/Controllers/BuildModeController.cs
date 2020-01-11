using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class BuildModeController : MonoBehaviour
{


    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;
    bool buildModeIsObject = false;

    FurnitureSpriteController fsc;
    GameObject furniturePreview;

    MouseController mouseController;

    // Start is called before the first frame update
    void Start()
    {
        mouseController = GameObject.FindObjectOfType<MouseController>();
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();
        if (furniturePreview == null)
        {
            furniturePreview = new GameObject();
            furniturePreview.transform.SetParent(this.transform);
            furniturePreview.AddComponent<SpriteRenderer>().sortingLayerName = "Jobs";
            furniturePreview.SetActive(false);
        }

    }

    void Update()
    {
        if (buildModeIsObject == true && buildModeObjectType != null && buildModeObjectType != "")
        {
            //! Show a transparent preview of the object that is color-coded based 
            //! on whether or not you can actually set it there
            ShowFurnitureSpriteAtTile(buildModeObjectType, mouseController.GetMouseOverTile());

        }
    }

    public bool IsObjectDraggable()
    {
        if (buildModeIsObject == false)
        {
            //! Floors are draggable
            return true;
        }
        Furniture prototype = WorldController.instance.world.furniturePrototypes[buildModeObjectType];

        return ((prototype.Width == 1) && (prototype.Height == 1));

    }

    void ShowFurnitureSpriteAtTile(string furnitureType, Tile tile)
    {
        furniturePreview.SetActive(true);
        SpriteRenderer sr = furniturePreview.GetComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(furnitureType);

        if (WorldController.instance.world.IsFurniturePlacementValid(furnitureType, tile))
        {
            sr.color = new Color(0.5f, 0.5f, 1.0f, 0.25f);
        }
        else
        {
            sr.color = new Color(1.0f, 0.5f, 0.5f, 0.25f);
        }
        Furniture prototype = tile.world.furniturePrototypes[furnitureType];

        furniturePreview.transform.position = new Vector3(tile.X + ((prototype.Width - 1) / 2f), tile.Y + ((prototype.Height - 1) / 2f), 0);

    }


    //public void 

    public void SetMode_BuildFloor()
    {
        buildModeIsObject = false;
        buildModeTile = TileType.Floor;
    }

    public void SetMode_Bulldoze()
    {
        buildModeIsObject = false;
        buildModeTile = TileType.Empty;
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        // Wall is not a TileType! Wall is an "InstalledObject" that is placed on top of a tile!
        buildModeIsObject = true;
        buildModeObjectType = objectType;
    }

    public void SetMode_PathfindingExample()
    {
        WorldController.instance.world.SetupPathfindingExample();

    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObject == true)
        {
            // Create the InstalledObject and assign to tile

            //WorldController.instance.world.PlaceFurniture(buildModeObjectType, t);
            // Check to see if we are allowed to build in this tile
            string furnitureType = buildModeObjectType;
            if (WorldController.instance.world.IsFurniturePlacementValid(furnitureType, t) && t.pendingFurnitureJob == null)
            {
                // This tile position is valid for this piece of furniture
                Job job;
                
                if (WorldController.instance.world.furnitureJobPrototypes.ContainsKey(furnitureType))
                {
                    // Make a clone of job prototype
                    job = WorldController.instance.world.furnitureJobPrototypes[furnitureType].Clone();
                    // Assign correct tile
                    job.tile = t;
                }
                else
                {
                    Debug.LogError("There is no furnitureJobPrototype for '" + furnitureType + "'");
                    job = new Job(t, furnitureType, FurnitureActions.JobComplete_FurnitureBuilding, 0.1f, null);

                }

                job.furniturePrototype = WorldController.instance.world.furniturePrototypes[furnitureType]; 

                t.pendingFurnitureJob = job;
                job.RegisterJobCancelCallback((theJob) => { theJob.tile.pendingFurnitureJob = null; });
                WorldController.instance.world.jobQueue.Enqueue(job);

            }
        }
        else
        {
            t.Type = buildModeTile;
        }

    }

    
}
