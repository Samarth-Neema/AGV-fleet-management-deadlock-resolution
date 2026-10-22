
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // Resource ID -> AGV ID currently holding it
    private Dictionary<int, int> resourceOwners =
        new Dictionary<int, int>();

    public bool TryReserve(int resourceId, int agvId)
    {
        if (!resourceOwners.ContainsKey(resourceId))
        {
            resourceOwners.Add(resourceId, agvId);

            Debug.Log("AGV " + agvId +
                      " reserved Resource " + resourceId);

            return true;
        }

        return resourceOwners[resourceId] == agvId;
    }

    public int GetOwner(int resourceId)
    {
        if (resourceOwners.ContainsKey(resourceId))
            return resourceOwners[resourceId];

        return -1;
    }

    public void Release(int resourceId, int agvId)
    {
        if (resourceOwners.ContainsKey(resourceId) &&
            resourceOwners[resourceId] == agvId)
        {
            resourceOwners.Remove(resourceId);

            Debug.Log("AGV " + agvId +
                      " released Resource " + resourceId);
        }
    }

    public void ReleaseAllForAGV(int agvId)
    {
        List<int> resourcesToRelease = new List<int>();

        foreach (var entry in resourceOwners)
        {
            if (entry.Value == agvId)
                resourcesToRelease.Add(entry.Key);
        }

        foreach (int resourceId in resourcesToRelease)
            Release(resourceId, agvId);
    }

    public void ResetResources()
    {
        resourceOwners.Clear();
    }
}
