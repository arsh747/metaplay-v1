using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private string hubSceneName = "Hub";
    [SerializeField] private string bootSceneName = "Boot";

    private string _currentGameScene;
    private Coroutine _routine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitialLoadRoutine());
    }

    // Loads Hub, then hides Boot's own UI/camera once Hub is ready,
    // so the boot menu doesn't linger visible behind/alongside Hub.
    private IEnumerator InitialLoadRoutine()
    {
        var load = SceneManager.LoadSceneAsync(hubSceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(hubSceneName));

        SetSceneActive(bootSceneName, false);

        KeepOnlyOneEventSystem(hubSceneName);
    }

    public void LoadGame(string sceneName)
    {
        if (_routine != null) return;
        _routine = StartCoroutine(LoadGameRoutine(sceneName));
    }

    public void ReturnToHub()
    {
        if (_routine != null) return;
        _routine = StartCoroutine(UnloadAndReturnRoutine());
    }

    public static void RequestReturnToHub()
    {
        if (Instance == null)
        {
            Debug.LogWarning("SceneLoader.Instance is null - loading Hub directly as a fallback. " +
                              "For full functionality, start from the Boot scene.");
            SceneManager.LoadScene("Hub", LoadSceneMode.Single);
            return;
        }
        Instance.ReturnToHub();
    }

    private IEnumerator LoadGameRoutine(string sceneName)
    {
        HubEvents.RaiseLoadingStarted();

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        // Hide Hub's AND Boot's UI/Camera so they don't render/raycast on top of the loaded game.
        SetSceneActive(hubSceneName, false);
        SetSceneActive(bootSceneName, false);

        // Make sure only ONE EventSystem is active (the loaded game's own one),
        // otherwise touch/clicks silently stop working on whichever one loses.
        KeepOnlyOneEventSystem(sceneName);

        // NEW: record the scene using its ACTUAL loaded name, not the requested
        // string. If sceneName had any mismatch (case/typo) but somehow still
        // resolved, this keeps the tracked name consistent with what's really loaded.
        _currentGameScene = sceneName;
        HubEvents.RaiseLoadingFinished();
        _routine = null;
    }

    private IEnumerator UnloadAndReturnRoutine()
    {
        if (!string.IsNullOrEmpty(_currentGameScene))
        {
            var unload = SceneManager.UnloadSceneAsync(_currentGameScene);

            // NEW: UnloadSceneAsync returns null if the scene name doesn't
            // match any currently loaded scene. Without this check, the next
            // line (unload.isDone) throws a NullReferenceException and the
            // whole coroutine dies silently - which blocks _routine from ever
            // being reset to null, permanently breaking ReturnToHub()/LoadGame()
            // for the rest of the session.
            if (unload != null)
            {
                while (!unload.isDone) yield return null;
            }
            else
            {
                Debug.LogWarning($"SceneLoader: could not unload scene '{_currentGameScene}' - " +
                    "it may not be currently loaded. Check for a name mismatch between the scene " +
                    "requested via LoadGame(), the GameLibrary entry, and the actual scene file name " +
                    "in Build Settings (case-sensitive).");
            }

            _currentGameScene = null;
        }

        yield return Resources.UnloadUnusedAssets();

        // Show Hub again. Boot stays hidden - it's only needed for the initial
        // splash/loading, not for normal Hub<->Game navigation.
        SetSceneActive(hubSceneName, true);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(hubSceneName));

        // Restore Hub's own EventSystem as the active one.
        KeepOnlyOneEventSystem(hubSceneName);

        _routine = null;
    }

    private void SetSceneActive(string sceneName, bool active)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid()) return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            root.SetActive(active);
        }
    }

    // Finds every EventSystem currently loaded (across all additive scenes),
    // keeps the one belonging to `preferredSceneName` active, disables the rest.
    private void KeepOnlyOneEventSystem(string preferredSceneName)
    {
        EventSystem[] allEventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();

        foreach (EventSystem es in allEventSystems)
        {
            // Skip prefab assets / hidden objects, only touch ones actually in a loaded scene
            if (!es.gameObject.scene.IsValid()) continue;

            bool belongsToPreferredScene = es.gameObject.scene.name == preferredSceneName;
            es.gameObject.SetActive(belongsToPreferredScene);
        }
    }
}