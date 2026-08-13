using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TouchFixer : EditorWindow
{
    [MenuItem("Tools/Fix Touch Issues (Active Scene)")]
    public static void FixTouch()
    {
        int fixCount = 0;

        // 1. Find ALL EventSystems in ALL loaded scenes, keep only the one in the active scene
        EventSystem[] allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        Scene activeScene = SceneManager.GetActiveScene();

        EventSystem keeper = null;
        foreach (var es in allEventSystems)
        {
            if (es.gameObject.scene == activeScene)
            {
                keeper = es;
                break;
            }
        }

        foreach (var es in allEventSystems)
        {
            if (es != keeper)
            {
                es.gameObject.SetActive(false);
                Debug.Log("Disabled duplicate EventSystem on: " + es.gameObject.name + " (scene: " + es.gameObject.scene.name + ")");
                fixCount++;
            }
            else if (es != null)
            {
                es.gameObject.SetActive(true);
                Debug.Log("Kept EventSystem active on: " + es.gameObject.name + " (scene: " + es.gameObject.scene.name + ")");
            }
        }

        // 2. Find ALL Cameras, make sure only cameras belonging to the active scene are tagged MainCamera and enabled
        Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            bool inActiveScene = cam.gameObject.scene == activeScene;
            if (!inActiveScene && cam.gameObject.CompareTag("MainCamera"))
            {
                cam.gameObject.tag = "Untagged";
                Debug.Log("Removed MainCamera tag from out-of-scene camera: " + cam.gameObject.name);
                fixCount++;
            }
        }

        // 3. Make sure every Canvas set to "Screen Space - Camera" actually has a camera reference in the active scene
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in allCanvases)
        {
            if (canvas.gameObject.scene != activeScene) continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                // Auto-fix: switch it to Overlay so it always works regardless of camera setup
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log("Canvas '" + canvas.gameObject.name + "' had no camera assigned — switched to Screen Space Overlay.");
                fixCount++;
            }

            // Ensure it has a GraphicRaycaster (needed for clicks)
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("Added missing GraphicRaycaster to: " + canvas.gameObject.name);
                fixCount++;
            }
        }

        Debug.Log($"<b>TouchFixer done.</b> Applied {fixCount} fix(es) for active scene: {activeScene.name}");
    }
}