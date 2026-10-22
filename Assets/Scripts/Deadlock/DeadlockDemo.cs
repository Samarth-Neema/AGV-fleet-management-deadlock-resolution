using System.Collections.Generic;
using UnityEngine;

public class DeadlockDemo : MonoBehaviour
{
    public ResourceManager resourceManager;

    private WaitForGraph waitGraph = new WaitForGraph();

    private void Start()
    {
        // Automatically run the test when Play Mode starts.
        Invoke(nameof(RunControlledDeadlockTest), 0.5f);
    }

    public void RunControlledDeadlockTest()
    {
        if (resourceManager == null)
        {
            Debug.LogError("Assign ResourceManager first!");
            return;
        }

        resourceManager.ResetResources();
        waitGraph.Clear();

        Debug.Log("========== DEADLOCK TEST START ==========");

        // AGV 1 holds Resource 0
        resourceManager.TryReserve(0, 1);

        // AGV 2 holds Resource 1
        resourceManager.TryReserve(1, 2);

        // AGV 3 holds Resource 2
        resourceManager.TryReserve(2, 3);

        // Circular waiting
        RequestResource(1, 1);
        RequestResource(2, 2);
        RequestResource(0, 3);

        List<int> cycle = waitGraph.FindDeadlockCycle();

        if (cycle != null)
        {
            Debug.Log("DEADLOCK DETECTED!");

            string cycleText = "";

            foreach (int agv in cycle)
            {
                cycleText += agv + " -> ";
            }

            Debug.Log("Wait-for cycle: " + cycleText);

            ResolveDeadlock(cycle);
        }
        else
        {
            Debug.Log("No deadlock detected.");
        }

        Debug.Log("========== DEADLOCK TEST END ==========");
    }

    private void RequestResource(int resourceId, int requestingAGV)
    {
        int owner = resourceManager.GetOwner(resourceId);

        if (resourceManager.TryReserve(resourceId, requestingAGV))
        {
            Debug.Log(
                "AGV " + requestingAGV +
                " acquired Resource " + resourceId
            );
        }
        else if (owner != -1)
        {
            waitGraph.AddWait(requestingAGV, owner);

            Debug.Log(
                "AGV " + requestingAGV +
                " is WAITING for AGV " + owner
            );
        }
    }

    private void ResolveDeadlock(List<int> cycle)
    {
        int victimAGV = -1;

        foreach (int agv in cycle)
        {
            if (agv > victimAGV)
                victimAGV = agv;
        }

        Debug.Log(
            "Resolution: AGV " + victimAGV +
            " selected to back off."
        );

        resourceManager.ReleaseAllForAGV(victimAGV);
        waitGraph.RemoveAGV(victimAGV);

        Debug.Log(
            "AGV " + victimAGV +
            " released its reservations."
        );

        Debug.Log("DEADLOCK RESOLVED.");
    }
}