using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AGVFleetSpawner : MonoBehaviour
{
    [Header("Fleet Settings")]
    public int fleetSize = 30;

    [Header("AGV Template")]
    public GameObject agvTemplate;

    [Header("Generated Nodes")]
    public Transform nodeRoot;

    [Header("Spawn Settings")]
    public int startingNodeOffset = 0;

    public bool useUniqueStartNodes = true;

    private readonly List<GameObject> spawnedAGVs =
        new List<GameObject>();

    public List<AGVAgent> GetFleet()
{
    List<AGVAgent> fleet =
        new List<AGVAgent>();

    // First use the runtime list if available.
    foreach (GameObject agv in spawnedAGVs)
    {
        if (agv == null)
            continue;

        AGVAgent agent =
            agv.GetComponent<AGVAgent>();

        if (agent != null)
            fleet.Add(agent);
    }

    // If the list is empty, discover the AGVs
    // that already exist under this spawner.
    if (fleet.Count == 0)
    {
        AGVAgent[] agents =
            GetComponentsInChildren<AGVAgent>(
                true
            );

        foreach (AGVAgent agent in agents)
        {
            if (agent != null)
                fleet.Add(agent);
        }
    }

    fleet.Sort(
        (a, b) =>
            a.agvId.CompareTo(b.agvId)
    );

    return fleet;
}

    [ContextMenu("Spawn Fleet")]
    public void SpawnFleet()
    {
        if (agvTemplate == null)
        {
            Debug.LogError(
                "AGVFleetSpawner: AGV Template is not assigned."
            );

            return;
        }

        Transform root = GetNodeRoot();

        if (root == null)
        {
            Debug.LogError(
                "AGVFleetSpawner: GeneratedNodes not found."
            );

            return;
        }

        ClearFleet();

        AGVNode[] nodes =
            root.GetComponentsInChildren<AGVNode>(
                true
            );

        if (nodes.Length < fleetSize)
        {
            Debug.LogError(
                "Not enough generated nodes. Found " +
                nodes.Length +
                ", required " +
                fleetSize
            );

            return;
        }

        // Sort nodes by ID.
        System.Array.Sort(
            nodes,
            (a, b) =>
                a.nodeId.CompareTo(b.nodeId)
        );

        for (int i = 0; i < fleetSize; i++)
{
    AGVNode start =
        nodes[
            (startingNodeOffset + i)
            % nodes.Length
        ];

    int destinationIndex =
        (i * 7 + 63)
        % nodes.Length;

    if (destinationIndex == start.nodeId)
    {
        destinationIndex =
            (destinationIndex + 1)
            % nodes.Length;
    }

    AGVNode destination =
        FindNodeById(
            nodes,
            destinationIndex
        );

    GameObject agv =
        Instantiate(
            agvTemplate,
            start.transform.position,
            Quaternion.identity,
            transform
        );

    agv.name =
        "AGV_" +
        (i + 1).ToString("00");

    AGVAgent agent =
        agv.GetComponent<AGVAgent>();

    if (agent == null)
    {
        agent =
            agv.AddComponent<AGVAgent>();
    }

    agent.agvId = i + 1;
    agent.startNode = start;
    agent.currentNode = start;
    agent.destinationNode = destination;
    agent.currentPath.Clear();

    AGVController controller =
        agv.GetComponent<AGVController>();

    if (controller != null)
    {
        controller.enabled = false;
    }

    agv.SetActive(true);

    spawnedAGVs.Add(agv);
}

        Debug.Log(
            "AGV Fleet created successfully. Fleet Size: " +
            spawnedAGVs.Count
        );
    }

    private Transform GetNodeRoot()
    {
        if (nodeRoot != null)
            return nodeRoot;

        GameObject generated =
            GameObject.Find("GeneratedNodes");

        if (generated != null)
            return generated.transform;

        return null;
    }

    private AGVNode FindNodeById(
        AGVNode[] nodes,
        int id)
    {
        foreach (AGVNode node in nodes)
        {
            if (node.nodeId == id)
                return node;
        }

        return nodes[0];
    }

    [ContextMenu("Clear Fleet")]
    public void ClearFleet()
    {
        for (int i =
             spawnedAGVs.Count - 1;
             i >= 0;
             i--)
        {
            if (spawnedAGVs[i] != null)
            {
                DestroyImmediateIfNeeded(
                    spawnedAGVs[i]
                );
            }
        }

        spawnedAGVs.Clear();

        // Remove any generated AGVs that were
        // previously created but are not in our list.
        for (int i =
             transform.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                transform.GetChild(i);

            if (child.name.StartsWith("AGV_"))
            {
                DestroyImmediateIfNeeded(
                    child.gameObject
                );
            }
        }
    }

    private void DestroyImmediateIfNeeded(
        GameObject target)
    {
        if (target == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(target);
        else
            Destroy(target);
#else
        Destroy(target);
#endif
    }
}