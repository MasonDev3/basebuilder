using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class MouseController : MonoBehaviour
{
    Vector3 lastFramePosition;
    Vector3 dragStartPosition;
    Vector3 currFrameMousePosition;
    List<GameObject> dragPreviewGameObjects;    
    public GameObject circleCursorPrefab;
    BuildModeController buildController;

    // Start is called before the first frame update
    void Start()
    {
        buildController = FindObjectOfType<BuildModeController>();

        dragPreviewGameObjects = new List<GameObject>();
    }

    /// <summary>
    /// Gets the mouse position in world space
    /// </summary>
    public Vector3 GetMousePosition()
    {
        return currFrameMousePosition;
    }
    
    public Tile GetMouseOverTile()
    {
        return WorldController.instance.GetTileAtWorldCoord(currFrameMousePosition);
    }

    // Update is called once per frame
    void Update()
    {
        currFrameMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFrameMousePosition.z = 0;

        //UpdateCursor();
        UpdateTileDragging();
        UpdateCameraMovement();

        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
        
    }
    
    
    void UpdateTileDragging()
    {
        // If over UI element, bailout
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }


        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currFrameMousePosition;
        }

        if (buildController.IsObjectDraggable() == false)
        {
            dragStartPosition = currFrameMousePosition;
        }

        // Add 0.5f to offset for "center" pivot position
        int start_x = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int end_x   = Mathf.FloorToInt(currFrameMousePosition.x + 0.5f);
        int start_y = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int end_y   = Mathf.FloorToInt(currFrameMousePosition.y + 0.5f);

        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        } 
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        // Clean up old drag previews
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject dragPreview = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(dragPreview);
        }

        if (Input.GetMouseButton(0))
        {
            // Display a preview of drag area
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.instance.world.GetTileAt(x, y);
                    if (t != null)
                    {
                        // Display the building hint on top of this tile position.
                        GameObject dragPreview = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                        dragPreview.transform.SetParent(this.transform, true);
                        SpriteRenderer sr = dragPreview.GetComponent<SpriteRenderer>();
                        sr.sortingLayerName = "TileUI";
                        dragPreviewGameObjects.Add(dragPreview);
                    }
                }

            }
        }
        // End drag
        if (Input.GetMouseButtonUp(0))
        {

            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.instance.world.GetTileAt(x, y);
                    if (t != null)
                    {
                        // Call BuildModeController::DoBuild()
                        buildController.DoBuild(t);
                    }
                }

            }
        }
    }

    void UpdateCameraMovement()
    {
        // Check to see if the right or middle mouse buttons are down
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 diff = lastFramePosition - currFrameMousePosition;
            Camera.main.transform.Translate(diff);
        }

        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
        
    }
}
