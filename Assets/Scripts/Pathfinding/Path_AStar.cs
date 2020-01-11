using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;

public class Path_AStar
{
    Queue<Tile> path;
    public Path_AStar(World world, Tile tileStart, Tile tileEnd)
    {
        // See if there is a valid tileGraph
        if (world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }
        // Dictionary of all valid walkable nodes
        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        // Make sure start and end tiles are in list of nodes
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Path_AStar: The starting tile isn't in list of nodes!");

            

            return;
        }
        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("Path_AStar: The ending tile isn't in list of nodes!");
            return;
        }

        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];

        // Mostly following this psuedocode:
        // https://en.wikipedia.org/wiki/A*_search_algorithm
        List<Path_Node<Tile>> ClosedSet = new List<Path_Node<Tile>>();
        
        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);

        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();
        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> node in nodes.Values)
        {
            g_score[node] = Mathf.Infinity;
        }
        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> node in nodes.Values)
        {
            f_score[node] = Mathf.Infinity;
        }
        f_score[start] = heuristic_cost_estimate(start, goal);

        while(OpenSet.Count > 0)
        {
            Path_Node<Tile> current = OpenSet.Dequeue();
            if (current == goal)
            {
                // Goal has been reached
                // Convert to sequence of tiles to walk on
                
                reconstruct_path(Came_From, current);
                return;
            }
            ClosedSet.Add(current);

            foreach(Path_Edge<Tile> edge_neighbor in current.edges)
            {
                Path_Node<Tile> neighbor = edge_neighbor.node;
                if (ClosedSet.Contains(neighbor) == true)
                {
                    continue; // ignore this already completed neighbor
                }

                float movement_cost_to_neighbor = neighbor.data.movementCost * dist_between(current, neighbor);

                float tentative_g_score = g_score[current] + movement_cost_to_neighbor;

                if (OpenSet.Contains(neighbor) && tentative_g_score >= g_score[neighbor])
                {
                    continue;
                }

                Came_From[neighbor] = current;
                g_score[neighbor] = tentative_g_score;
                f_score[neighbor] = g_score[neighbor] + heuristic_cost_estimate(neighbor, goal);

                if (OpenSet.Contains(neighbor) == false)
                {
                    OpenSet.Enqueue(neighbor, f_score[neighbor]);
                }
                else
                {
                    OpenSet.UpdatePriority(neighbor, f_score[neighbor]);
                }
            } // foreach neighbor
        } // while
        // If we reached here, we burned through entire openset
        // without ever reaching a point where current == goal
        // This happens when there is no path from start to goal
        // so there is a wall or missing floor or SOMETHING 

        // We dont have a failure state, maybe? 
        // The path list will be null
        
    }

    void reconstruct_path(Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current)
    {
        // At this point, current is the goal
        // So we want to walk backwards from the CameFrom map
        // until we have reached the end of that map
        // which will be our starting node
        Queue<Tile> total_path = new Queue<Tile>();
        total_path.Enqueue(current.data);
        while (Came_From.ContainsKey(current))
        {
            // Came_From is a map where the key => value relation
            // is really saying "some_node => we_got_there_from_this_node
            current = Came_From[current];
            total_path.Enqueue(current.data);
        }

        // At this point, total_path is a queue that 
        // is running backwards from the end tile
        // to the start tile so lets reverse it
        path = new Queue<Tile>(total_path.Reverse());
    }

    float heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
            );
    }

    float dist_between(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        // Make assumption because we know we're working 
        // on a grid at this point.

        // Hori/Vert neighbors have a distance of 1
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1)  // Check hori/vert adjacentcy
        {
            return 1.0f;
        }
             
        // Check diag adjacentcy

        // diag neighbors have a distance of 1.41421356237
        if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1.41421356237f;
        }
        // Otherwise, do the actual math
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
            );
    }

    public Tile Dequeue()
    {
        return path.Dequeue();
    }

    public int Length()
    {
        if (path == null)
        {
            return 0;
        }

        return path.Count;
    }
}
