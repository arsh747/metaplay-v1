using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the WordGameUI.uxml / WordGameUI.uss UI Toolkit layout.
///
/// This is a pure "view" layer: it knows nothing about game rules, word banks,
/// PlayerPrefs, etc. Your existing GameMasterManager / WordGridManager logic
/// should subscribe to the public events below to react to input, and call
/// the public Set/Show/Build methods below to push state into the UI.
///
/// SETUP:
/// 1. Add a UIDocument component to a GameObject in your scene.
/// 2. Assign WordGameUI.uxml as its Source Asset, and a PanelSettings asset.
/// 3. Add this script to the same GameObject.
/// 4. From GameMasterManager/WordGridManager, grab a reference (e.g. via
///    [SerializeField] WordGameUIController ui;) and subscribe in Start/Awake:
///        ui.OnPlayClicked += () => GoToGameplayMenu(false);
///        ui.OnWordLengthChanged += SetLength;
///        ... etc.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class WordGameUIController : MonoBehaviour
{
    public const int MinWordLength = 1, MaxWordLength = 8;
    public const int MinGuesses = 1, MaxGuesses = 10;

    UIDocument document;
    VisualElement root;
    List<VisualElement> allPanels;

    // Panels
    VisualElement mainMenuPanel, howToPlayPanel, optionsPanel, gameplayPanel, winPanel, losePanel;
    Label toastLabel;

    // ---- Main menu ----
    Label dailyTimerLabel, dailyPuzzleLabel, wordLengthValueLabel, guessesValueLabel;
    Button dailyPlayButton, wordLengthMinusButton, wordLengthPlusButton, guessesMinusButton, guessesPlusButton;
    Button playButton, howToPlayButton, optionsButton, quitButton;

    // ---- How to play ----
    Button letsPlayButton;

    // ---- Options ----
    Button optionsCloseButton, deleteProgressButton;
    VisualElement hardModeToggle, dictionaryToggle, soundToggle;

    // ---- Gameplay ----
    Button backButton, helpButton;
    VisualElement gridContainer, keyboardContainer;
    readonly List<VisualElement> gridRowElements = new List<VisualElement>();
    readonly List<List<(VisualElement tile, Label label)>> gridTiles = new List<List<(VisualElement, Label)>>();
    readonly Dictionary<char, Button> keyButtons = new Dictionary<char, Button>();
    int activeRow = -1;

    // ---- Win screen ----
    Label winWordChipLabel, winStatPlayed, winStatWinrate, winStatStreak, winStatMaxStreak;
    Button winPlayAgainButton, winMainMenuButton, winHideButton;
    readonly VisualElement[] winDistBars = new VisualElement[6];

    // ---- Lose screen ----
    Label loseWordChipLabel, loseStatPlayed, loseStatWinrate, loseStatStreak, loseStatMaxStreak;
    Button loseTryAgainButton, loseMainMenuButton, loseHideButton;
    readonly VisualElement[] loseDistBars = new VisualElement[6];

    int wordLength = 5;
    int numGuesses = 6;

    public int WordLength => wordLength;
    public int NumberOfGuesses => numGuesses;

    // =====================================================================
    // Events — subscribe to these from your game logic
    // =====================================================================

    public event Action OnPlayClicked;
    public event Action OnDailyPlayClicked;
    public event Action OnHowToPlayClicked;
    public event Action OnOptionsClicked;
    public event Action OnQuitClicked;
    public event Action OnLetsPlayClicked;
    public event Action OnOptionsCloseClicked;
    public event Action OnDeleteProgressClicked;
    public event Action OnBackClicked;
    public event Action OnHelpClicked;

    public event Action OnPlayAgainClicked;   // win screen
    public event Action OnTryAgainClicked;    // lose screen
    public event Action OnMainMenuClicked;    // fired from either win or lose Main Menu button
    public event Action OnHideClicked;        // fired from either win or lose Hide button

    public event Action<int> OnWordLengthChanged;
    public event Action<int> OnGuessesChanged;
    public event Action<bool> OnHardModeChanged;
    public event Action<bool> OnDictionaryCheckChanged;
    public event Action<bool> OnSoundChanged;

    public event Action<char> OnKeyPressed;
    public event Action OnEnterPressed;
    public event Action OnBackspacePressed;

    // =====================================================================
    // Setup
    // =====================================================================

    void Awake()
    {
        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;

        CacheElements();
        BindEvents();

        allPanels = new List<VisualElement> { mainMenuPanel, howToPlayPanel, optionsPanel, gameplayPanel, winPanel, losePanel };
        ShowMainMenu();
    }

    void CacheElements()
    {
        mainMenuPanel = Get<VisualElement>("main-menu-panel");
        howToPlayPanel = Get<VisualElement>("how-to-play-panel");
        optionsPanel = Get<VisualElement>("options-panel");
        gameplayPanel = Get<VisualElement>("gameplay-panel");
        winPanel = Get<VisualElement>("win-panel");
        losePanel = Get<VisualElement>("lose-panel");
        toastLabel = Get<Label>("toast-label");

        // Main menu
        dailyTimerLabel = Get<Label>("daily-timer-label");
        dailyPuzzleLabel = Get<Label>("daily-puzzle-label");
        dailyPlayButton = Get<Button>("daily-play-button");
        wordLengthValueLabel = Get<Label>("word-length-value");
        guessesValueLabel = Get<Label>("guesses-value");
        wordLengthMinusButton = Get<Button>("word-length-minus");
        wordLengthPlusButton = Get<Button>("word-length-plus");
        guessesMinusButton = Get<Button>("guesses-minus");
        guessesPlusButton = Get<Button>("guesses-plus");
        playButton = Get<Button>("play-button");
        howToPlayButton = Get<Button>("how-to-play-button");
        optionsButton = Get<Button>("options-button");
        quitButton = Get<Button>("quit-button");

        // How to play
        letsPlayButton = Get<Button>("lets-play-button");

        // Options
        optionsCloseButton = Get<Button>("options-close-button");
        deleteProgressButton = Get<Button>("delete-progress-button");
        hardModeToggle = Get<VisualElement>("hard-mode-toggle");
        dictionaryToggle = Get<VisualElement>("dictionary-toggle");
        soundToggle = Get<VisualElement>("sound-toggle");

        // Gameplay
        backButton = Get<Button>("back-button");
        helpButton = Get<Button>("help-button");
        gridContainer = Get<VisualElement>("grid-container");
        keyboardContainer = Get<VisualElement>("keyboard-container");

        // Win
        winWordChipLabel = Get<Label>("win-word-chip");
        winStatPlayed = Get<Label>("win-stat-played");
        winStatWinrate = Get<Label>("win-stat-winrate");
        winStatStreak = Get<Label>("win-stat-streak");
        winStatMaxStreak = Get<Label>("win-stat-maxstreak");
        winPlayAgainButton = Get<Button>("win-play-again-button");
        winMainMenuButton = Get<Button>("win-main-menu-button");
        winHideButton = Get<Button>("win-hide-button");
        for (int i = 0; i < 6; i++) winDistBars[i] = Get<VisualElement>("win-dist-bar-" + (i + 1));

        // Lose
        loseWordChipLabel = Get<Label>("lose-word-chip");
        loseStatPlayed = Get<Label>("lose-stat-played");
        loseStatWinrate = Get<Label>("lose-stat-winrate");
        loseStatStreak = Get<Label>("lose-stat-streak");
        loseStatMaxStreak = Get<Label>("lose-stat-maxstreak");
        loseTryAgainButton = Get<Button>("lose-try-again-button");
        loseMainMenuButton = Get<Button>("lose-main-menu-button");
        loseHideButton = Get<Button>("lose-hide-button");
        for (int i = 0; i < 6; i++) loseDistBars[i] = Get<VisualElement>("lose-dist-bar-" + (i + 1));
    }

    /// <summary>Looks up a named element and logs a clear, specific error if it's missing —
    /// instead of returning null silently and causing a confusing NullReferenceException
    /// somewhere else later (e.g. inside BuildGrid()).</summary>
    T Get<T>(string elementName) where T : VisualElement
    {
        var element = root.Q<T>(elementName);
        if (element == null)
        {
            Debug.LogError($"[WordGameUIController] Could not find a '{typeof(T).Name}' named \"{elementName}\" " +
                $"in the UXML. Check that WordGameUI.uxml has an element with name=\"{elementName}\", " +
                "that the file hasn't been corrupted (e.g. smart quotes replacing straight quotes), " +
                "and that the UIDocument's Source Asset is pointing at this file.");
        }
        return element;
    }

    void BindEvents()
    {
        playButton.clicked += () => OnPlayClicked?.Invoke();
        dailyPlayButton.clicked += () => OnDailyPlayClicked?.Invoke();
        howToPlayButton.clicked += () => { ShowHowToPlay(); OnHowToPlayClicked?.Invoke(); };
        optionsButton.clicked += () => { ShowOptions(); OnOptionsClicked?.Invoke(); };
        quitButton.clicked += () =>
        {
            OnQuitClicked?.Invoke();
            SceneLoader.RequestReturnToHub();
        };

        wordLengthMinusButton.clicked += () => SetWordLength(wordLength - 1, notify: true);
        wordLengthPlusButton.clicked += () => SetWordLength(wordLength + 1, notify: true);
        guessesMinusButton.clicked += () => SetGuesses(numGuesses - 1, notify: true);
        guessesPlusButton.clicked += () => SetGuesses(numGuesses + 1, notify: true);

        letsPlayButton.clicked += () => { ShowMainMenu(); OnLetsPlayClicked?.Invoke(); };

        optionsCloseButton.clicked += () => { ShowMainMenu(); OnOptionsCloseClicked?.Invoke(); };
        deleteProgressButton.clicked += () => OnDeleteProgressClicked?.Invoke();

        hardModeToggle.RegisterCallback<ClickEvent>(_ =>
        {
            bool on = !hardModeToggle.ClassListContains("on");
            SetToggleVisual(hardModeToggle, on);
            OnHardModeChanged?.Invoke(on);
        });
        dictionaryToggle.RegisterCallback<ClickEvent>(_ =>
        {
            bool on = !dictionaryToggle.ClassListContains("on");
            SetToggleVisual(dictionaryToggle, on);
            OnDictionaryCheckChanged?.Invoke(on);
        });
        soundToggle.RegisterCallback<ClickEvent>(_ =>
        {
            bool on = !soundToggle.ClassListContains("on");
            SetToggleVisual(soundToggle, on);
            OnSoundChanged?.Invoke(on);
        });

        backButton.clicked += () => { ShowMainMenu(); OnBackClicked?.Invoke(); };
        helpButton.clicked += () => OnHelpClicked?.Invoke();

        winPlayAgainButton.clicked += () => OnPlayAgainClicked?.Invoke();
        winMainMenuButton.clicked += () => { ShowMainMenu(); OnMainMenuClicked?.Invoke(); };
        winHideButton.clicked += () => OnHideClicked?.Invoke();

        loseTryAgainButton.clicked += () => OnTryAgainClicked?.Invoke();
        loseMainMenuButton.clicked += () => { ShowMainMenu(); OnMainMenuClicked?.Invoke(); };
        loseHideButton.clicked += () => OnHideClicked?.Invoke();
    }

    // =====================================================================
    // Screen navigation
    // =====================================================================

    public void ShowMainMenu() => SetActivePanel(mainMenuPanel);
    public void ShowHowToPlay() => SetActivePanel(howToPlayPanel);
    public void ShowOptions() => SetActivePanel(optionsPanel);
    public void ShowGameplay() => SetActivePanel(gameplayPanel);
    public void ShowWinScreenPanel() => SetActivePanel(winPanel);
    public void ShowLoseScreenPanel() => SetActivePanel(losePanel);

    void SetActivePanel(VisualElement panel)
    {
        foreach (var p in allPanels) p.AddToClassList("hidden");
        panel.RemoveFromClassList("hidden");
    }

    // =====================================================================
    // Main menu
    // =====================================================================

    public void SetWordLength(int value, bool notify = false)
    {
        wordLength = Mathf.Clamp(value, MinWordLength, MaxWordLength);
        wordLengthValueLabel.text = wordLength.ToString();
        wordLengthMinusButton.SetEnabled(wordLength > MinWordLength);
        wordLengthPlusButton.SetEnabled(wordLength < MaxWordLength);
        if (notify) OnWordLengthChanged?.Invoke(wordLength);
    }

    public void SetGuesses(int value, bool notify = false)
    {
        numGuesses = Mathf.Clamp(value, MinGuesses, MaxGuesses);
        guessesValueLabel.text = numGuesses.ToString();
        guessesMinusButton.SetEnabled(numGuesses > MinGuesses);
        guessesPlusButton.SetEnabled(numGuesses < MaxGuesses);
        if (notify) OnGuessesChanged?.Invoke(numGuesses);
    }

    public void SetDailyTimerText(string text) => dailyTimerLabel.text = text;

    public void SetDailyPuzzleLabel(string text) => dailyPuzzleLabel.text = text;

    /// <summary>Greys out / disables the Daily button once today's puzzle is played.</summary>
    public void SetDailyPlayable(bool playable)
    {
        dailyPlayButton.SetEnabled(playable);
        dailyTimerLabel.style.display = playable ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // =====================================================================
    // Options
    // =====================================================================

    void SetToggleVisual(VisualElement toggle, bool on)
    {
        if (on) toggle.AddToClassList("on");
        else toggle.RemoveFromClassList("on");
    }

    /// <summary>Sync the visual state without firing the change event (e.g. on load from PlayerPrefs).</summary>
    public void SetHardMode(bool on) => SetToggleVisual(hardModeToggle, on);
    public void SetDictionaryCheck(bool on) => SetToggleVisual(dictionaryToggle, on);
    public void SetSound(bool on) => SetToggleVisual(soundToggle, on);

    // =====================================================================
    // Gameplay — grid
    // =====================================================================

    /// <summary>Rebuilds the guess grid for a given word length / number of guesses.</summary>
    public void BuildGrid(int rows, int cols)
    {
        gridContainer.Clear();
        gridRowElements.Clear();
        gridTiles.Clear();
        activeRow = -1;

        for (int r = 0; r < rows; r++)
        {
            var rowEl = new VisualElement();
            rowEl.AddToClassList("grid-row");
            gridContainer.Add(rowEl);
            gridRowElements.Add(rowEl);

            var tileRow = new List<(VisualElement, Label)>();
            for (int c = 0; c < cols; c++)
            {
                var tile = new VisualElement();
                tile.AddToClassList("tile");
                tile.AddToClassList("empty");

                var label = new Label("");
                tile.Add(label);

                rowEl.Add(tile);
                tileRow.Add((tile, label));
            }
            gridTiles.Add(tileRow);
        }
    }

    /// <summary>Sets a tile's letter and color state. Pass state = WORDBUTTONSTATE.EMPTY for an unrevealed/typed letter.</summary>
    public void SetTile(int row, int col, char letter, WORDBUTTONSTATE state)
    {
        if (row < 0 || row >= gridTiles.Count || col < 0 || col >= gridTiles[row].Count) return;
        var (tile, label) = gridTiles[row][col];
        label.text = letter == '\0' ? "" : letter.ToString();

        tile.RemoveFromClassList("empty");
        tile.RemoveFromClassList("green");
        tile.RemoveFromClassList("yellow");

        switch (state)
        {
            case WORDBUTTONSTATE.GREEN: tile.AddToClassList("green"); break;
            case WORDBUTTONSTATE.YELLOW: tile.AddToClassList("yellow"); break;
            default: tile.AddToClassList("empty"); break;
        }
    }

    public void ClearTile(int row, int col) => SetTile(row, col, '\0', WORDBUTTONSTATE.EMPTY);

    /// <summary>Highlights the row currently being typed into.</summary>
    public void SetActiveRow(int row)
    {
        if (activeRow >= 0 && activeRow < gridTiles.Count)
            foreach (var (tile, _) in gridTiles[activeRow]) tile.RemoveFromClassList("active");

        activeRow = row;

        if (activeRow >= 0 && activeRow < gridTiles.Count)
            foreach (var (tile, _) in gridTiles[activeRow]) tile.AddToClassList("active");
    }

    /// <summary>Wiggles a row left/right to indicate an invalid guess (matches WordRow.Shake in the old uGUI version).</summary>
    public void ShakeRow(int row)
    {
        if (row < 0 || row >= gridRowElements.Count) return;
        VisualElement rowEl = gridRowElements[row];

        int[] pattern = { 8, -8, 6, -6, 4, -4, 0 };
        int i = 0;
        IVisualElementScheduledItem item = null;
        item = rowEl.schedule.Execute(() =>
        {
            rowEl.style.translate = new Translate(pattern[i], 0);
            i++;
            if (i >= pattern.Length) item.Pause();
        }).Every(40);
    }

    // =====================================================================
    // Gameplay — keyboard
    // =====================================================================

    static readonly string[] KeyboardRows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };

    /// <summary>Builds the on-screen QWERTY keyboard. Call once per gameplay session (or once at startup).</summary>
    public void BuildKeyboard()
    {
        keyboardContainer.Clear();
        keyButtons.Clear();

        for (int r = 0; r < KeyboardRows.Length; r++)
        {
            var rowEl = new VisualElement();
            rowEl.AddToClassList("key-row");
            keyboardContainer.Add(rowEl);

            if (r == 2)
            {
                var enterBtn = MakeKey("ENTER", wide: true);
                enterBtn.clicked += () => OnEnterPressed?.Invoke();
                rowEl.Add(enterBtn);
            }

            foreach (char c in KeyboardRows[r])
            {
                var keyBtn = MakeKey(c.ToString(), wide: false);
                keyBtn.clicked += () => OnKeyPressed?.Invoke(c);
                rowEl.Add(keyBtn);
                keyButtons[c] = keyBtn;
            }

            if (r == 2)
            {
                var backBtn = MakeKey("⌫", wide: true);
                backBtn.clicked += () => OnBackspacePressed?.Invoke();
                rowEl.Add(backBtn);
            }
        }
    }

    Button MakeKey(string text, bool wide)
    {
        var btn = new Button { text = text };
        btn.AddToClassList("key");
        if (wide) btn.AddToClassList("wide");
        return btn;
    }

    static int StatePriority(WORDBUTTONSTATE s) => s switch
    {
        WORDBUTTONSTATE.GREEN => 3,
        WORDBUTTONSTATE.YELLOW => 2,
        _ => 1,
    };

    readonly Dictionary<char, WORDBUTTONSTATE> keyStates = new Dictionary<char, WORDBUTTONSTATE>();

    /// <summary>Colors a keyboard key, only upgrading (grey -> yellow -> green), never downgrading, matching Wordle behavior.</summary>
    public void SetKeyState(char c, WORDBUTTONSTATE state)
    {
        c = char.ToUpperInvariant(c);
        if (!keyButtons.TryGetValue(c, out var btn)) return;

        if (keyStates.TryGetValue(c, out var current) && StatePriority(current) >= StatePriority(state)) return;
        keyStates[c] = state;

        btn.RemoveFromClassList("green");
        btn.RemoveFromClassList("yellow");
        btn.RemoveFromClassList("grey");

        switch (state)
        {
            case WORDBUTTONSTATE.GREEN: btn.AddToClassList("green"); break;
            case WORDBUTTONSTATE.YELLOW: btn.AddToClassList("yellow"); break;
            default: btn.AddToClassList("grey"); break;
        }
    }

    public void ResetKeyboard()
    {
        keyStates.Clear();
        foreach (var btn in keyButtons.Values)
        {
            btn.RemoveFromClassList("green");
            btn.RemoveFromClassList("yellow");
            btn.RemoveFromClassList("grey");
        }
    }

    // =====================================================================
    // Toast / error message
    // =====================================================================

    public void ShowToast(string message, float duration = 1.5f)
    {
        toastLabel.text = message;
        toastLabel.RemoveFromClassList("hidden");
        toastLabel.schedule.Execute(() => toastLabel.AddToClassList("hidden"))
            .ExecuteLater((long)(duration * 1000));
    }

    // =====================================================================
    // Win / Lose screens
    // =====================================================================

    /// <param name="distribution">6 entries, index 0 = wins in 1 guess ... index 5 = wins in 6 guesses.</param>
    public void ShowWinScreen(string wordChipText, int played, float winPercent, int streak, int maxStreak, int[] distribution)
    {
        ShowWinScreenPanel();
        winWordChipLabel.text = wordChipText;
        winStatPlayed.text = played.ToString();
        winStatWinrate.text = winPercent.ToString("0.0") + "%";
        winStatStreak.text = streak.ToString();
        winStatMaxStreak.text = maxStreak.ToString();
        ApplyDistribution(winDistBars, distribution);
    }

    public void ShowLoseScreen(string word, int played, float winPercent, int streak, int maxStreak, int[] distribution)
    {
        ShowLoseScreenPanel();
        loseWordChipLabel.text = "The word was " + word;
        loseStatPlayed.text = played.ToString();
        loseStatWinrate.text = winPercent.ToString("0.0") + "%";
        loseStatStreak.text = streak.ToString();
        loseStatMaxStreak.text = maxStreak.ToString();
        ApplyDistribution(loseDistBars, distribution);
    }

    void ApplyDistribution(VisualElement[] bars, int[] counts)
    {
        int max = 1;
        for (int i = 0; i < 6 && i < counts.Length; i++) max = Mathf.Max(max, counts[i]);

        for (int i = 0; i < 6; i++)
        {
            if (bars[i] == null) continue;
            int count = i < counts.Length ? counts[i] : 0;
            float pct = count <= 0 ? 4f : Mathf.Max(4f, (count / (float)max) * 100f);
            bars[i].style.width = new Length(pct, LengthUnit.Percent);
        }
    }

    /// <summary>Call from HideWinScreen/ReShowWinScreen equivalents if you keep the win screen mounted behind the grid.</summary>
    public void SetWinPanelVisible(bool visible)
    {
        if (visible) winPanel.RemoveFromClassList("hidden");
        else winPanel.AddToClassList("hidden");
    }
}
