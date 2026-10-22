using System.Collections.Generic;

public class BankersAlgorithm
{
    private int agvCount;
    private int resourceCount;

    // Resources currently available
    private int[] available;

    // Maximum resources each AGV may need
    private int[,] maximum;

    // Resources currently allocated to each AGV
    private int[,] allocation;

    // Remaining resources each AGV may still need
    private int[,] need;

    public void Initialize(int agvCount, int resourceCount)
    {
        this.agvCount = agvCount;
        this.resourceCount = resourceCount;

        available = new int[resourceCount];
        maximum = new int[agvCount, resourceCount];
        allocation = new int[agvCount, resourceCount];
        need = new int[agvCount, resourceCount];
    }

    public bool SetState(
        int[] availableResources,
        int[,] allocationState,
        int[,] maximumState,
        out string error)
    {
        error = "";

        if (availableResources == null ||
            allocationState == null ||
            maximumState == null)
        {
            error = "One or more state arrays are null.";
            return false;
        }

        for (int r = 0; r < resourceCount; r++)
        {
            if (availableResources[r] < 0)
            {
                error = "Available resource count cannot be negative.";
                return false;
            }

            available[r] = availableResources[r];
        }

        for (int a = 0; a < agvCount; a++)
        {
            for (int r = 0; r < resourceCount; r++)
            {
                int allocated = allocationState[a, r];
                int maximumRequired = maximumState[a, r];

                if (allocated < 0 || maximumRequired < 0)
                {
                    error = "Allocation/maximum values cannot be negative.";
                    return false;
                }

                if (allocated > maximumRequired)
                {
                    error =
                        "AGV " + a +
                        " has allocation greater than maximum demand.";

                    return false;
                }

                allocation[a, r] = allocated;
                maximum[a, r] = maximumRequired;
                need[a, r] = maximumRequired - allocated;
            }
        }

        return true;
    }

    public bool IsSafe(out List<int> safeSequence)
    {
        safeSequence = new List<int>();

        int[] work = new int[resourceCount];

        for (int r = 0; r < resourceCount; r++)
            work[r] = available[r];

        bool[] finished = new bool[agvCount];

        bool progress = true;

        while (progress)
        {
            progress = false;

            for (int agv = 0; agv < agvCount; agv++)
            {
                if (finished[agv])
                    continue;

                bool canFinish = true;

                for (int r = 0; r < resourceCount; r++)
                {
                    if (need[agv, r] > work[r])
                    {
                        canFinish = false;
                        break;
                    }
                }

                if (!canFinish)
                    continue;

                // Simulate AGV completion:
                // its currently allocated resources are released.
                for (int r = 0; r < resourceCount; r++)
                {
                    work[r] += allocation[agv, r];
                }

                finished[agv] = true;
                safeSequence.Add(agv);
                progress = true;
            }
        }

        for (int agv = 0; agv < agvCount; agv++)
        {
            if (!finished[agv])
                return false;
        }

        return true;
    }

    public bool TryRequest(
        int agvId,
        int[] request,
        out string message,
        out List<int> safeSequence)
    {
        message = "";
        safeSequence = null;

        if (agvId < 0 || agvId >= agvCount)
        {
            message = "Invalid AGV ID.";
            return false;
        }

        if (request == null ||
            request.Length != resourceCount)
        {
            message = "Invalid resource request.";
            return false;
        }

        // Request must not exceed remaining maximum need
        for (int r = 0; r < resourceCount; r++)
        {
            if (request[r] < 0)
            {
                message = "Request cannot contain negative values.";
                return false;
            }

            if (request[r] > need[agvId, r])
            {
                message =
                    "Request exceeds AGV " +
                    agvId +
                    "'s remaining maximum need.";

                return false;
            }
        }

        // Request must be currently available
        for (int r = 0; r < resourceCount; r++)
        {
            if (request[r] > available[r])
            {
                message =
                    "Resources are currently unavailable. AGV must wait.";

                return false;
            }
        }

        // Tentatively allocate resources
        for (int r = 0; r < resourceCount; r++)
        {
            available[r] -= request[r];
            allocation[agvId, r] += request[r];
            need[agvId, r] -= request[r];
        }

        // Check whether the tentative allocation is safe
        bool safe = IsSafe(out safeSequence);

        if (safe)
        {
            message =
                "Request GRANTED. System remains in a SAFE state.";

            return true;
        }

        // Unsafe -> rollback
        for (int r = 0; r < resourceCount; r++)
        {
            available[r] += request[r];
            allocation[agvId, r] -= request[r];
            need[agvId, r] += request[r];
        }

        message =
            "Request DENIED. Granting it would make the system UNSAFE.";

        safeSequence = null;

        return false;
    }

    public void ReleaseResources(
        int agvId,
        int[] released)
    {
        if (agvId < 0 || agvId >= agvCount)
            return;

        if (released == null ||
            released.Length != resourceCount)
            return;

        for (int r = 0; r < resourceCount; r++)
        {
            if (released[r] <= 0)
                continue;

            int amount =
                released[r] > allocation[agvId, r]
                    ? allocation[agvId, r]
                    : released[r];

            allocation[agvId, r] -= amount;
            available[r] += amount;
            need[agvId, r] += amount;
        }
    }

    public int[] GetAvailable()
    {
        int[] copy = new int[resourceCount];

        for (int r = 0; r < resourceCount; r++)
            copy[r] = available[r];

        return copy;
    }

    public int[] GetNeed(int agvId)
    {
        int[] result = new int[resourceCount];

        if (agvId < 0 || agvId >= agvCount)
            return result;

        for (int r = 0; r < resourceCount; r++)
            result[r] = need[agvId, r];

        return result;
    }

    public void CompleteProcess(int agvId)
{
    if (agvId < 0 || agvId >= agvCount)
        return;

    for (int r = 0; r < resourceCount; r++)
    {
        // Once an AGV's task is complete, it has
        // no remaining claims on future resources.
        maximum[agvId, r] = allocation[agvId, r];
        need[agvId, r] = 0;
    }
}
public void ReleaseAllResources(int agvId)
{
    if (agvId < 0 || agvId >= agvCount)
        return;

    for (int r = 0; r < resourceCount; r++)
    {
        available[r] += allocation[agvId, r];

        allocation[agvId, r] = 0;

        // Restore the remaining claim after backing off.
        need[agvId, r] = maximum[agvId, r];
    }
}
}