using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGVController : MonoBehaviour
{
    public float speed = 2f;

    public AGVNode startNode;
    public AGVNode targetNode;
    public AGVGraph graph;

    private List<AGVNode> currentPath;

    private void Start()
    {
        if (graph == null)
        {
            Debug.LogError("AGVGraph is not assigned.");
            return;
        }

        if (startNode == null || targetNode == null)
        {
            Debug.LogError("Start Node or Target Node is missing.");
            return;
        }

        PathFinder pathFinder = graph.gameObject.AddComponent<PathFinder>();

        currentPath = pathFinder.FindPath(startNode, targetNode);

        if (currentPath.Count > 0)
        {
            StartCoroutine(FollowPath());
        }
    }

    private IEnumerator FollowPath()
    {
        foreach (AGVNode node in currentPath)
        {
            Vector3 targetPosition = node.transform.position;

            while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );

                yield return null;
            }
        }

        Debug.Log("AGV reached destination.");
    }
}