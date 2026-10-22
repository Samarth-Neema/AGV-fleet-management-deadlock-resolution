using System.Collections.Generic;
using UnityEngine;

public class AGVGraph : MonoBehaviour
{
    public List<AGVNode> nodes =
        new List<AGVNode>();

    [Header("Node Source")]
    [Tooltip("Leave empty to automatically find GeneratedNodes.")]
    public Transform nodeRoot;

    [Header("Graph Settings")]
    [SerializeField]
    private float neighbourDistance = 1.3f;

    private void Awake()
    {
        BuildGraph();
    }

    public void BuildGraph()
    {
        nodes.Clear();

        // Use explicitly assigned node root if available.
        // Otherwise find the generated node container.
        Transform root = nodeRoot;

        if (root == null)
        {
            GameObject generated =
                GameObject.Find("GeneratedNodes");

            if (generated != null)
                root = generated.transform;
        }

        if (root == null)
        {
            Debug.LogError(
                "AGVGraph: GeneratedNodes could not be found."
            );

            return;
        }

        AGVNode[] foundNodes =
            root.GetComponentsInChildren<AGVNode>(
                true
            );

        nodes.AddRange(foundNodes);

        // Sort by node ID for predictable ordering.
        nodes.Sort(
            (a, b) =>
                a.nodeId.CompareTo(b.nodeId)
        );

        // Clear previous connections.
        foreach (AGVNode node in nodes)
        {
            node.neighbours.Clear();
        }

        // Build graph.
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                if (i == j)
                    continue;

                float distance =
                    Vector2.Distance(
                        nodes[i].transform.position,
                        nodes[j].transform.position
                    );

                if (distance <= neighbourDistance)
                {
                    nodes[i].neighbours.Add(
                        nodes[j]
                    );
                }
            }
        }

        Debug.Log(
            "AGV Graph built successfully. Total Nodes: " +
            nodes.Count
        );
    }
}