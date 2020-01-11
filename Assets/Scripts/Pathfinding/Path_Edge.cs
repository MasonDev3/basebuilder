using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_Edge<T> // Generic
{
    public float cost; // cost to travel along this edge (e.g. cost to ENTER the tile)

    public Path_Node<T> node;
}
