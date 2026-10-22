using System.Collections.Generic;
using UnityEngine;

public class AGVAgent : MonoBehaviour
{
    [Header("Identity")]
    public int agvId;

    [Header("Task")]
    public AGVNode startNode;
    public AGVNode destinationNode;

    [Header("Current State")]
    public AGVNode currentNode;

    public string state = "IDLE";

    [Header("Route")]
    public List<AGVNode> currentPath = new List<AGVNode>();

    public bool HasDestination
    {
        get
        {
            return destinationNode != null;
        }
    }

    public bool HasPath
    {
        get
        {
            return currentPath != null &&
                   currentPath.Count > 0;
        }
    }
}