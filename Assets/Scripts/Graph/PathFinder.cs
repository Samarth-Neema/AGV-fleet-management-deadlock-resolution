using System.Collections.Generic;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    public List<AGVNode> FindPath(AGVNode start, AGVNode goal)
    {
        Queue<AGVNode> queue = new Queue<AGVNode>();

        Dictionary<AGVNode, AGVNode> cameFrom =
            new Dictionary<AGVNode, AGVNode>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            AGVNode current = queue.Dequeue();

            if (current == goal)
                break;

            foreach (AGVNode neighbour in current.neighbours)
            {
                if (!cameFrom.ContainsKey(neighbour))
                {
                    cameFrom[neighbour] = current;
                    queue.Enqueue(neighbour);
                }
            }
        }

        // No path found
        if (!cameFrom.ContainsKey(goal))
        {
            Debug.LogWarning("No path found!");
            return new List<AGVNode>();
        }

        // Reconstruct path
        List<AGVNode> path = new List<AGVNode>();

        AGVNode node = goal;

        while (node != null)
        {
            path.Add(node);
            node = cameFrom[node];
        }

        path.Reverse();

        return path;
    }
}