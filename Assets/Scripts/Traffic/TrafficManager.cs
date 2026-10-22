using System.Collections.Generic;
using UnityEngine;

public class TrafficManager : MonoBehaviour
{
    public ResourceManager resourceManager;

    private WaitForGraph waitGraph = new WaitForGraph();

    public int deadlocksDetected { get; private set; }
    public int deadlocksResolved { get; private set; }

    public bool RequestResource(int resourceId, int agvId)
    {
        int owner = resourceManager.GetOwner(resourceId);

        // Resource is free or already owned by this AGV.
        if (resourceManager.TryReserve(resourceId, agvId))
        {
            // If this AGV was waiting for the previous owner,
            // remove that waiting relationship.
            if (owner != -1 && owner != agvId)
            {
                waitGraph.RemoveWait(agvId, owner);
            }

            Debug.Log(
                "AGV " + agvId +
                " acquired Resource " + resourceId
            );

            return true;
        }

        // Resource is occupied.
        if (owner != -1 && owner != agvId)
        {
            waitGraph.AddWait(agvId, owner);

            Debug.Log(
                "AGV " + agvId +
                " is WAITING for AGV " + owner
            );
        }

        return false;
    }

    public void ReleaseResource(int resourceId, int agvId)
    {
        resourceManager.Release(resourceId, agvId);
    }

    public List<int> DetectDeadlock()
    {
        List<int> cycle = waitGraph.FindDeadlockCycle();

        if (cycle != null)
        {
            deadlocksDetected++;

            return cycle;
        }

        return null;
    }

    public int SelectVictim(List<int> cycle)
    {
        int victim = -1;

        foreach (int agv in cycle)
        {
            if (agv > victim)
                victim = agv;
        }

        return victim;
    }

    public void ResolveDeadlock(int victimAGV)
    {
        resourceManager.ReleaseAllForAGV(victimAGV);

        waitGraph.RemoveAGV(victimAGV);

        deadlocksResolved++;

        Debug.Log(
            "AGV " + victimAGV +
            " released its reservations."
        );

        Debug.Log(
            "Deadlock resolution completed."
        );
    }

    public void ResetTraffic()
    {
        resourceManager.ResetResources();
        waitGraph.Clear();

        deadlocksDetected = 0;
        deadlocksResolved = 0;
    }

    public int GetResourceOwner(int resourceId)
    {
        return resourceManager.GetOwner(resourceId);
    }
}