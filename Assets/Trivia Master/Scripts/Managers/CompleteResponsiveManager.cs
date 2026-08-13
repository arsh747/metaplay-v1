using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TriviaGame
{

[RequireComponent(typeof(Canvas))]
public class CompleteResponsiveManager : MonoBehaviour
{
    [Header("Canvas Configuration")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    public CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    public float matchWidthOrHeight = 0.5f;

    [Header("Automatic Scaling")]
    public bool autoConfigureOnStart = true;
    public bool updateInRuntime = true;
    public float checkInterval = 0.5f;

    [Header("Safe Area Settings (Mobile)")]
    public bool applySafeArea = true;

    [Header("Text Settings")]
    public bool autoScaleText = true;
    public float minTextScale = 0.8f;
    public float maxTextScale = 1.5f;

    [Header("Image Settings")]
    public bool preserveImageAspect = true;

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private RectTransform safeAreaTransform;
    private Vector2 lastScreenSize;
    private float timeSinceLastCheck;

    void Start()
    {
        // Get or create required components
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();

        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();

        // Configure the Canvas Render Mode
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Configure Canvas Scaler
        ConfigureCanvasScaler();

        // Create safe area container if needed
        CreateSafeAreaContainer();

        if (autoConfigureOnStart)
        {
            ApplyResponsiveSettings();
            ScaleAllText();
            ScaleAllImages();
            Handle3DObjects(); // If you have 3D elements
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    void Update()
    {
        if (!updateInRuntime) return;

        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            timeSinceLastCheck = 0;

            // Check if screen size changed
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
            if (currentScreenSize != lastScreenSize)
            {
                lastScreenSize = currentScreenSize;
                ApplyResponsiveSettings();
            }
        }
    }

    void ConfigureCanvasScaler()
    {
        canvasScaler.uiScaleMode = scaleMode;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = screenMatchMode;
        canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
    }

    void CreateSafeAreaContainer()
    {
        if (!applySafeArea) return;

        // Look for existing safe area container
        GameObject safeAreaGO = GameObject.Find("SafeAreaContainer");
        if (safeAreaGO == null)
        {
            safeAreaGO = new GameObject("SafeAreaContainer");
            safeAreaGO.transform.SetParent(transform, false);
            safeAreaTransform = safeAreaGO.AddComponent<RectTransform>();
        }
        else
        {
            safeAreaTransform = safeAreaGO.GetComponent<RectTransform>();
        }

        // Set up safe area container
        safeAreaTransform.anchorMin = Vector2.zero;
        safeAreaTransform.anchorMax = Vector2.one;
        safeAreaTransform.offsetMin = Vector2.zero;
        safeAreaTransform.offsetMax = Vector2.zero;

        // Move all existing UI elements to safe area container
        foreach (Transform child in transform)
        {
            if (child != safeAreaTransform && child.name != "SafeAreaContainer")
            {
                child.SetParent(safeAreaTransform);
            }
        }
    }

    void ApplySafeArea()
    {
        if (!applySafeArea || safeAreaTransform == null) return;

        Rect safeArea = Screen.safeArea;

        // Convert safe area rectangle to anchor points
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeAreaTransform.anchorMin = anchorMin;
        safeAreaTransform.anchorMax = anchorMax;
    }

    void ApplyResponsiveSettings()
    {
        // 1. Apply safe area first
        ApplySafeArea();

        // 2. Scale all text elements
        if (autoScaleText) ScaleAllText();

        // 3. Handle images
        if (preserveImageAspect) ScaleAllImages();

        // 4. Notify all responsive components
        BroadcastMessage("OnScreenSizeChanged", SendMessageOptions.DontRequireReceiver);

        Debug.Log($"Applied responsive settings for screen: {Screen.width}x{Screen.height}");
    }

    void ScaleAllText()
    {
        // Scale TextMeshPro text
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in tmpTexts)
        {
            ScaleSingleText(text);
        }

        // Scale legacy UI Text
        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        foreach (Text text in legacyTexts)
        {
            ScaleLegacyText(text);
        }
    }

    void ScaleSingleText(TMP_Text text)
    {
        if (text == null) return;

        // Calculate scale based on screen width
        float widthRatio = (float)Screen.width / referenceResolution.x;
        float heightRatio = (float)Screen.height / referenceResolution.y;
        float scaleFactor = Mathf.Lerp(widthRatio, heightRatio, matchWidthOrHeight);

        // Clamp the scale factor
        scaleFactor = Mathf.Clamp(scaleFactor, minTextScale, maxTextScale);

        // Apply scale to font size
        if (!text.enableAutoSizing)
        {
            // If auto-sizing is off, scale the fontSize
            text.fontSize = text.fontSize * scaleFactor;
        }
        else
        {
            // If auto-sizing is on, scale the min/max
            text.fontSizeMin *= scaleFactor;
            text.fontSizeMax *= scaleFactor;
        }

        // Also scale the rect transform if it's a heading or large text
        if (text.fontSize > 30) // Adjust this threshold as needed
        {
            RectTransform rt = text.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one * scaleFactor;
            }
        }
    }

    void ScaleLegacyText(Text text)
    {
        if (text == null) return;

        float widthRatio = (float)Screen.width / referenceResolution.x;
        float scaleFactor = Mathf.Lerp(widthRatio, (float)Screen.height / referenceResolution.y, matchWidthOrHeight);
        scaleFactor = Mathf.Clamp(scaleFactor, minTextScale, maxTextScale);

        if (text.resizeTextForBestFit)
        {
            text.resizeTextMinSize = Mathf.RoundToInt(text.resizeTextMinSize * scaleFactor);
            text.resizeTextMaxSize = Mathf.RoundToInt(text.resizeTextMaxSize * scaleFactor);
        }
        else
        {
            text.fontSize = Mathf.RoundToInt(text.fontSize * scaleFactor);
        }
    }

    void ScaleAllImages()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (preserveImageAspect)
            {
                image.preserveAspect = true;
            }

            // Scale UI Images based on screen size
            RectTransform rt = image.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Check if image should maintain aspect ratio
                float aspectRatio = image.sprite != null ?
                    image.sprite.rect.width / image.sprite.rect.height : 1f;

                // Calculate new size based on screen
                float screenHeightRatio = (float)Screen.height / referenceResolution.y;
                float newHeight = rt.sizeDelta.y * screenHeightRatio;
                float newWidth = preserveImageAspect ? newHeight * aspectRatio : rt.sizeDelta.x * screenHeightRatio;

                rt.sizeDelta = new Vector2(newWidth, newHeight);
            }
        }
    }

    void Handle3DObjects()
    {
        // If you have 3D UI elements or game objects that need scaling
        GameObject[] responsive3DObjects = GameObject.FindGameObjectsWithTag("Responsive3D");
        foreach (GameObject obj in responsive3DObjects)
        {
            Scale3DObject(obj);
        }
    }

    void Scale3DObject(GameObject obj)
    {
        // Calculate scale for 3D objects based on screen height
        float screenHeightRatio = (float)Screen.height / referenceResolution.y;
        float scaleFactor = Mathf.Clamp(screenHeightRatio, 0.5f, 2f);

        obj.transform.localScale = Vector3.one * scaleFactor;
    }

    // Public method to manually trigger responsive update
    public void ForceUpdateResponsive()
    {
        ApplyResponsiveSettings();
    }

    // Method to get current scale factor
    public float GetCurrentScaleFactor()
    {
        float widthRatio = (float)Screen.width / referenceResolution.x;
        float heightRatio = (float)Screen.height / referenceResolution.y;
        return Mathf.Lerp(widthRatio, heightRatio, matchWidthOrHeight);
    }

    // Called automatically when screen size changes
    void OnRectTransformDimensionsChange()
    {
        if (updateInRuntime)
        {
            ApplyResponsiveSettings();
        }
    }
}
}
