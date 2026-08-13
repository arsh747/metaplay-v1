using UnityEngine;

namespace ChessGame
{

public class OnScreenDebug : MonoBehaviour
{
    private static string log = "";
    private static bool doShow = true;

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    void HandleLog(string message, string stackTrace, LogType type)
    {
        // Show everything (not just errors) so we can see the normal
        // [AI-TRACE] flow logs too, not just crashes.
        string prefix = "";
        if (type == LogType.Error || type == LogType.Exception) prefix = "[ERROR] ";
        else if (type == LogType.Warning) prefix = "[WARN] ";

        log = prefix + message + "\n" + log; // newest on top
        if (log.Length > 4000) log = log.Substring(0, 4000);
    }

    void OnGUI()
    {
        if (!doShow) return;
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, Screen.width - 20, Screen.height - 20), log, style);
    }
}
}
