using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PlantGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 10;
    public int columns = 10;

    [Tooltip("Distance between neighboring nodes.")]
    public float spacing = 1.2f;

    [Header("Node Template")]
    [Tooltip("Drag your existing Node_0 here.")]
    public GameObject nodeTemplate;

    [Header("Generated Parent")]
    public Transform generatedParent;

    [Header("Generation")]
    public bool centerGrid = true;

    [ContextMenu("Generate 10x10 Grid")]
    public void GenerateGrid()
    {
        if (nodeTemplate == null)
        {
            Debug.LogError(
                "PlantGridGenerator: Node Template is not assigned."
            );
            return;
        }

        ClearGeneratedGrid();

        GameObject parentObject =
            new GameObject("GeneratedNodes");

        generatedParent = parentObject.transform;
        generatedParent.SetParent(transform);

        float width =
            (columns - 1) * spacing;

        float height =
            (rows - 1) * spacing;

        Vector3 origin = Vector3.zero;

        if (centerGrid)
        {
            origin =
                new Vector3(
                    -width / 2f,
                    -height / 2f,
                    0f
                );
        }

        int nodeId = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                GameObject node = Instantiate(nodeTemplate);

                node.name =
                    "Node_" + nodeId;

                node.transform.SetParent(
                    generatedParent
                );

                node.transform.position =
                    origin +
                    new Vector3(
                        column * spacing,
                        row * spacing,
                        0f
                    );

                AGVNode agvNode =
                    node.GetComponent<AGVNode>();

                if (agvNode != null)
                {
                    agvNode.nodeId = nodeId;
                }

                nodeId++;
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(
                gameObject.scene
            );
        }
#endif

        Debug.Log(
            "10x10 plant grid generated successfully. " +
            "Total Nodes: " +
            (rows * columns)
        );
    }

    [ContextMenu("Clear Generated Grid")]
    public void ClearGeneratedGrid()
    {
        Transform oldParent =
            transform.Find("GeneratedNodes");

        if (oldParent == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.DestroyObjectImmediate(
                oldParent.gameObject
            );
        }
        else
        {
            Destroy(oldParent.gameObject);
        }
#else
        Destroy(oldParent.gameObject);
#endif
    }
}