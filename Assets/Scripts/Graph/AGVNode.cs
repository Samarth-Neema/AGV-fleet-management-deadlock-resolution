using System.Collections.Generic;
using UnityEngine;

public class AGVNode : MonoBehaviour
{
    public int nodeId;

    public List<AGVNode> neighbours = new List<AGVNode>();
}