using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetTrafficController : MonoBehaviour
{
    [Header("References")]
    public AGVFleetSpawner fleetSpawner;
    public AGVGraph graph;
    public ResourceManager resourceManager;

    [Header("Shared Bottleneck Nodes")]
    public int[] bottleneckNodeIds = { 44, 45, 54, 55 };

    [Header("Movement")]
    public float speed = 0.65f;
    public float retryInterval = 0.05f;
    public bool startOnPlay = false;

    private PathFinder pathFinder;
    private BankersAlgorithm banker;

    private readonly Dictionary<int, int> resourceIndexByNodeId =
        new Dictionary<int, int>();

    private AGVNode[] bottleneckNodes;
    private List<AGVAgent> fleet;

    private WaitForGraph waitGraph = new WaitForGraph();

    private readonly Dictionary<int, int> recoveryCounts =
        new Dictionary<int, int>();

    private int completedTasks;
    private int grantedRequests;
    private int waitingEvents;

    private float totalWaitingSeconds;
    private float totalRecoverySeconds;

    private int activeAgents;

    private bool isRunning;
    private bool resolvingDeadlock;

    private int deadlocksDetected;
    private int deadlocksResolved;

    // =========================================================
    // DASHBOARD PROPERTIES
    // =========================================================

    public int FleetSize
    {
        get
        {
            if (fleet != null)
                return fleet.Count;

            return fleetSpawner != null
                ? fleetSpawner.fleetSize
                : 0;
        }
    }

    public int CompletedTasks => completedTasks;
    public int GrantedRequests => grantedRequests;
    public int WaitingEvents => waitingEvents;

    public float TotalWaitingSeconds => totalWaitingSeconds;
    public float TotalRecoverySeconds => totalRecoverySeconds;

    public int DeadlocksDetected => deadlocksDetected;
    public int DeadlocksResolved => deadlocksResolved;

    public bool IsRunning => isRunning;

    public int BottleneckCount
    {
        get
        {
            return bottleneckNodeIds != null
                ? bottleneckNodeIds.Length
                : 0;
        }
    }

    public string StatusLabel
    {
        get
        {
            if (isRunning)
                return "RUNNING";

            if (FleetSize > 0 &&
                completedTasks >= FleetSize)
                return "COMPLETE";

            return "READY";
        }
    }

    public int WaitingAgents
    {
        get
        {
            if (fleet == null)
                return 0;

            int count = 0;

            foreach (AGVAgent agent in fleet)
            {
                if (agent != null &&
                    agent.state == "WAITING")
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int MovingAgents
    {
        get
        {
            if (fleet == null)
                return 0;

            int count = 0;

            foreach (AGVAgent agent in fleet)
            {
                if (agent != null &&
                    agent.state == "MOVING")
                {
                    count++;
                }
            }

            return count;
        }
    }

    // =========================================================
    // START / STOP
    // =========================================================

    private void Start()
    {
        if (startOnPlay)
            StartControlledFleet();
    }

    public void StartControlledFleet()
    {
        if (isRunning)
            return;

        StartCoroutine(RunFleet());
    }

    // =========================================================
    // MAIN FLEET RUN
    // =========================================================

    private IEnumerator RunFleet()
    {
        isRunning = true;

        if (fleetSpawner == null)
        {
            fleetSpawner =
                FindFirstObjectByType<AGVFleetSpawner>();
        }

        if (graph == null)
        {
            graph =
                FindFirstObjectByType<AGVGraph>();
        }

        if (resourceManager == null)
        {
            resourceManager =
                FindFirstObjectByType<ResourceManager>();
        }

        if (fleetSpawner == null ||
            graph == null ||
            resourceManager == null)
        {
            Debug.LogError(
                "FleetTrafficController: Missing references. " +
                "Assign Fleet Spawner, Graph and Resource Manager."
            );

            isRunning = false;
            yield break;
        }

        pathFinder = GetComponent<PathFinder>();

        if (pathFinder == null)
        {
            pathFinder = gameObject.AddComponent<PathFinder>();
        }

        if (graph.nodes == null ||
            graph.nodes.Count != 100)
        {
            graph.BuildGraph();
        }

        if (graph.nodes == null ||
            graph.nodes.Count != 100)
        {
            Debug.LogError(
                "Expected 100 graph nodes. Found " +
                (graph.nodes == null ? 0 : graph.nodes.Count)
            );

            isRunning = false;
            yield break;
        }

        fleet = fleetSpawner.GetFleet();

        if (fleet == null || fleet.Count != 30)
        {
            Debug.LogError(
                "Expected 30 AGVs, but found " +
                (fleet == null ? 0 : fleet.Count) +
                ". Stop Play Mode and regenerate the fleet."
            );

            isRunning = false;
            yield break;
        }

        if (!PrepareRoutesAndBanker())
        {
            isRunning = false;
            yield break;
        }

        // Start every run from a clean state.
        resourceManager.ResetResources();

        waitGraph.Clear();
        recoveryCounts.Clear();

        resolvingDeadlock = false;

        completedTasks = 0;
        grantedRequests = 0;
        waitingEvents = 0;

        deadlocksDetected = 0;
        deadlocksResolved = 0;

        totalWaitingSeconds = 0f;
        totalRecoverySeconds = 0f;

        activeAgents = fleet.Count;

        Debug.Log("==============================================");
        Debug.Log("       BANKER-CONTROLLED AGV FLEET");
        Debug.Log("==============================================");

        Debug.Log("Fleet size: " + fleet.Count);
        Debug.Log("Graph nodes: " + graph.nodes.Count);
        Debug.Log("Bottleneck zones: " + bottleneckNodes.Length);

        for (int i = 0; i < fleet.Count; i++)
        {
            StartCoroutine(MoveAGV(fleet[i], i));

            yield return new WaitForSeconds(0.02f);
        }

        while (activeAgents > 0)
        {
            yield return null;
        }

        Debug.Log("==============================================");
        Debug.Log("          FLEET RUN COMPLETE");
        Debug.Log("==============================================");

        Debug.Log("Tasks completed: " + completedTasks);
        Debug.Log("Resource grants: " + grantedRequests);
        Debug.Log("Waiting events: " + waitingEvents);
        Debug.Log("Deadlocks detected: " + deadlocksDetected);
        Debug.Log("Deadlocks resolved: " + deadlocksResolved);

        Debug.Log(
            "Total waiting time: " +
            totalWaitingSeconds.ToString("F2") + " s"
        );

        Debug.Log(
            "Total recovery time: " +
            totalRecoverySeconds.ToString("F2") + " s"
        );

        isRunning = false;
    }

    // =========================================================
    // ROUTE GENERATION AND BANKER INITIALIZATION
    // =========================================================

    private bool PrepareRoutesAndBanker()
    {
        if (bottleneckNodeIds == null ||
            bottleneckNodeIds.Length != 4)
        {
            Debug.LogError(
                "Configure exactly four bottleneck node IDs."
            );

            return false;
        }

        bottleneckNodes =
            new AGVNode[bottleneckNodeIds.Length];

        resourceIndexByNodeId.Clear();

        // Resolve bottleneck IDs to graph nodes.
        for (int r = 0; r < bottleneckNodeIds.Length; r++)
        {
            bottleneckNodes[r] =
                FindNode(bottleneckNodeIds[r]);

            if (bottleneckNodes[r] == null)
            {
                Debug.LogError(
                    "Could not find Node_" +
                    bottleneckNodeIds[r]
                );

                return false;
            }

            resourceIndexByNodeId.Add(
                bottleneckNodes[r].nodeId,
                r
            );
        }

        HashSet<int> usedStarts = new HashSet<int>();

        // Assign start, destination and path to every AGV.
        for (int i = 0; i < fleet.Count; i++)
        {
            int startIndex =
                (i * 37) % graph.nodes.Count;

            int attempts = 0;

            while (
                (usedStarts.Contains(
                    graph.nodes[startIndex].nodeId) ||
                 resourceIndexByNodeId.ContainsKey(
                    graph.nodes[startIndex].nodeId)) &&
                attempts < graph.nodes.Count)
            {
                startIndex =
                    (startIndex + 1) % graph.nodes.Count;

                attempts++;
            }

            AGVNode start = graph.nodes[startIndex];

            usedStarts.Add(start.nodeId);

            int destinationIndex =
                (startIndex + 47) % graph.nodes.Count;

            attempts = 0;

            while (
                (destinationIndex == startIndex ||
                 resourceIndexByNodeId.ContainsKey(
                    graph.nodes[destinationIndex].nodeId)) &&
                attempts < graph.nodes.Count)
            {
                destinationIndex =
                    (destinationIndex + 13) %
                    graph.nodes.Count;

                attempts++;
            }

            AGVNode destination =
                graph.nodes[destinationIndex];

            // Assign one primary waypoint per AGV.
            AGVNode waypoint =
                bottleneckNodes[
                    i % bottleneckNodes.Length
                ];

            List<AGVNode> route =
                new List<AGVNode>();

            List<AGVNode> firstSegment =
                pathFinder.FindPath(
                    start,
                    waypoint
                );

            List<AGVNode> secondSegment =
                pathFinder.FindPath(
                    waypoint,
                    destination
                );

            if (!AppendSegment(route, firstSegment) ||
                !AppendSegment(route, secondSegment))
            {
                Debug.LogError(
                    "Could not build a route for AGV " +
                    (i + 1)
                );

                return false;
            }

            AGVAgent agent = fleet[i];

            agent.agvId = i + 1;
            agent.startNode = start;
            agent.currentNode = start;
            agent.destinationNode = destination;
            agent.currentPath = route;

            agent.transform.position =
                start.transform.position;

            agent.state = "READY";

            // Visual appearance.
            SpriteRenderer sprite =
                agent.GetComponent<SpriteRenderer>();

            if (sprite != null)
            {
                sprite.color = Color.HSVToRGB(
                    (i % 6) / 6f,
                    0.8f,
                    1f
                );

                sprite.sortingOrder = 10;
            }

            agent.transform.localScale =
                new Vector3(0.32f, 0.22f, 1f);
        }

        // Banker model: four resources, one instance each.
        int processCount = fleet.Count;
        int resourceCount = bottleneckNodes.Length;

        int[] available =
            new int[resourceCount];

        int[,] allocation =
            new int[processCount, resourceCount];

        int[,] maximum =
            new int[processCount, resourceCount];

        for (int r = 0; r < resourceCount; r++)
        {
            available[r] = 1;
        }

        // Record the bottlenecks appearing on each route.
        for (int p = 0; p < processCount; p++)
        {
            foreach (AGVNode node in fleet[p].currentPath)
            {
                if (resourceIndexByNodeId.TryGetValue(
                    node.nodeId,
                    out int resourceIndex))
                {
                    maximum[p, resourceIndex] = 1;
                }
            }
        }

        banker = new BankersAlgorithm();

        banker.Initialize(
            processCount,
            resourceCount
        );

        if (!banker.SetState(
            available,
            allocation,
            maximum,
            out string error))
        {
            Debug.LogError(
                "Banker initialization failed: " + error
            );

            return false;
        }

        if (!banker.IsSafe(
            out List<int> safeSequence))
        {
            Debug.LogError(
                "Initial Banker safety check failed."
            );

            return false;
        }

        Debug.Log(
            "Initial Banker safety check passed. " +
            "Safe sequence length: " +
            safeSequence.Count
        );

        return true;
    }

    // =========================================================
    // AGV MOVEMENT, RESOURCE REQUESTS AND WFG
    // =========================================================

    private IEnumerator MoveAGV(
        AGVAgent agent,
        int processIndex)
    {
        int heldResource = -1;

        for (int i = 1;
             i < agent.currentPath.Count;
             i++)
        {
            if (agent.state == "BACKOFF")
                yield break;

            AGVNode nextNode =
                agent.currentPath[i];

            bool isBottleneck =
                resourceIndexByNodeId.TryGetValue(
                    nextNode.nodeId,
                    out int resourceIndex
                );

            if (isBottleneck &&
                resourceIndex != heldResource)
            {
                float waitStart = Time.time;

                bool reportedWait = false;

                int lastWaitOwner = -1;

                while (true)
                {
                    if (agent.state == "BACKOFF")
                        yield break;

                    // Discover the AGV currently holding this
                    // resource, and maintain the WFG edge.
                    int owner =
                        resourceManager.GetOwner(
                            nextNode.nodeId
                        );

                    if (owner != lastWaitOwner)
                    {
                        if (lastWaitOwner != -1)
                        {
                            waitGraph.RemoveWait(
                                agent.agvId,
                                lastWaitOwner
                            );
                        }

                        lastWaitOwner = -1;

                        if (owner != -1 &&
                            owner != agent.agvId)
                        {
                            waitGraph.AddWait(
                                agent.agvId,
                                owner
                            );

                            lastWaitOwner = owner;

                            Debug.Log(
                                "WFG EDGE: AGV " +
                                agent.agvId +
                                " waits for AGV " +
                                owner
                            );

                            List<int> cycle =
                                waitGraph.FindDeadlockCycle();

                            if (cycle != null &&
                                cycle.Count > 0 &&
                                !resolvingDeadlock)
                            {
                                deadlocksDetected++;

                                Debug.LogError(
                                    "WFG CYCLE DETECTED: " +
                                    string.Join(" -> ", cycle)
                                );

                                int victimId =
                                    SelectWfgVictim(cycle);

                                StartCoroutine(
                                    RecoverWfgDeadlock(
                                        victimId
                                    )
                                );
                            }
                        }
                    }

                    if (agent.state == "BACKOFF")
                        yield break;

                    int[] request =
                        new int[bottleneckNodes.Length];

                    request[resourceIndex] = 1;

                    bool safe = banker.TryRequest(
                        processIndex,
                        request,
                        out string message,
                        out _
                    );

                    if (safe)
                    {
                        if (resourceManager.TryReserve(
                            nextNode.nodeId,
                            agent.agvId))
                        {
                            grantedRequests++;

                            // Clear this AGV's outgoing waiting edge.
                            if (lastWaitOwner != -1)
                            {
                                waitGraph.RemoveWait(
                                    agent.agvId,
                                    lastWaitOwner
                                );
                            }

                            break;
                        }

                        // The resource was occupied despite the
                        // Banker check; roll back the tentative grant.
                        banker.ReleaseResources(
                            processIndex,
                            request
                        );

                        message =
                            "Bottleneck occupied by another AGV.";
                    }

                    if (!reportedWait)
                    {
                        waitingEvents++;

                        Debug.Log(
                            "AGV " + agent.agvId +
                            " waiting at bottleneck B" +
                            (resourceIndex + 1) +
                            ": " + message
                        );

                        reportedWait = true;
                    }

                    agent.state = "WAITING";

                    yield return new WaitForSeconds(
                        retryInterval
                    );
                }

                totalWaitingSeconds +=
                    Time.time - waitStart;

                int previousResource = heldResource;

                agent.state = "MOVING";

                yield return MoveTo(
                    agent.transform,
                    nextNode.transform.position
                );

                if (agent.state == "BACKOFF")
                    yield break;

                agent.currentNode = nextNode;

                // Release the previous resource after entering
                // the next bottleneck.
                if (previousResource >= 0 &&
                    previousResource != resourceIndex)
                {
                    ReleaseBottleneck(
                        previousResource,
                        agent,
                        processIndex
                    );
                }

                heldResource = resourceIndex;
            }
            else
            {
                agent.state = "MOVING";

                yield return MoveTo(
                    agent.transform,
                    nextNode.transform.position
                );

                if (agent.state == "BACKOFF")
                    yield break;

                agent.currentNode = nextNode;

                if (!isBottleneck &&
                    heldResource >= 0)
                {
                    ReleaseBottleneck(
                        heldResource,
                        agent,
                        processIndex
                    );

                    heldResource = -1;
                }
            }
        }

        if (agent.state == "BACKOFF")
            yield break;

        if (heldResource >= 0)
        {
            ReleaseBottleneck(
                heldResource,
                agent,
                processIndex
            );
        }

        waitGraph.RemoveAGV(agent.agvId);

        banker.CompleteProcess(processIndex);

        agent.state = "COMPLETED";

        completedTasks++;
        activeAgents--;

        Debug.Log(
            "AGV " + agent.agvId +
            " completed its task at Node " +
            agent.destinationNode.nodeId
        );
    }

    // =========================================================
    // DEADLOCK DETECTION AND RECOVERY
    // =========================================================

    private int SelectWfgVictim(List<int> cycle)
    {
        int victim = cycle[0];

        foreach (int agvId in cycle)
        {
            if (agvId > victim)
                victim = agvId;
        }

        return victim;
    }

    private IEnumerator RecoverWfgDeadlock(int victimId)
    {
        if (resolvingDeadlock)
            yield break;

        resolvingDeadlock = true;

        float recoveryStart = Time.time;

        AGVAgent victim = null;

        if (fleet != null)
        {
            foreach (AGVAgent agent in fleet)
            {
                if (agent != null &&
                    agent.agvId == victimId)
                {
                    victim = agent;
                    break;
                }
            }
        }

        if (victim == null)
        {
            Debug.LogError(
                "Deadlock recovery failed: victim AGV not found."
            );

            resolvingDeadlock = false;
            yield break;
        }

        int processIndex = victim.agvId - 1;

        recoveryCounts.TryGetValue(
            victimId,
            out int previousCount
        );

        if (previousCount >= 2)
        {
            Debug.LogError(
                "AGV " + victimId +
                " exceeded the automatic recovery retry limit. " +
                "Inspect the route and resource model."
            );

            resolvingDeadlock = false;
            yield break;
        }

        recoveryCounts[victimId] =
            previousCount + 1;

        Debug.Log(
            "Deadlock recovery: AGV " +
            victimId +
            " selected for safe backoff."
        );

        // Signal the existing movement coroutine to stop.
        victim.state = "BACKOFF";

        // Allow an in-progress edge movement to finish.
        yield return new WaitForSeconds(
            Mathf.Max(
                2.1f,
                1.5f / Mathf.Max(speed, 0.1f)
            )
        );

        // Release physical resource ownership.
        resourceManager.ReleaseAllForAGV(
            victimId
        );

        // Remove the victim's waiting relationships.
        waitGraph.RemoveAGV(victimId);

        // Release allocations in the Banker model.
        banker.ReleaseAllResources(
            processIndex
        );

        // Return the victim to its original position.
        if (victim.startNode != null)
        {
            victim.transform.position =
                victim.startNode.transform.position;

            victim.currentNode =
                victim.startNode;
        }

        victim.state = "READY";

        totalRecoverySeconds +=
            Time.time - recoveryStart;

        deadlocksResolved++;

        Debug.Log(
            "Deadlock recovery completed for AGV " +
            victimId
        );

        resolvingDeadlock = false;

        // Resume the interrupted task.
        if (isRunning && activeAgents > 0)
        {
            StartCoroutine(
                MoveAGV(
                    victim,
                    processIndex
                )
            );
        }
    }

    // =========================================================
    // RESOURCE HELPERS
    // =========================================================

    private void ReleaseBottleneck(
        int resourceIndex,
        AGVAgent agent,
        int processIndex)
    {
        int nodeId =
            bottleneckNodes[resourceIndex].nodeId;

        resourceManager.Release(
            nodeId,
            agent.agvId
        );

        int[] released =
            new int[bottleneckNodes.Length];

        released[resourceIndex] = 1;

        banker.ReleaseResources(
            processIndex,
            released
        );
    }

    // =========================================================
    // MOVEMENT HELPER
    // =========================================================

    private IEnumerator MoveTo(
        Transform agv,
        Vector3 target)
    {
        while (
            Vector3.Distance(
                agv.position,
                target
            ) > 0.03f)
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

    // =========================================================
    // ROUTE HELPERS
    // =========================================================

    private bool AppendSegment(
        List<AGVNode> combined,
        List<AGVNode> segment)
    {
        if (segment == null ||
            segment.Count == 0)
        {
            return false;
        }

        int startIndex =
            combined.Count == 0 ? 0 : 1;

        for (int i = startIndex;
             i < segment.Count;
             i++)
        {
            combined.Add(segment[i]);
        }

        return true;
    }

    private AGVNode FindNode(int nodeId)
    {
        foreach (AGVNode node in graph.nodes)
        {
            if (node.nodeId == nodeId)
                return node;
        }

        return null;
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetFleet()
    {
        StopAllCoroutines();

        isRunning = false;
        activeAgents = 0;
        resolvingDeadlock = false;

        if (fleetSpawner == null)
        {
            fleetSpawner =
                FindFirstObjectByType<AGVFleetSpawner>();
        }

        if (resourceManager == null)
        {
            resourceManager =
                FindFirstObjectByType<ResourceManager>();
        }

        fleet = fleetSpawner != null
            ? fleetSpawner.GetFleet()
            : new List<AGVAgent>();

        if (resourceManager != null)
        {
            resourceManager.ResetResources();
        }

        waitGraph.Clear();
        recoveryCounts.Clear();

        foreach (AGVAgent agent in fleet)
        {
            if (agent == null)
                continue;

            if (agent.startNode != null)
            {
                agent.transform.position =
                    agent.startNode.transform.position;

                agent.currentNode =
                    agent.startNode;
            }

            agent.state = "READY";

            if (agent.currentPath == null)
            {
                agent.currentPath =
                    new List<AGVNode>();
            }
            else
            {
                agent.currentPath.Clear();
            }
        }

        completedTasks = 0;
        grantedRequests = 0;
        waitingEvents = 0;

        deadlocksDetected = 0;
        deadlocksResolved = 0;

        totalWaitingSeconds = 0f;
        totalRecoverySeconds = 0f;

        banker = null;
        bottleneckNodes = null;

        resourceIndexByNodeId.Clear();

        Debug.Log(
            "Fleet reset successfully. Ready for a new run."
        );
    }
}