using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_TileGraph
{
    // This class constructs a simple pathfinding compatible graph
    // of our world. Every tile is a node and each WALKABLE neighbor
    // from a tile is linked by an edge connection

    public Dictionary<Tile, Path_Node<Tile>> nodes;
    
    public Path_TileGraph(World world)
    {
        Debug.Log("Path_TileGraph");
        // Loop through all tiles of world.
        // For each tile create a node.
        // Create nodes for non-floor tiles? NO
        // Create nodes for tiles that are unwalkable (e.g. walls)? NO

        nodes = new Dictionary<Tile, Path_Node<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile t = world.GetTileAt(x, y);

                //if (t.movementCost > 0) // Tiles with movementCost of 0 are unwalkable
                //{
                //    Path_Node<Tile> node = new Path_Node<Tile>();
                //    node.data = t;
                //    nodes.Add(t, node);
                //}
                Path_Node<Tile> node = new Path_Node<Tile>();
                node.data = t;
                nodes.Add(t, node);
            }
        }

        Debug.Log("Created " + nodes.Count + " nodes");

        // Loop through a second time and create edges for the neighbors
        int edgeCount = 0;
        foreach(Tile t in nodes.Keys)
        {
            Path_Node<Tile> node = nodes[t];

            List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();
            // Get a list of neighbors for the tile
            Tile[] neighbors = t.GetNeighbors(true); // NOTE: Some array spots could be null (e.g. building on edge of map)
            // If walkable, create edge to relavent node
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && neighbors[i].movementCost > 0)
                {
                    // This neighbor exists and is walkable
                    // Create an edge

                    // But first, make sure we aren't clipping a diagonal or trying to squeeze inappropriately

                    if (IsClippingCorner(t, neighbors[i]))
                    {
                        continue; // Skip to next neighbor
                    }

                    Path_Edge<Tile> edge = new Path_Edge<Tile>();
                    edge.cost = neighbors[i].movementCost;
                    edge.node = nodes[neighbors[i]];
                    // Add edge to temporary (and growable) list
                    edges.Add(edge);

                    edgeCount++;
                }
            }

            node.edges = edges.ToArray();
        }

        Debug.Log("Created " + edgeCount + " edges");


    }
    /// <summary>
    /// Finds if the path finding algorithm will allow 
    /// diagonal movement by determining if the path will
    /// cut (or clip) a corner
    /// </summary>
    /// <param name="current">Current Tile we are on</param>
    /// <param name="neighbor">Our immediate neighbor tile</param>
    /// <returns>True if clipping a corner, false if not</returns>
    bool IsClippingCorner(Tile current, Tile neighbor)
    {
        // If movement from current to neighbor is diagonal (i.e. NE)
        // Check to make sure we aren't clipping (e.g. N and E are both walkable)

        if (Mathf.Abs(current.X - neighbor.X) + Mathf.Abs(current.Y - neighbor.Y) == 2)
        {
            // We are diagonal!
            int diffX = current.X - neighbor.X;
            int diffY = current.Y - neighbor.Y;

            if (current.world.GetTileAt(current.X - diffX, current.Y).movementCost == 0)
            {
                // Then east or west is unwalkable
                return true;
            }
            if (current.world.GetTileAt(current.X, current.Y - diffY).movementCost == 0)
            {
                // Then north or south is unwalkable
                return true;
            }
        }

        return false;

    }
}
