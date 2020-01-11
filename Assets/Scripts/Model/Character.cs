using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable
{
    public float X
    {
        get
        {
            return Mathf.Lerp(CurrentTile.X, nextTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(CurrentTile.Y, nextTile.Y, movementPercentage);
        }
    }

    public Tile CurrentTile { get; protected set; }

    Tile _destTile;

    /// <summary>
    /// If we aren't moving, destTile == currentTile
    /// </summary>
    Tile destTile
    {
        get
        {
            return _destTile;
        }
        set
        {
            if (_destTile != value)
            {
                _destTile = value;
                // If this is a new destination, invalidate pathfinding
                pathAStar = null;
            }
        }
    }
    Tile nextTile;
    Path_AStar pathAStar;
    float movementPercentage; // Goes from 0 to 1 as we move from currentTile to destTile
    float speed = 5.0f; // Tiles per second

    Action<Character> cbCharacterChanged;
    Job myJob;
    /// <summary>
    /// A singular item we carry (not our gear/backpack/equipment/etc.)
    /// </summary>
    public Inventory inventory;

    public Character()
    {
        // Use only for serialization
        // Otherwise bad bad things happen
    }

    public Character(Tile tile)
    {
        CurrentTile = destTile = nextTile = tile;
    }

    void GetNewJob()
    {
        // Grab a new job
        myJob = CurrentTile.world.jobQueue.Dequeue();
        if (myJob == null)
        {
            return;
        }
        destTile = myJob.tile;
        myJob.RegisterJobCancelCallback(OnJobEnded);
        myJob.RegisterJobCompleteCallback(OnJobEnded);

        // Immediately check to see if job tile is reachable
        // NOTE We might not be pathing to it right away, since we 
        // need materials, but we still need to verify that the final location
        // can be reached
        pathAStar = new Path_AStar(CurrentTile.world, CurrentTile, destTile); // This will calculate a path from current tile to destination tile
        if (pathAStar.Length() == 0)
        {
            Debug.LogError("Path_AStar returned no path to target job tile");
            // cancel job? FIXME: job should re-enqueue instead?
            AbandonJob();
            destTile = CurrentTile;
        }
    }

    void Update_DoJob(float deltaTime)
    {
        // Do I have a job?
        if (myJob == null)
        {

            GetNewJob();

            if (myJob == null)
            {
                // There was no job on the queue so just return
                destTile = CurrentTile;
                return;
            }
        }
        // We have a job now! ( And reachable )
        // STEP 1: Does the job have all the required materials?
        if (myJob.HasAllMaterial() == false)
        {
            // No, we are missing something
            // STEP 2: Are we CARRYING anything that the job location wants?
            if (inventory != null)
            {
                if (myJob.DesiredInventoryAmount(inventory) > 0)
                {
                    // If so, deliver the goods
                    //  Walk to job tile, drop off the stack into the job
                    if (CurrentTile == myJob.tile)
                    {
                        // We are at job site so drop inventory
                        CurrentTile.world.inventoryManager.PlaceInventory(myJob, inventory);
                        myJob.DoWork(0); // This will call all cbJobWorked call backs, becuase even though
                                        // we are progressing, it might want to do something with the fact 
                                        // that the requirements are being met.
                        
                        // Are we still carrying things?
                        if (inventory.stackSize == 0)
                        {
                            inventory = null;
                        }
                        else
                        {
                            Debug.LogError("Character still carrying inventory, which shouldn't be. Just setting to NULL for now, but this means that we are LEAKING inventory");
                            inventory = null;
                        }
                    }
                    else
                    {
                        // Still need to walk to job site
                        destTile = myJob.tile;
                        return;
                    }
                }
                else
                {
                    // We are carrying something but the job doesn't want it
                    // Dump the inventory at our feet (or wherever is closest)
                    //TODO: Actually, walk to the nearest empty tile and dump it there
                    if (CurrentTile.world.inventoryManager.PlaceInventory(CurrentTile, inventory) == false)
                    {
                        Debug.LogError("Character tried to dump inventory into an invalid tile. Maybe there is already something here?");
                        //! For the sake of continuing on, we are still going to dump any reference to 
                        //! the current inventory. We are still leaking inventory. This is permanently lost for now
                        inventory = null; 
                    }
                }
            }
            else
            {
                // At this point, the job still requires inventory but we arent carrying it

                //? Are we standing on a tile with goods that are desired in a job?
                if (CurrentTile.inventory != null && 
                    (myJob.canTakeFromStockpile || 
                    CurrentTile.furniture == null || CurrentTile.furniture.IsStockpile() == false) &&
                    myJob.DesiredInventoryAmount(CurrentTile.inventory) > 0)
                {
                    //! Pick up the stuff!
                    CurrentTile.world.inventoryManager.PlaceInventory(this,
                        CurrentTile.inventory, 
                        myJob.DesiredInventoryAmount(CurrentTile.inventory)
                    );
                }
                else
                {
                    // Walk towards the tile containing the requred material.

                    // Find the first thing in job that isnt satisfied
                    Inventory desired = myJob.GetFirstDesiredInventory();

                    Inventory supplier = CurrentTile.world.inventoryManager.GetClosestInventoryOfType(
                        desired.objectType,
                        CurrentTile,
                        desired.maxStackSize - desired.stackSize,
                        myJob.canTakeFromStockpile
                        );

                    if (supplier == null)
                    {
                        Debug.Log("No tile contains objects of type '" + desired.objectType + "' to satisfy job requirements");
                        AbandonJob();
                        return;
                    }
                    destTile = supplier.tile;
                    return;
                }

                //      If already on that tile, pick up the material
            }
            return; // We cant continue until all materials are satisfied
        }
        // If we are here, the job has all materials needed 

        // Lets make sure our destination tile is job tile
        destTile = myJob.tile;

        // See if we are there yet

        if (CurrentTile == myJob.tile)
        {
            // We are at the right tile for our job
            // execute the jobs do work function
            // which is mostly going to countdown jobTime and 
            // call its jobCompleteCallback.
            myJob.DoWork(deltaTime);       
        }
        // Nothing left to do, we need Update_DoMovement to get
        // us where we want to go
       
    }

    public void AbandonJob()
    {
        nextTile = destTile = CurrentTile;
        CurrentTile.world.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    void Update_DoMovement(float deltaTime)
    {
        if (CurrentTile == destTile)
        {
            pathAStar = null;
            return; // We are already where we want to be
        }

        // CurrentTile = the tile i am currently in and may be in the process of leaving
        // nextTile = the tile I am currently entering
        // destTile = final destination, we never walk here directly it is used for the pathfinding

        if (nextTile == null || nextTile == CurrentTile)
        {
            // Get next tile from pathfinder
            if (pathAStar == null || pathAStar.Length() == 0)
            {
                // Generate a path to destination
                pathAStar = new Path_AStar(CurrentTile.world, CurrentTile, destTile); // This will calculate a path from current tile to destination tile
                if (pathAStar.Length() == 0)
                {
                    Debug.LogError("Path_AStar returned no path to destination");
                    // cancel job? FIXME: job should re-enqueue instead?

                    AbandonJob();
                    return;
                }
                // Let's ignore the first tile, because that's the tile we're currently in
                nextTile = pathAStar.Dequeue();
            }
            // Grab the next waypoint (tile) from the pathfinding system
            nextTile = pathAStar.Dequeue();

            if (nextTile == CurrentTile)
            {
                Debug.LogError("Update_DoMovement - nextTile is CurrentTile?");
            }
        }

        //if (pathAStar.Length() == 1)
        //{
        //    return;
        //}
        // Total distance from A to B
        // We are going to use Pythagorean distance FOR NOW...
        // When we switch to the pathfinding system, we will likely
        // switch to something like Manhattan or Chebyshev distance
        float distToTravel = Mathf.Sqrt(Mathf.Pow(CurrentTile.X - nextTile.X, 2) + Mathf.Pow(CurrentTile.Y - nextTile.Y, 2));

        if (nextTile.IsEnterable() == ENTERABILITY.Never)
        {
            // Most likely a wall got built so we just need to reset pathfinding info
            // FIXME: when a wall gets spawned, we should invalidate path immediately
            //        so we dont spend time walking towards a dead end. To save CPU,
            //        maybe only check so often or use a callback?
            Debug.LogError("FIXME: A character was trying to enter an unwalkable tile. ERROR: Cannot divide by 0 (movementCost)");
            nextTile = null; // Our next tile is a no-go
            pathAStar = null; // clearly our pathfinding info is out of date
            return;
        }
        else if (nextTile.IsEnterable() == ENTERABILITY.Soon)
        {
            // We can't enter the tile now but we should be able to
            // This is likely a DOOR
            // So we dont bail on our path, we return now and don't 
            // process the movement
            return;
        }
        // How far can we travel in this update?
        float distThisFrame = speed / nextTile.movementCost * deltaTime;
        // How much is that in terms of percentage to the destination?
        float percThisFrame = distThisFrame / distToTravel;
        // Add that to overall percentage travelled
        movementPercentage += percThisFrame;

        if (movementPercentage >= 1)
        {
            // We have reached our destination

            //TODO: Get next tile from pathfinding system
            // If there are no more tiles, then we have actually


            CurrentTile = nextTile;
            movementPercentage = 0;
            // FIXME? Do we want to preserve overshot movement?
        }
    }

    public void Update(float deltaTime)
    {

        Update_DoJob(deltaTime);

        Update_DoMovement(deltaTime);

        
        cbCharacterChanged?.Invoke(this);
    }

    public void SetDestination(Tile tile)
    {
        if (CurrentTile.IsNeighbor(tile, true) == false)
        {
            Debug.Log("Character::SetDestination -- Our destination tile isn't our neighbor");
        }


        destTile = tile;
    }

    public void RegisterCharacterChangedCallback(Action<Character> callback)
    {
        cbCharacterChanged += callback;
    }
    public void UnregisterCharacterChangedCallback(Action<Character> callback)
    {
        cbCharacterChanged -= callback;
    }

    void OnJobEnded(Job j)
    {
        // Job completed or cancelled

        j.UnregisterJobCancelCallback(OnJobEnded);
        j.UnregisterJobCompleteCallback(OnJobEnded);

        if (j != myJob)
        {
            Debug.LogError("Character being told about job that isn't his. You forgot to unregister something.");
            return;
        }

        myJob = null;
    }

    /////////////////////////////////////////////////////////////////////////////////
    ///
    ///
    ///                         Saving and Loading
    ///
    ///
    /////////////////////////////////////////////////////////////////////////////////

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", CurrentTile.X.ToString());
        writer.WriteAttributeString("Y", CurrentTile.Y.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        // DO NOTHING at this point
    }

    

}
