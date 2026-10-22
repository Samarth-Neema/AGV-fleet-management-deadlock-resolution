
using System.Collections.Generic;

public class WaitForGraph
{
    // AGV -> set of AGVs blocking it
    private Dictionary<int, HashSet<int>> graph =
        new Dictionary<int, HashSet<int>>();

    public void AddWait(int waitingAGV, int blockingAGV)
    {
        if (!graph.ContainsKey(waitingAGV))
            graph[waitingAGV] = new HashSet<int>();

        graph[waitingAGV].Add(blockingAGV);
    }

    public void RemoveAGV(int agvId)
    {
        graph.Remove(agvId);

        foreach (var neighbours in graph.Values)
            neighbours.Remove(agvId);
    }

    public void Clear()
    {
        graph.Clear();
    }

    public List<int> FindDeadlockCycle()
    {
        HashSet<int> visited = new HashSet<int>();
        HashSet<int> recursionStack = new HashSet<int>();
        List<int> path = new List<int>();

        foreach (int agv in graph.Keys)
        {
            if (DetectCycle(agv, visited, recursionStack, path))
                return new List<int>(path);
        }

        return null;
    }

    private bool DetectCycle(
        int current,
        HashSet<int> visited,
        HashSet<int> recursionStack,
        List<int> path)
    {
        if (recursionStack.Contains(current))
        {
            path.Add(current);
            return true;
        }

        if (visited.Contains(current))
            return false;

        visited.Add(current);
        recursionStack.Add(current);
        path.Add(current);

        if (graph.ContainsKey(current))
        {
            foreach (int neighbour in graph[current])
            {
                if (DetectCycle(
                    neighbour, visited, recursionStack, path))
                    return true;
            }
        }

        recursionStack.Remove(current);
        path.RemoveAt(path.Count - 1);

        return false;
    }


    public void RemoveWait(int waitingAGV, int blockingAGV)
{
    if (graph.ContainsKey(waitingAGV))
    {
        graph[waitingAGV].Remove(blockingAGV);

        if (graph[waitingAGV].Count == 0)
        {
            graph.Remove(waitingAGV);
        }
    }
}
public void ClearWaitsFrom(int waitingAGV)
{
    // Remove this AGV's outgoing wait relationships
    // after its request succeeds or its waiting state ends.
    graph.Remove(waitingAGV);
}
}
