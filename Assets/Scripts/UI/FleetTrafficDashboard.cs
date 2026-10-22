using UnityEngine;

public class FleetTrafficDashboard : MonoBehaviour
{
    public FleetTrafficController controller;
    public bool showDashboard = true;

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle statusStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        if (controller == null)
        {
            controller =
                FindFirstObjectByType<FleetTrafficController>();
        }
    }

    private void OnGUI()
    {
        if (!showDashboard)
            return;

        if (controller == null)
        {
            controller =
                FindFirstObjectByType<FleetTrafficController>();
        }

        if (controller == null)
            return;

        CreateStyles();

        float width = 260f;
        float height = 300f;
        float x = Screen.width - width - 14f;
        float y = 14f;

        GUI.Box(
            new Rect(x, y, width, height),
            GUIContent.none
        );

        GUI.Label(
            new Rect(x + 12, y + 10, width - 24, 25),
            "AGV FLEET MANAGEMENT",
            titleStyle
        );

        DrawMetric(x, y + 43, "Fleet Size",
            controller.FleetSize.ToString());

        DrawMetric(x, y + 67, "Tasks Completed",
            controller.CompletedTasks + " / " +
            controller.FleetSize);

        DrawMetric(x, y + 91, "Moving",
            controller.MovingAgents.ToString());

        DrawMetric(x, y + 115, "Waiting",
            controller.WaitingAgents.ToString());

        DrawMetric(x, y + 139, "Bottleneck Zones",
            controller.BottleneckCount.ToString());

        DrawMetric(x, y + 163, "Banker Grants",
            controller.GrantedRequests.ToString());

        DrawMetric(x, y + 187, "Waiting Events",
            controller.WaitingEvents.ToString());

        DrawMetric(x, y + 211, "Waiting Time",
            controller.TotalWaitingSeconds.ToString("F2") + " s");

        GUI.Label(
            new Rect(x + 12, y + 235, width - 24, 20),
            "STATUS: " + controller.StatusLabel,
            statusStyle
        );

        GUI.enabled = !controller.IsRunning;

        if (GUI.Button(
            new Rect(x + 12, y + 259, 112, 28),
            "RUN FLEET",
            buttonStyle))
        {
            controller.StartControlledFleet();
        }

        GUI.enabled = true;

        if (GUI.Button(
            new Rect(x + 132, y + 259, 116, 28),
            "RESET FLEET",
            buttonStyle))
        {
            controller.ResetFleet();
        }
    }

    private void DrawMetric(
        float x,
        float y,
        string label,
        string value)
    {
        GUI.Label(
            new Rect(x + 12, y, 155, 21),
            label,
            labelStyle
        );

        GUI.Label(
            new Rect(x + 168, y, 75, 21),
            value,
            valueStyle
        );
    }

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 15;
        titleStyle.fontStyle = FontStyle.Bold;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;

        valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.fontSize = 12;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.alignment = TextAnchor.MiddleRight;

        statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.fontSize = 12;
        statusStyle.fontStyle = FontStyle.Bold;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 11;
    }
}