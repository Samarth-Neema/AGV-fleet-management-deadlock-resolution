using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSimulation : MonoBehaviour
{
    [Header("AGV References")]
    public Transform agv1;
    public Transform agv2;
    public Transform agv3;

    [Header("Graph")]
    public AGVGraph graph;

    [Header("Important Nodes")]
    public AGVNode node0;
    public AGVNode node1;
    public AGVNode node2;
    public AGVNode node3;
    public AGVNode node5;
    public AGVNode node6;
    public AGVNode node8;
    public AGVNode safeNode;

    [Header("Manager")]
    public TrafficManager trafficManager;

    [Header("Movement")]
    public float speed = 2f;

    public string Status { get; private set; } = "READY";

    private PathFinder pathFinder;

    private void Awake()
    {
        pathFinder = gameObject.GetComponent<PathFinder>();

        if (pathFinder == null)
        {
            pathFinder = gameObject.AddComponent<PathFinder>();
        }

        ResetPositions();
    }

    // =========================================================
    //                      PUBLIC CONTROLS
    // =========================================================

    public void StartNormalTraffic()
    {
        StopAllCoroutines();

        trafficManager.ResetTraffic();
        ResetPositions();

        Status = "NORMAL";

        StartCoroutine(RunNormalTraffic());
    }

    public void TriggerDeadlock()
    {
        StopAllCoroutines();

        trafficManager.ResetTraffic();
        ResetPositions();

        Status = "DEADLOCK";

        StartCoroutine(RunDeadlockScenario());
    }

    public void ResetSimulation()
    {
        StopAllCoroutines();

        trafficManager.ResetTraffic();
        ResetPositions();

        Status = "READY";

        Debug.Log("Simulation reset.");
    }

    // =========================================================
    //                      NORMAL TRAFFIC
    // =========================================================

    private IEnumerator RunNormalTraffic()
    {
        Debug.Log("====================================");
        Debug.Log("        NORMAL TRAFFIC START");
        Debug.Log("====================================");

        Debug.Log("AGV 1: Node 0 -> Node 2");
        Debug.Log("AGV 2: Node 6 -> Node 8");
        Debug.Log("AGV 3: Node 3 -> Node 5");

        // Start all three routes simultaneously.
        Coroutine route1 = StartCoroutine(
            MoveAGVWithReservations(
                agv1,
                1,
                node2
            )
        );

        Coroutine route2 = StartCoroutine(
            MoveAGVWithReservations(
                agv2,
                2,
                node8
            )
        );

        Coroutine route3 = StartCoroutine(
            MoveAGVWithReservations(
                agv3,
                3,
                node5
            )
        );

        yield return route1;
        yield return route2;
        yield return route3;

        Status = "NORMAL";

        Debug.Log("====================================");
        Debug.Log("       NORMAL TRAFFIC COMPLETE");
        Debug.Log("====================================");
    }

    private IEnumerator MoveAGVWithReservations(
        Transform agv,
        int agvId,
        AGVNode targetNode)
    {
        AGVNode startNode =
            FindClosestNode(agv.position);

        List<AGVNode> path =
            pathFinder.FindPath(
                startNode,
                targetNode
            );

        if (path.Count == 0)
        {
            Debug.LogError(
                "No path found for AGV " + agvId
            );

            yield break;
        }

        for (int i = 1; i < path.Count; i++)
        {
            AGVNode nextNode = path[i];

            // Ask TrafficManager for the next resource.
            while (
                !trafficManager.RequestResource(
                    nextNode.nodeId,
                    agvId
                ))
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Move to the reserved node.
            yield return MoveToPosition(
                agv,
                nextNode.transform.position
            );

            // Release the node after leaving it
            // so another AGV can use it.
            trafficManager.ReleaseResource(
                nextNode.nodeId,
                agvId
            );

            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log(
            "AGV " + agvId +
            " completed its task."
        );
    }

    // =========================================================
    //                   DEADLOCK SCENARIO
    // =========================================================

    private IEnumerator RunDeadlockScenario()
    {
        Debug.Log("====================================");
        Debug.Log("       DEADLOCK TEST START");
        Debug.Log("====================================");

        Debug.Log(
            "AGV 1 started at Node 0"
        );

        Debug.Log(
            "AGV 2 started at Node 1"
        );

        Debug.Log(
            "AGV 3 started at Node 2"
        );

        yield return new WaitForSeconds(1f);

        // Each AGV holds one resource.
        trafficManager.RequestResource(0, 1);
        trafficManager.RequestResource(1, 2);
        trafficManager.RequestResource(2, 3);

        Debug.Log("Resources reserved.");

        yield return new WaitForSeconds(1f);

        // Create circular waiting.
        trafficManager.RequestResource(1, 1);
        trafficManager.RequestResource(2, 2);
        trafficManager.RequestResource(0, 3);

        Debug.Log("Circular waiting created.");

        yield return new WaitForSeconds(1f);

        List<int> cycle =
            trafficManager.DetectDeadlock();

        if (cycle == null)
        {
            Status = "ERROR";

            Debug.LogError(
                "Deadlock was not detected."
            );

            yield break;
        }

        Debug.Log("************************************");
        Debug.Log("        DEADLOCK DETECTED");
        Debug.Log("************************************");

        Debug.Log(
            "Cycle: AGV 1 -> AGV 2 -> AGV 3 -> AGV 1"
        );

        yield return new WaitForSeconds(2f);

        // Select victim.
        int victim =
            trafficManager.SelectVictim(cycle);

        Debug.Log(
            "AGV " + victim +
            " selected for safe backoff."
        );

        // Move AGV 3 to safety.
        yield return MoveToPosition(
            agv3,
            safeNode.transform.position
        );

        Debug.Log(
            "AGV 3 reached Safe Buffer Node " +
            safeNode.nodeId
        );

        // Release AGV 3 resources.
        trafficManager.ResolveDeadlock(victim);

        yield return new WaitForSeconds(1f);

        // AGV 2 continues.
        Debug.Log(
            "AGV 2 continues to Node 2."
        );

        trafficManager.RequestResource(2, 2);

        yield return MoveToPosition(
            agv2,
            node2.transform.position
        );

        trafficManager.ReleaseResource(2, 2);
        trafficManager.ReleaseResource(1, 2);

        yield return new WaitForSeconds(1f);

        // AGV 1 continues.
        Debug.Log(
            "AGV 1 continues to Node 1."
        );

        trafficManager.RequestResource(1, 1);

        yield return MoveToPosition(
            agv1,
            node1.transform.position
        );

        trafficManager.ReleaseResource(1, 1);
        trafficManager.ReleaseResource(0, 1);

        yield return new WaitForSeconds(1f);

        Status = "RESOLVED";

        Debug.Log("====================================");
        Debug.Log(
            "Deadlocks Detected: " +
            trafficManager.deadlocksDetected
        );

        Debug.Log(
            "Deadlocks Resolved: " +
            trafficManager.deadlocksResolved
        );

        Debug.Log(
            "        DEADLOCK RESOLVED"
        );

        Debug.Log("====================================");
    }

    // =========================================================
    //                        MOVEMENT
    // =========================================================

    private IEnumerator MoveToPosition(
        Transform agv,
        Vector3 target)
    {
        while (
            Vector3.Distance(
                agv.position,
                target
            ) > 0.05f)
        {
            agv.position = Vector3.MoveTowards(
                agv.position,
                target,
                speed * Time.deltaTime
            );

            yield return null;
        }
    }

    // =========================================================
    //                      RESET POSITIONS
    // =========================================================

    private void ResetPositions()
    {
        if (agv1 != null && node0 != null)
            agv1.position = node0.transform.position;

        if (agv2 != null && node1 != null)
            agv2.position = node1.transform.position;

        if (agv3 != null && node2 != null)
            agv3.position = node2.transform.position;
    }

    // =========================================================
    //                   FIND CLOSEST NODE
    // =========================================================

    private AGVNode FindClosestNode(
        Vector3 position)
    {
        AGVNode closest = null;

        float shortestDistance =
            Mathf.Infinity;

        foreach (AGVNode node in graph.nodes)
        {
            float distance =
                Vector2.Distance(
                    position,
                    node.transform.position
                );

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = node;
            }
        }

        return closest;
    }
}