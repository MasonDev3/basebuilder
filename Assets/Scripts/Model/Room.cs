using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public float atmosO2 = 0;
    public float atmosN = 0;
    public float atmosCO2 = 0;

    List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
    }

    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t))
        {
            return;
        }

        if (t.room != null)
        {
            t.room.tiles.Remove(t);
        }

        t.room = this;

        tiles.Add(t);
    }

    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom();
        }
        tiles = new List<Tile>();
    }

    /// <summary>
    /// Flood room to see if it is enclosed
    /// </summary>
    /// <param name="sourceFurniture">Piece of furniture that may be
    /// splitting two existing rooms, or may be the final enclosing piece to form
    ///  a new room</param>
    public static void RoomFloodFill(Furniture sourceFurniture)
    {
        World world = sourceFurniture.Tile.world;

        Room oldRoom = sourceFurniture.Tile.room;

        // Try building new rooms for each of our NESW directions
        foreach(Tile t in sourceFurniture.Tile.GetNeighbors())
        {
            ActualFloodFill(t, oldRoom);
        }


        sourceFurniture.Tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.Tile);

        // Check NESW neighbors of furniture tiles 
        // and do flood fill from them
       
        if (oldRoom != world.GetOutsideRoom())
        {
            // oldRoom shouldn't have any more tiles left in it
            // in practice this "DeleteRoom" should need to remove
            // the room from the world's list

            if (oldRoom.tiles.Count > 0)
            {
                Debug.LogError("Old room still has tiles assigned to it.");
            }

            world.DeleteRoom(oldRoom); 
        }



    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // We are trying to flood fill off the map, so just return
            // without doing anything
            return;
        }

        if (tile.room != oldRoom)
        {
            // This tile was already assigned to another "new" room,
            // which means that the direction picked isn't isolated. So we
            // can just return without creating a new room
            return;
        }

        if (tile.furniture != null && tile.furniture.roomEnclosure)
        {
            // This tile has a wall/door/etc in it, we can't do a room here
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            // This tile is empty space and must remain part of the outside
            return;
        }

        // If we get to this point, we know that we need to create a new room.
        Room newRoom = new Room();

        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            

            if (t.room == oldRoom)
            {
                newRoom.AssignTile(t);

                Tile[] neighbors = t.GetNeighbors();
                foreach(Tile t2 in neighbors)
                {
                    if (t2 == null || t2.Type == TileType.Empty)
                    {
                        // We have hit open space, either the edge of the map or empty tile
                        // This room we're building is actually part of the outside
                        // We can immediately end the flood fill 
                        // We need to delete this "newRoom" and re-assign all tiles to outside
                        newRoom.UnAssignAllTiles();
                        return;
                    }
                    if (t2.room == oldRoom && (t2.furniture == null || t2.furniture.roomEnclosure == false))
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }
            }
        }
        // Copy data from old room to new room
        newRoom.atmosCO2 = oldRoom.atmosCO2;
        newRoom.atmosN = oldRoom.atmosN;
        newRoom.atmosO2 = oldRoom.atmosO2;

        // Tell the world that a new room has been formed.
        tile.world.AddRoom(newRoom);
    } 
}
