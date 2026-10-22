using UnityEngine;

public class DashboardUI : MonoBehaviour
{
    public TrafficSimulation simulation;

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle statusStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;

    private Texture2D panelTexture;

    private void CreateStyles()
    {
        panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(
            0,
            0,
            new Color(
                0.03f,
                0.07f,
                0.12f,
                0.92f
            )
        );
        panelTexture.Apply();

        panelStyle = new GUIStyle();
        panelStyle.normal.background = panelTexture;

        titleStyle = new GUIStyle();
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle();
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = Color.white;

        statusStyle = new GUIStyle();
        statusStyle.fontSize = 14;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 12;
        buttonStyle.fontStyle = FontStyle.Bold;
    }

    private void OnGUI()
    {
        if (titleStyle == null)
            CreateStyles();

        if (simulation == null)
            return;

        TrafficManager manager =
            simulation.trafficManager;

        if (manager == null)
            return;

        float panelWidth = 300f;
        float panelHeight = 250f;

        float panelX =
            Screen.width - panelWidth - 20f;

        float panelY = 20f;

        GUI.Box(
            new Rect(
                panelX,
                panelY,
                panelWidth,
                panelHeight
            ),
            GUIContent.none,
            panelStyle
        );

        GUI.Label(
            new Rect(
                panelX + 15f,
                panelY + 12f,
                270f,
                28f
            ),
            "AGV FLEET CONTROL",
            titleStyle
        );

        GUI.Label(
            new Rect(
                panelX + 15f,
                panelY + 42f,
                270f,
                22f
            ),
            "Fleet Size:          3",
            labelStyle
        );

        GUI.Label(
            new Rect(
                panelX + 15f,
                panelY + 66f,
                270f,
                22f
            ),
            "Simulation Time:    " +
            Time.time.ToString("F1") +
            " s",
            labelStyle
        );

        GUI.Label(
            new Rect(
                panelX + 15f,
                panelY + 90f,
                270f,
                22f
            ),
            "Deadlocks:          " +
            manager.deadlocksDetected,
            labelStyle
        );

        GUI.Label(
            new Rect(
                panelX + 150f,
                panelY + 90f,
                130f,
                22f
            ),
            "Resolved: " +
            manager.deadlocksResolved,
            labelStyle
        );

        GUI.Label(
            new Rect(
                panelX + 15f,
                panelY + 118f,
                270f,
                25f
            ),
            "STATUS: " +
            simulation.Status,
            statusStyle
        );

        // NORMAL TRAFFIC
        if (GUI.Button(
            new Rect(
                panelX + 15f,
                panelY + 155f,
                125f,
                32f
            ),
            "NORMAL TRAFFIC",
            buttonStyle))
        {
            simulation.StartNormalTraffic();
        }

        // DEADLOCK TEST
        if (GUI.Button(
            new Rect(
                panelX + 155f,
                panelY + 155f,
                125f,
                32f
            ),
            "DEADLOCK TEST",
            buttonStyle))
        {
            simulation.TriggerDeadlock();
        }

        // RESET
        if (GUI.Button(
            new Rect(
                panelX + 15f,
                panelY + 198f,
                265f,
                32f
            ),
            "RESET",
            buttonStyle))
        {
            simulation.ResetSimulation();
        }
    }
}