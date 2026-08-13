using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class WordGridManager : MonoBehaviour {

    public const string NUMBEROFPUZZLESPLAYED = "NUMBERPUZZLES", NUMBERWINS = "NUMBERWIN", WIN1 = "WIN1", WIN2 = "WIN2", WIN3 = "WIN3", WIN4 = "WIN4", WIN5 = "WIN5", WIN6 = "WIN6", CURRENTSTREAK = "CURRENTSTREAK", LARGESTSTREAK = "LARGESTSTREAK", HARDMODE = "HARDMODE", DICTIONARYCHECK = "DICTIONARYCHECK", MONTH = "MONTH", DAY = "DAY", YEAR = "YEAR", SELECTEDLENGTH = "SELECTEDLENGTH1", NUMBEROFGUESSES = "NUMBEROFGUESSES";

    public GameMasterManager manager;
    public WordGameUIController ui;
    public string currentWord = "";
    public int currentRow;
    public StateMachine<WordGridManager> gridController;
    public bool canEnterWords = false;
    public float errorLingerTime = 0.5f;
    public HashSet<char> necessaryCharacter = new HashSet<char>();

    string greenEmoji, yellowEmoji, emptyEmoji;

    string inputString = "";
    bool isDaily;
    bool keyboardBuilt = false;

    /// <summary>[letterIndex, guessRowIndex] — tracked locally since tile state now lives in
    /// UI Toolkit VisualElements rather than WordGridButton components we can query later.</summary>
    WORDBUTTONSTATE[,] gridStates;

    private void Awake() {
        greenEmoji = char.ConvertFromUtf32(0x1F7E9);
        yellowEmoji = char.ConvertFromUtf32(0x1F7E8);
        emptyEmoji = char.ConvertFromUtf32(0x2B1B);

        ui.OnKeyPressed += HandleKeyPressed;
        ui.OnEnterPressed += PlayerPressEnter;
        ui.OnBackspacePressed += HandleBackspace;
    }

    public void BackToMainMenu() {
        SetInt(CURRENTSTREAK, 0);
        manager.GoToMainMenu();
    }

    public void Setup(string s, bool isDailyPuzzle) {
        Debug.Log($"[WordGridManager] Setup() called with word: \"{s}\" (length {s?.Length ?? -1})");

        if (string.IsNullOrEmpty(s)) {
            Debug.LogError("[WordGridManager] Setup() received a null/empty word — aborting before building the grid.");
            return;
        }

        currentWord = s.ExceptChars(new List<char> { ' ', '\n', '\t' });
        if (string.IsNullOrEmpty(currentWord)) {
            Debug.LogError($"[WordGridManager] ExceptChars() turned \"{s}\" into a null/empty string — check MyExtensions.ExceptChars().");
            return;
        }
        currentWord = currentWord.ToUpper();

        isDaily = isDailyPuzzle;

        int numberPlayed = GetInt(NUMBEROFPUZZLESPLAYED) + 1;
        SetInt(NUMBEROFPUZZLESPLAYED, numberPlayed);

        currentRow = 0;
        inputString = "";
        necessaryCharacter = new HashSet<char>();
        gridStates = new WORDBUTTONSTATE[currentWord.Length, GameMasterManager.numberOfGuesses];

        Debug.Log($"[WordGridManager] gameObject=\"{gameObject.name}\" — ui field is {(ui == null ? "NULL" : "assigned (" + ui.gameObject.name + ")")}");
        ui.BuildGrid(GameMasterManager.numberOfGuesses, currentWord.Length);

        if (!keyboardBuilt) {
            ui.BuildKeyboard();
            keyboardBuilt = true;
        }
        ui.ResetKeyboard();
        ui.SetActiveRow(0);

        gridController = new StateMachine<WordGridManager>(new EnterWordState(), this);
    }

    void HandleKeyPressed(char c) {
        if (!canEnterWords) return;
        if (inputString.Length >= currentWord.Length) return;
        inputString += char.ToUpperInvariant(c);
        RefreshInputRow();
    }

    void HandleBackspace() {
        if (!canEnterWords) return;
        if (inputString.Length == 0) return;
        inputString = inputString.Substring(0, inputString.Length - 1);
        RefreshInputRow();
    }

    void RefreshInputRow() {
        for (int i = 0; i < currentWord.Length; i++) {
            char c = i < inputString.Length ? inputString[i] : '\0';
            ui.SetTile(currentRow, i, c, WORDBUTTONSTATE.EMPTY);
        }
    }

    public void ShowError(string s) {
        ui.ShowToast(s, errorLingerTime + 1f);
    }

    public void PlayerPressEnter() {
        if (!canEnterWords) return;

        if (inputString.Length < currentWord.Length) {
            ui.ShakeRow(currentRow);
            ShowError("Not enough letters");
            return;
        }

        bool valid = true;
        string s = inputString.ToUpper();

        if (GetBool(DICTIONARYCHECK) && currentWord.Length < 6
            && !GameMasterManager.answerWords[currentWord.Length].Contains(s)
            && !GameMasterManager.commonWords[currentWord.Length].Contains(s)) {
            ui.ShakeRow(currentRow);
            ShowError("Not in word list");
            valid = false;
        }

        if (valid && GetBool(HARDMODE)) {
            foreach (char c in necessaryCharacter) {
                if (s.IndexOf(c) < 0) { valid = false; break; }
            }
            if (!valid) {
                ui.ShakeRow(currentRow);
                ShowError("Must contain all previously revealed characters");
            }
        }

        if (valid) {
            gridController.ChangeState(new WordAnimationState());
        }
    }

    public bool YellowCheck(string input, int i) {
        char a = input[i];
        for (int x = 0; x < input.Length; x++) {
            if (x != i && currentWord[x] == a && input[x] != currentWord[x]) {
                return true;
            }
        }
        return false;
    }

    public IEnumerator WordAnimationFlip() {
        yield return null;

        string s = inputString.ToUpper();
        int rowIndex = currentRow;
        bool allCorrect = true;

        for (int i = 0; i < currentWord.Length; i++) {
            char c = s[i];
            WORDBUTTONSTATE state;

            if (c == currentWord[i]) {
                state = WORDBUTTONSTATE.GREEN;
                necessaryCharacter.Add(c);
            } else if (currentWord.IndexOf(c) >= 0 && YellowCheck(s, i)) {
                state = WORDBUTTONSTATE.YELLOW;
                necessaryCharacter.Add(c);
                allCorrect = false;
            } else {
                state = WORDBUTTONSTATE.EMPTY;
                allCorrect = false;
            }

            gridStates[i, rowIndex] = state;
            ui.SetTile(rowIndex, i, c, state);
            ui.SetKeyState(c, state);

            yield return new WaitForSeconds(0.15f);
        }

        currentRow = rowIndex + 1;

        if (allCorrect) {
            gridController.ChangeState(new WinState(true));
        } else if (currentRow >= GameMasterManager.numberOfGuesses) {
            gridController.ChangeState(new WinState(false));
        } else {
            inputString = "";
            ui.SetActiveRow(currentRow);
            gridController.ChangeState(new EnterWordState());
        }
    }

    /// <summary>Grid state getter used by WinState when building the LastPuzzle save + share text.</summary>
    public WORDBUTTONSTATE GetGridState(int letterIndex, int rowIndex) {
        if (gridStates == null) return WORDBUTTONSTATE.EMPTY;
        if (letterIndex < 0 || letterIndex >= gridStates.GetLength(0)) return WORDBUTTONSTATE.EMPTY;
        if (rowIndex < 0 || rowIndex >= gridStates.GetLength(1)) return WORDBUTTONSTATE.EMPTY;
        return gridStates[letterIndex, rowIndex];
    }

    public void CopyResultsToClipboard() {
        string result = "Word Game " + (currentRow) + "/" + GameMasterManager.numberOfGuesses + "\n\n";

        LastPuzzle puzzle = null;
        if (File.Exists(manager.GetLastPuzzleSavePath())) {
            puzzle = JsonTool.StringToObject<LastPuzzle>(File.ReadAllText(manager.GetLastPuzzleSavePath()));
        }

        if (puzzle != null && puzzle.grid != null) {
            int width = puzzle.grid.GetLength(0);
            int height = puzzle.grid.GetLength(1);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    switch (puzzle.grid[x, y]) {
                        case WORDBUTTONSTATE.GREEN: result += greenEmoji; break;
                        case WORDBUTTONSTATE.YELLOW: result += yellowEmoji; break;
                        case WORDBUTTONSTATE.EMPTY: result += emptyEmoji; break;
                    }
                }
                result += '\n';
            }
        }

        try { result.CopyToClipboard(); } catch { }
        try { ClipboardExtension.CopyToClipboard(result); } catch { }
        try {
            TextEditor t = new TextEditor();
            t.text = result;
            t.SelectAll();
            t.Copy();
        } catch { }

        try {
#if UNITY_WEBGL
            passCopyToBrowser(result);
#endif
        } catch { }
    }

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void passCopyToBrowser(string str);
#endif

    public void SetInt(string s, int val) => PlayerPrefs.SetInt(s, val);
    public bool HasInt(string s) => PlayerPrefs.HasKey(s);
    public int GetInt(string s) => PlayerPrefs.GetInt(s);

    public void SetBool(string s, bool b) => PlayerPrefs.SetInt(s, b ? 1 : 0);
    public bool HasBool(string s) => PlayerPrefs.HasKey(s);
    public bool GetBool(string s) => PlayerPrefs.GetInt(s) > 0;

    public void PlayAgain() {
        Setup(manager.GetWord(), false);
    }
}

public class EnterWordState : State<WordGridManager> {
    public override void Enter(StateMachine<WordGridManager> obj) {
        obj.target.canEnterWords = true;
    }

    public override void Exit(StateMachine<WordGridManager> obj) {
        obj.target.canEnterWords = false;
    }
}

public class WordAnimationState : State<WordGridManager> {
    public override void Enter(StateMachine<WordGridManager> obj) {
        obj.target.canEnterWords = false;
        obj.target.StartCoroutine(obj.target.WordAnimationFlip());
    }
}

public class WinState : State<WordGridManager> {

    public bool win = true;

    public WinState(bool b) {
        win = b;
    }

    public IEnumerator ShowScreen(StateMachine<WordGridManager> obj) {
        yield return new WaitForSeconds(1f);

        WordGridManager t = obj.target;

        if (win) {
            int toAdd = 0;
            switch (t.currentRow) {
                case 1: toAdd = t.GetInt(WordGridManager.WIN1) + 1; t.SetInt(WordGridManager.WIN1, toAdd); break;
                case 2: toAdd = t.GetInt(WordGridManager.WIN2) + 1; t.SetInt(WordGridManager.WIN2, toAdd); break;
                case 3: toAdd = t.GetInt(WordGridManager.WIN3) + 1; t.SetInt(WordGridManager.WIN3, toAdd); break;
                case 4: toAdd = t.GetInt(WordGridManager.WIN4) + 1; t.SetInt(WordGridManager.WIN4, toAdd); break;
                case 5: toAdd = t.GetInt(WordGridManager.WIN5) + 1; t.SetInt(WordGridManager.WIN5, toAdd); break;
                case 6: toAdd = t.GetInt(WordGridManager.WIN6) + 1; t.SetInt(WordGridManager.WIN6, toAdd); break;
            }
        }

        int currentStreak = win ? t.GetInt(WordGridManager.CURRENTSTREAK) + 1 : 0;
        t.SetInt(WordGridManager.CURRENTSTREAK, currentStreak);
        if (currentStreak > t.GetInt(WordGridManager.LARGESTSTREAK)) {
            t.SetInt(WordGridManager.LARGESTSTREAK, currentStreak);
        }

        float numberOfWins = t.GetInt(WordGridManager.NUMBERWINS);
        if (win) numberOfWins++;
        t.SetInt(WordGridManager.NUMBERWINS, (int)numberOfWins);

        int played = t.GetInt(WordGridManager.NUMBEROFPUZZLESPLAYED);
        float winPercent = played > 0 ? (numberOfWins / played) * 100f : 0f;
        int maxStreak = t.GetInt(WordGridManager.LARGESTSTREAK);

        int[] distribution = new int[6];
        for (int i = 0; i < 6; i++) distribution[i] = t.GetInt("WIN" + (i + 1));

        if (win) {
            t.ui.ShowWinScreen("Solved in " + t.currentRow + " guesses", played, winPercent, currentStreak, maxStreak, distribution);
        } else {
            t.ui.ShowLoseScreen(t.currentWord, played, winPercent, currentStreak, maxStreak, distribution);
        }
    }

    public override void Enter(StateMachine<WordGridManager> obj) {
        WordGridManager t = obj.target;
        t.canEnterWords = false;
        t.StartCoroutine(ShowScreen(obj));

        LastPuzzle l = new LastPuzzle();
        l.grid = new WORDBUTTONSTATE[t.currentWord.Length, t.currentRow];

        for (int y = 0; y < t.currentRow; y++) {
            for (int x = 0; x < t.currentWord.Length; x++) {
                l.grid[x, y] = t.GetGridState(x, y);
            }
        }

        t.manager.SaveLastPuzzle(l);
    }
}

public static class ClipboardExtension {
    public static void CopyToClipboard(this string str) {
        GUIUtility.systemCopyBuffer = str;
    }
}
