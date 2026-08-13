using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HubUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GameLibrary gameLibrary;
    [SerializeField] private VisualTreeAsset gameCardTemplate;

    [Header("Splash timing (seconds)")]
    [SerializeField] private float splashDuration = 1.8f;

    private const string KeySound = "pref_sound_effects";
    private const string KeyMusic = "pref_music";
    private const string KeyDarkTheme = "pref_dark_theme";

    private VisualElement _root;
    private VisualElement _appRoot;
    private VisualElement _splashPanel;
    private VisualElement _homePanel;
    private VisualElement _settingsPanel;
    private VisualElement _loadingFill;
    private VisualElement _gameGrid;
    private ScrollView _settingsScrollView;
    private ScrollView _homeScrollView;

    // NEW: persists across OnEnable/OnDisable (this component is on a
    // DontDestroyOnLoad object) so the splash only ever plays once per app run.
    private static bool _hasShownSplashOnce = false;

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _appRoot = _root.Q<VisualElement>("app-root");

        _splashPanel = _root.Q<VisualElement>("splash-panel");
        _homePanel = _root.Q<VisualElement>("home-panel");
        _settingsPanel = _root.Q<VisualElement>("settings-panel");
        _loadingFill = _root.Q<VisualElement>("loading-fill");
        _gameGrid = _root.Q<VisualElement>("game-grid");
        _settingsScrollView = _root.Q<ScrollView>("settings-scroll-view");
        if (_settingsScrollView != null)
        {
            _settingsScrollView.elasticity = 0f;
            _settingsScrollView.contentContainer.style.justifyContent = Justify.FlexStart;
            var viewport = _settingsScrollView.Q<VisualElement>(className: "unity-content-viewport");
            if (viewport != null) viewport.style.justifyContent = Justify.FlexStart;
        }

        _homeScrollView = _root.Q<ScrollView>("home-scroll-view");
        if (_homeScrollView != null)
        {
            _homeScrollView.elasticity = 0f;
            _homeScrollView.contentContainer.style.justifyContent = Justify.FlexStart;
            var homeViewport = _homeScrollView.Q<VisualElement>(className: "unity-content-viewport");
            if (homeViewport != null) homeViewport.style.justifyContent = Justify.FlexStart;
        }

        StartCoroutine(ApplySafeAreaRoutine());
        PopulateGrid();
        WireHomeButtons();
        WireSettingsControls();

        HubEvents.OnLoadingStarted += HideHubForGameLoad;
        HubEvents.OnLoadingFinished += ShowHubAfterGameLoad;

        // CHANGED: only show the splash + run its coroutine the first time
        // Hub is ever enabled. Every later re-enable (e.g. returning from a
        // game) goes straight to the home panel.
        if (!_hasShownSplashOnce)
        {
            _hasShownSplashOnce = true;
            ShowOnly(_splashPanel);
            StartCoroutine(SplashRoutine());
        }
        else
        {
            ShowOnly(_homePanel);
        }
    }

    void OnDisable()
    {
        HubEvents.OnLoadingStarted -= HideHubForGameLoad;
        HubEvents.OnLoadingFinished -= ShowHubAfterGameLoad;
    }

    // ---------- Safe area (avoids camera cutouts / notches) ----------

    private IEnumerator ApplySafeAreaRoutine()
    {
        yield return null; // let Screen dimensions settle to the real device for one frame
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (screenWidth <= 0f || screenHeight <= 0f) return;

        float leftPct = (safeArea.xMin / screenWidth) * 100f;
        float rightPct = ((screenWidth - safeArea.xMax) / screenWidth) * 100f;
        float topPct = ((screenHeight - safeArea.yMax) / screenHeight) * 100f;
        float bottomPct = (safeArea.yMin / screenHeight) * 100f;

        const float minTopInsetPercent = 10f;
        topPct = Mathf.Max(topPct, minTopInsetPercent);

        _appRoot.style.paddingLeft = new StyleLength(new Length(leftPct, LengthUnit.Percent));
        _appRoot.style.paddingRight = new StyleLength(new Length(rightPct, LengthUnit.Percent));
        _appRoot.style.paddingTop = new StyleLength(new Length(topPct, LengthUnit.Percent));
        _appRoot.style.paddingBottom = new StyleLength(new Length(bottomPct, LengthUnit.Percent));
    }

    // ---------- Splash ----------

    private IEnumerator SplashRoutine()
    {
        float t = 0f;
        while (t < splashDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.Clamp01(t / splashDuration);
            _loadingFill.style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));
            yield return null;
        }
        ShowOnly(_homePanel);
    }

    // ---------- Navigation between the 3 screens ----------

    private void ShowOnly(VisualElement panel)
    {
        _splashPanel.style.display = DisplayStyle.None;
        _homePanel.style.display = DisplayStyle.None;
        _settingsPanel.style.display = DisplayStyle.None;
        panel.style.display = DisplayStyle.Flex;
    }

    private void WireHomeButtons()
    {
        _root.Q<Button>("menu-button").clicked += () =>
        {
            _settingsScrollView.scrollOffset = Vector2.zero;
            ShowOnly(_settingsPanel);
        };

        _root.Q<Button>("quit-button").clicked += () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        };
    }

    // ---------- Game grid ----------

    private void PopulateGrid()
    {
        _gameGrid.Clear();

        foreach (var game in gameLibrary.games)
        {
            var card = gameCardTemplate.Instantiate();
            card.AddToClassList("game-card-slot");
            var cardRoot = card.Q<VisualElement>("card-root");
            var icon = card.Q<VisualElement>("card-icon");
            var title = card.Q<Label>("card-title");
            var underline = card.Q<VisualElement>("card-underline");
            var lockOverlay = card.Q<VisualElement>("card-lock");

            title.text = game.displayName;
            cardRoot.style.backgroundColor = game.cardColor;
            title.style.color = game.accentColor;
            underline.style.backgroundColor = game.accentColor;

            if (game.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(game.icon);
                icon.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            }

            lockOverlay.style.display = game.isLocked ? DisplayStyle.Flex : DisplayStyle.None;

            var captured = game;
            cardRoot.RegisterCallback<ClickEvent>(evt =>
            {
                if (captured.isLocked) return;
                SceneLoader.Instance.LoadGame(captured.sceneName);
            });

            _gameGrid.Add(card);
        }
    }

    // ---------- Settings ----------

    private Toggle _soundToggle;
    private Toggle _musicToggle;
    private Toggle _themeToggle;

    private void WireSettingsControls()
    {
        _root.Q<Button>("settings-back-button").clicked += () => ShowOnly(_homePanel);

        _soundToggle = _root.Q<Toggle>("toggle-sound");
        _musicToggle = _root.Q<Toggle>("toggle-music");
        _themeToggle = _root.Q<Toggle>("toggle-theme");

        bool isDark = PlayerPrefs.GetInt(KeyDarkTheme, 1) == 1;

        _soundToggle.value = PlayerPrefs.GetInt(KeySound, 1) == 1;
        _musicToggle.value = PlayerPrefs.GetInt(KeyMusic, 0) == 1;
        _themeToggle.value = isDark;
        ApplyTheme(isDark);

        _soundToggle.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetInt(KeySound, evt.newValue ? 1 : 0);
        });

        _musicToggle.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetInt(KeyMusic, evt.newValue ? 1 : 0);
        });

        _themeToggle.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetInt(KeyDarkTheme, evt.newValue ? 1 : 0);
            ApplyTheme(evt.newValue);
        });

        var versionLabel = _root.Q<Label>("about-version-value");
        if (versionLabel != null) versionLabel.text = Application.version;

        _root.Q<Button>("button-reset").clicked += () =>
        {
            PlayerPrefs.DeleteKey(KeySound);
            PlayerPrefs.DeleteKey(KeyMusic);
            PlayerPrefs.DeleteKey(KeyDarkTheme);

            _soundToggle.value = true;
            _musicToggle.value = false;
            _themeToggle.value = true;
            ApplyTheme(true);
        };
    }

    private void ApplyTheme(bool isDark)
    {
        _appRoot.RemoveFromClassList("theme-dark");
        _appRoot.RemoveFromClassList("theme-light");
        _appRoot.AddToClassList(isDark ? "theme-dark" : "theme-light");
    }

    // ---------- Hide/show whole hub around a game load ----------

    private void HideHubForGameLoad() => _root.style.display = DisplayStyle.None;
    private void ShowHubAfterGameLoad() => _root.style.display = DisplayStyle.Flex;
}