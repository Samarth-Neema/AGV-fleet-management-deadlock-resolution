using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetMovementPreview : MonoBehaviour
{
    [Header("References")]
    public AGVFleetSpawner fleetSpawner;
    public AGVGraph graph;

    [Header("Movement")]
    public float speed = 0.65f;
    public float waitAtDestination = 0.5f;

    [Header("Preview Settings")]
    public bool startOnPlay = false;

    private PathFinder pathFinder;
    private bool isRunning;
    private int activeMoves;

    private void Awake()
    {
        pathFinder = GetComponent<PathFinder>();

        if (pathFinder == null)
        {
            pathFinder = gameObject.AddComponent<PathFinder>();
        }
    }

    private void Start()
    {
        if (startOnPlay)
        {
            StartPreview();
        }
    }

    public void StartPreview()
    {
        if (isRunning)
            return;

        StartCoroutine(RunFleet());
    }

    private IEnumerator RunFleet()
    {
        isRunning = true;
        activeMoves = 0;

        if (fleetSpawner == null)
        {
            fleetSpawner =
                FindFirstObjectByType<AGVFleetSpawner>();
        }

        if (graph == null)
        {
            graph = FindFirstObjectByType<AGVGraph>();
        }

        if (fleetSpawner == null || graph == null)
        {
            Debug.LogError(
                "FleetMovementPreview: Assign the Fleet Spawner " +
                "and AGV Graph."
            );

            isRunning = false;
            yield break;
        }

        // Wait one frame so scene initialization can finish.
        yield return null;

        List<AGVAgent> fleet = fleetSpawner.GetFleet();

        if (fleet == null || fleet.Count == 0)
        {
            Debug.LogError(
                "FleetMovementPreview: No AGVs found. " +
                "Stop Play Mode and use Spawn Fleet first."
            );

            isRunning = false;
            yield break;
        }

        Debug.Log(
            "========== AGV MOVEMENT PREVIEW =========="
        );

        Debug.Log("Fleet size: " + fleet.Count);
        Debug.Log("Graph nodes: " + graph.nodes.Count);

        foreach (AGVAgent agent in fleet)
        {
            if (agent == null)
                continue;

            activeMoves++;

            StartCoroutine(MoveAGV(agent));

            // Stagger the starts slightly.
            yield return new WaitForSeconds(0.03f);
        }

        while (activeMoves > 0)
        {
            yield return null;
        }

        Debug.Log("========== MOVEMENT PREVIEW COMPLETE ==========");

        isRunning = false;
    }

    private IEnumerator MoveAGV(AGVAgent agent)
    {
        if (agent.currentNode == null ||
            agent.destinationNode == null)
        {
            Debug.LogWarning(
                "AGV " + agent.agvId +
                " has no valid start or destination."
            );

            activeMoves--;
            yield break;
        }

        List<AGVNode> path = pathFinder.FindPath(
            agent.currentNode,
            agent.destinationNode
        );

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning(
                "AGV " + agent.agvId +
                ": No path found."
            );

            activeMoves--;
            yield break;
        }

        agent.currentPath = new List<AGVNode>(path);
        agent.state = "MOVING";

        Debug.Log(
            "AGV " + agent.agvId +
            ": Node " + agent.currentNode.nodeId +
            " -> Node " + agent.destinationNode.nodeId
        );

        for (int i = 1; i < path.Count; i++)
        {
            AGVNode nextNode = path[i];

            yield return MoveTo(
                agent.transform,
                nextNode.transform.position
            );

            agent.currentNode = nextNode;
        }

        agent.state = "COMPLETED";

        Debug.Log(
            "AGV " + agent.agvId +
            " reached Node " + agent.destinationNode.nodeId
        );

        yield return new WaitForSeconds(waitAtDestination);

        activeMoves--;
    }

    private IEnumerator MoveTo(Transform agv, Vector3 target)
    {
        while (
            Vector3.Distance(agv.position, target) > 0.03f)
        {
            agv.position = Vector3.MoveTowards(
                agv.position,
                target,
                speed * Time.deltaTime
            );

            yield return null;
        }

        agv.position = target;
    }
}