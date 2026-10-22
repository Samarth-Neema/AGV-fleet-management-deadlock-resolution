# AGV Fleet Management & Traffic Deadlock Resolution

A Unity-based simulation of an Automated Guided Vehicle (AGV) fleet in a manufacturing-plant environment. The project applies graph theory, hash tables, Banker's Algorithm, and Wait-For Graph cycle detection to study routing and shared-resource management.

## Project Scenario

The simulated plant contains:
- 30 AGVs
- A procedurally generated 10 × 10 grid with 100 nodes
- Four shared bottleneck resources: Nodes 44, 45, 54, and 55
- Individual AGV routes, destinations, and movement states
- Resource ownership tracking and safety checks

The grid and fleet use synthetic, code-generated data rather than data from a physical factory.

## DSA Concepts

### Graph Theory and Pathfinding
The manufacturing plant is represented as a graph. Nodes represent locations, while edges represent permitted connections. The graph and pathfinding scripts are used to calculate AGV routes.

### Hash Tables
A C# Dictionary in the ResourceManager tracks resource ownership, mapping a resource ID to the AGV currently holding it.

### Banker's Algorithm
The Banker implementation maintains resource-allocation state and evaluates whether a proposed allocation leaves the system in a safe state. Requests that cannot be granted safely are denied and retried by the fleet controller.

### Wait-For Graph and DFS
The Wait-For Graph represents AGV-to-AGV waiting dependencies. DFS checks the graph for cycles. The project also contains a separate controlled three-AGV deadlock demonstration with recovery.

## Implementation

The project was developed in Unity 6 with C#.

Key components include:
- `PlantGridGenerator.cs` — generates the grid
- `AGVFleetSpawner.cs` — creates the AGV fleet
- `AGVNode.cs` and `AGVGraph.cs` — represent the graph
- `PathFinder.cs` — calculates routes
- `ResourceManager.cs` — tracks resource ownership
- `BankersAlgorithm.cs` — performs resource-safety checks
- `WaitForGraph.cs` — represents waiting dependencies
- `FleetTrafficController.cs` — controls fleet movement and bottleneck requests
- `FleetTrafficDashboard.cs` — displays live fleet statistics
- `PerformanceBenchmark.cs` — benchmarks dictionary lookup and DFS operations

## Recorded Results

A recorded 30-AGV simulation run completed all 30 assigned tasks.

One recorded run reported:
- Tasks completed: 30/30
- Resource grants: 63
- Waiting events: 42
- Cumulative waiting time: 716.86 seconds
- Wait-For Graph cycles detected: 0
- Deadlocks resolved in that run: 0

The controlled three-AGV demonstration separately illustrates circular waiting, cycle detection, and recovery. These results are reported separately because the 30-AGV run did not detect a Wait-For Graph cycle.

The algorithm microbenchmark measures dictionary lookup and DFS cycle-detection time over 5,000 repetitions for each tested fleet size. These are algorithm-level measurements, not end-to-end simulation throughput.

## Complexity Analysis

Let V denote graph vertices, E graph edges, P AGV processes, R resource types, and W Wait-For Graph edges.

| Operation | Complexity |
|---|---|
| Adjacency-list graph storage | O(V + E) |
| Current graph construction | O(V²) |
| BFS pathfinding | O(V + E) |
| Dictionary lookup | O(1) average |
| Banker's safety check | O(P²R) |
| Wait-For Graph DFS | O(P + W) |

## Running the Project

1. Install a compatible Unity 6 Editor.
2. Clone or download this repository.
3. Open the project folder in Unity Hub.
4. Open `Assets/Scenes/MainScene.unity`.
5. Run the scene and use the fleet dashboard to start the simulation.

Unity may regenerate local editor and Library files when the project is opened.

## AI Disclosure

AI assistance was used during scenario formulation, code development and debugging, and documentation preparation. Implementation behaviour and reported measurements were checked against the Unity project and recorded test output. Unity's in-editor AI feature was not used because the required subscription or seat was unavailable.

## Limitations and Future Work

- The plant is a simplified 2D simulation, not a physical factory model.
- Routes and task assignments are generated algorithmically.
- The recorded 30-AGV run completed without a detected Wait-For Graph cycle.
- Future work includes further validation of live-fleet deadlock scenarios and recovery behaviour, repeated benchmark trials, and richer plant visuals.

---
Developed as an academic Robotics and Automation / Data Structures and Algorithms project.
