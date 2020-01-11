using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_Node<T> // Generic class
{
    public T data;

    public Path_Edge<T>[] edges; // Nodes leading out from this node.


}
