using UnityEngine;
using UnityEngine.UIElements;

namespace TriviaGame
{

/// <summary>
/// Drives the whole game's UI through Unity UI Toolkit (UIDocument +
/// UXML + USS) instead of the old Canvas/GameObject panel system.
/// One screen ("panel") is a VisualElement carrying the "screen" class;
/// only one is visible (flex) at a time, everything else gets the
/// "screen--hidden" class (display:none).
///
/// Public API is intentionally unchanged from the old uGUI version
/// (ShowMainMenu, ShowSettings, ShowCategoryPanel, ...) so GameManager
/// and QuestionManager don't need to change how they call into this.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIManager : MonoBehaviour
{
    // ---------------- SINGLETON ----------------
    public static UIManager Instance;

    // ---------------- ROOT ----------------
    UIDocument document;
    VisualElement root;

    /// <summary>Exposed so QuestionManager (and anything else) can query
    /// elements out of the same visual tree.</summary>
    public VisualElement Root => root;

    // The element that actually carries the ".theme-root" class (defined in
    // GameRoot.uxml) - NOT the same as `root` above, which is UIDocument's
    // outer wrapper (#UIManager-container). ThemeManager needs THIS element
    // specifically, since the dark-theme override rule in theme.uss is the
    // compound selector ".theme-root.dark-theme" - both classes have to land
    // on the same element or it never matches anything.
    VisualElement themeRoot;

    // ---------------- PANELS ----------------
    const string HiddenClass = "screen--hidden";

    VisualElement mainMenuPanel;
    VisualElement settingsPanel;
    VisualElement categoryPanel;
    VisualElement questionPanel;
    VisualElement categoryCompletePanel;
    VisualElement gameOverPanel;

    VisualElement[] allPanels;

    // ---------------- SCORE / LIVES / TIMER ----------------
    Label categoryCompleteScoreText;
    Label gameOverScoreText;
    Label currentScoreText;
    Label timerText;
    Label questionCounterText;
    VisualElement[] hearts;

    const string HeartLostClass = "heart--lost";

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        document = GetComponent<UIDocument>();
        // NOTE: document.rootVisualElement is intentionally NOT read here.
        // UIDocument builds its actual visual tree (cloning the UXML content
        // into the root) during ITS OWN OnEnable, and Unity runs Awake() for
        // every object in the scene before OnEnable() for any of them - so
        // reading/querying the tree this early would always find it empty.
    }

    void Start()
    {
        // Safe now: every object's OnEnable (including UIDocument's, which is
        // where it actually clones GameRoot.uxml into the tree) has already
        // run by the time ANY object's Start() runs - regardless of component
        // or GameObject order. This is the first point where root.Q<>() is
        // guaranteed to find real content.
        root = document.rootVisualElement;
        CacheElements();
        WireButtons();

        // ThemeManager/AudioManager singletons are also guaranteed ready here.
        if (ThemeManager.Instance != null)
            ThemeManager.Instance.Initialize(themeRoot);

        WireToggles();

        ShowMainMenu();
    }

    // ---------------- CACHE ----------------
    void CacheElements()
    {
        mainMenuPanel = root.Q<VisualElement>("MainMenuPanel");
        settingsPanel = root.Q<VisualElement>("SettingsPanel");
        categoryPanel = root.Q<VisualElement>("CategoryPanel");
        questionPanel = root.Q<VisualElement>("QuestionPanel");
        categoryCompletePanel = root.Q<VisualElement>("CategoryCompletePanel");
        gameOverPanel = root.Q<VisualElement>("GameOverPanel");

        themeRoot = root.Q<VisualElement>("GameRoot");

        allPanels = new[]
        {
            mainMenuPanel, settingsPanel, categoryPanel,
            questionPanel, categoryCompletePanel, gameOverPanel
        };

        categoryCompleteScoreText = root.Q<Label>("CategoryCompleteScoreText");
        gameOverScoreText = root.Q<Label>("GameOverScoreText");
        currentScoreText = root.Q<Label>("ScoreText");
        timerText = root.Q<Label>("TimerText");
        questionCounterText = root.Q<Label>("QuestionCounterText");

        hearts = new[]
        {
            root.Q<VisualElement>("Heart0"),
            root.Q<VisualElement>("Heart1"),
            root.Q<VisualElement>("Heart2"),
        };
    }

    // ---------------- BUTTON WIRING ----------------
    void Wire(string buttonName, System.Action onClick)
    {
        Button button = root.Q<Button>(buttonName);
        if (button == null)
        {
            Debug.LogWarning($"[UIManager] Wire: '{buttonName}' NOT FOUND in the visual tree.");
            return;
        }

        button.RegisterCallback<ClickEvent>(_ => onClick());
    }

    void WireButtons()
    {
        Wire("StartButton", () => GameManager.Instance.StartGame());
        Wire("SettingsButton", ShowSettings);
        Wire("QuitButton", () => GameManager.Instance.QuitGame());

        Wire("SettingsBackButton", ShowMainMenu);

        Wire("CategoryMoviesButton", () => GameManager.Instance.SelectCategory("Movies"));
        Wire("CategoryHistoryButton", () => GameManager.Instance.SelectCategory("History"));
        Wire("CategorySportsButton", () => GameManager.Instance.SelectCategory("Sports"));
        Wire("CategoryScienceButton", () => GameManager.Instance.SelectCategory("Science"));
        Wire("CategoryRandomButton", () => GameManager.Instance.SelectCategory("Random"));
        Wire("CategoryBackButton", ShowMainMenu);

        Wire("GameOverRetryButton", () => GameManager.Instance.ReplayCategory());
        Wire("GameOverMenuButton", ShowMainMenu);

        Wire("NextCategoryButton", ShowCategoryPanel);
        Wire("ReplayCategoryButton", () => GameManager.Instance.ReplayCategory());
        Wire("CategoryCompleteMenuButton", ShowMainMenu);
    }

    void WireToggles()
    {
        Toggle musicToggle = root.Q<Toggle>("MusicToggle");
        Toggle sfxToggle = root.Q<Toggle>("SFXToggle");
        Toggle themeToggle = root.Q<Toggle>("ThemeToggle");

        if (musicToggle != null)
        {
            // Reflect AudioManager's actual current state in the checkbox
            // WITHOUT firing the callback (which would immediately flip it).
            musicToggle.SetValueWithoutNotify(AudioManager.Instance != null && AudioManager.Instance.MusicOn);
            musicToggle.RegisterValueChangedCallback(evt =>
            {
                // Pass the checkbox's actual new value through - never a
                // blind flip, so the checkbox and the real audio state can't
                // drift apart.
                AudioManager.Instance?.SetMusic(evt.newValue);
            });
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetValueWithoutNotify(AudioManager.Instance != null && AudioManager.Instance.SfxOn);
            sfxToggle.RegisterValueChangedCallback(evt =>
            {
                AudioManager.Instance?.SetSFX(evt.newValue);
            });
        }

        if (themeToggle != null)
        {
            // Reflect the saved theme in the toggle without re-triggering it.
            themeToggle.SetValueWithoutNotify(ThemeManager.Instance != null && ThemeManager.Instance.IsDark);
            themeToggle.RegisterValueChangedCallback(evt =>
            {
                ThemeManager.Instance?.SetDark(evt.newValue);
            });
        }
    }

    // ---------------- PANEL CONTROL ----------------
    // Two layers need the hidden class, not one:
    //  1. The panel itself ("CategoryPanel" etc) - controls whether IT renders.
    //     Five of the six panel UXML files bake "screen--hidden" in as their
    //     default state directly in the markup (only MainMenu ships visible),
    //     so this class must be explicitly removed on show / re-added on hide.
    //  2. Its slot wrapper (panel.parent, the auto-generated TemplateContainer
    //     from <ui:Instance class="screen-slot">) - this is position:absolute
    //     covering the FULL SCREEN. Even with the panel inside it hidden, the
    //     wrapper itself stays a full-screen invisible box that still
    //     intercepts pointer clicks by default, silently eating input meant
    //     for whatever panel is visible underneath/behind it.
    // Both must be toggled together on every show/hide.
    VisualElement Slot(VisualElement panel) => panel != null ? (panel.parent ?? panel) : null;

    void HideAllPanels()
    {
        foreach (var panel in allPanels)
        {
            panel?.AddToClassList(HiddenClass);
            Slot(panel)?.AddToClassList(HiddenClass);
        }
    }

    void ShowPanel(VisualElement panel)
    {
        HideAllPanels();
        panel?.RemoveFromClassList(HiddenClass);
        Slot(panel)?.RemoveFromClassList(HiddenClass);
    }

    public void ShowMainMenu() => ShowPanel(mainMenuPanel);

    public void ShowSettings() => ShowPanel(settingsPanel);

    public void ShowCategoryPanel() => ShowPanel(categoryPanel);

    public void ShowQuestionPanel()
    {
        ShowPanel(questionPanel);
        UpdateScoreDisplay();
        UpdateLivesDisplay();
    }

    public void ShowCategoryComplete(int score, int total)
    {
        ShowPanel(categoryCompletePanel);
        if (categoryCompleteScoreText != null)
            categoryCompleteScoreText.text = score + "/" + total;
    }

    public void ShowGameOver(int score, int total)
    {
        ShowPanel(gameOverPanel);
        if (gameOverScoreText != null)
            gameOverScoreText.text = score + "/" + total;
    }

    // ---------------- UPDATE DISPLAYS ----------------
    public void UpdateScoreDisplay()
    {
        if (currentScoreText != null && QuestionManager.Instance != null)
        {
            int score = QuestionManager.Instance.GetScore();
            int total = QuestionManager.Instance.GetTotalQuestions();
            currentScoreText.text = "Score: " + score + "/" + total;
        }
    }

    public void UpdateLivesDisplay()
    {
        if (GameManager.Instance == null || hearts == null) return;

        int lives = GameManager.Instance.currentLives;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i]?.EnableInClassList(HeartLostClass, i >= lives);
        }
    }

    public void UpdateTimerDisplay(string formatted)
    {
        if (timerText != null)
            timerText.text = formatted;
    }

    public void UpdateQuestionCounter(string text)
    {
        if (questionCounterText != null)
            questionCounterText.text = text;
    }
}

}
