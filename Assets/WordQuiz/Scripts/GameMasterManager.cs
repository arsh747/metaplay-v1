using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameMasterManager : MonoBehaviour
{

    public StateMachine<GameMasterManager> controller;
    public WordGameUIController ui;
    public WordGridManager gridManger;
    public DateTime currentTime;
    public bool DailyDisabled = false;

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Start()
    {

        // ---- restore saved word length / guess count ----
        // NOTE (migration): the old build stored TMP_Dropdown *indices* here
        // (0-7 for length, 0-9 for guesses). This version stores the actual
        // value the stepper shows. If you're upgrading an existing save,
        // either wipe these two keys once, or add +1 the first time you read
        // them. Fresh installs are unaffected.
        if (!gridManger.HasInt(WordGridManager.SELECTEDLENGTH)) gridManger.SetInt(WordGridManager.SELECTEDLENGTH, 4);
        if (!gridManger.HasInt(WordGridManager.NUMBEROFGUESSES)) gridManger.SetInt(WordGridManager.NUMBEROFGUESSES, 6);

        int savedLength = gridManger.GetInt(WordGridManager.SELECTEDLENGTH);
        int savedGuesses = gridManger.GetInt(WordGridManager.NUMBEROFGUESSES);

        ui.SetWordLength(savedLength);
        ui.SetGuesses(savedGuesses);
        SetLength(savedLength);
        SetNumberOfGuesses(savedGuesses);

        InitializeStringVars(new List<string>() { WordGridManager.CURRENTSTREAK, WordGridManager.LARGESTSTREAK, WordGridManager.NUMBEROFPUZZLESPLAYED, WordGridManager.NUMBERWINS, WordGridManager.WIN1, WordGridManager.WIN2, WordGridManager.WIN3, WordGridManager.WIN4, WordGridManager.WIN5, WordGridManager.WIN6 });

        if (!gridManger.HasBool(WordGridManager.DICTIONARYCHECK)) gridManger.SetBool(WordGridManager.DICTIONARYCHECK, true);
        if (!gridManger.HasBool(WordGridManager.HARDMODE)) gridManger.SetBool(WordGridManager.HARDMODE, false);
        if (!gridManger.HasBool("SOUND")) gridManger.SetBool("SOUND", true);

        ui.SetHardMode(gridManger.GetBool(WordGridManager.HARDMODE));
        ui.SetDictionaryCheck(gridManger.GetBool(WordGridManager.DICTIONARYCHECK));
        ui.SetSound(gridManger.GetBool("SOUND"));

        if (!gridManger.HasInt(WordGridManager.DAY)) gridManger.SetInt(WordGridManager.DAY, 0);
        if (!gridManger.HasInt(WordGridManager.MONTH)) gridManger.SetInt(WordGridManager.MONTH, 0);
        if (!gridManger.HasInt(WordGridManager.YEAR)) gridManger.SetInt(WordGridManager.YEAR, 0);

        // ---- wire UI events to game logic ----
        ui.OnPlayClicked += () => GoToGameplayMenu(false);
        ui.OnDailyPlayClicked += () => GoToGameplayMenu(true);
       
        ui.OnHelpClicked += ui.ShowHowToPlay; // the "?" chip in gameplay reuses the How To Play panel

        ui.OnWordLengthChanged += SetLength;
        ui.OnGuessesChanged += SetNumberOfGuesses;
        ui.OnHardModeChanged += SetHardMode;
        ui.OnDictionaryCheckChanged += SetDict;
        ui.OnSoundChanged += b => gridManger.SetBool("SOUND", b);
        ui.OnDeleteProgressClicked += DeleteProgress;

        ui.OnBackClicked += gridManger.BackToMainMenu; // panel switch already handled inside the controller

        ui.OnPlayAgainClicked += () => { gridManger.PlayAgain(); ui.ShowGameplay(); };
        ui.OnTryAgainClicked += () => { gridManger.PlayAgain(); ui.ShowGameplay(); };
        ui.OnMainMenuClicked += GoToMainMenu; // panel switch already handled inside the controller
        ui.OnHideClicked += ui.ShowGameplay;  // peek at the finished board; wire a "back to results" affordance if you want to return

        controller = new StateMachine<GameMasterManager>(new MainMenuState(), this);
        GoToMainMenu();
        GetWord();
    }

    public const string DAILY_EPOCH_YEAR = "DAILYEPOCHYEAR", DAILY_EPOCH_MONTH = "DAILYEPOCHMONTH", DAILY_EPOCH_DAY = "DAILYEPOCHDAY";

    public int GetDailyPuzzleNumber()
    {
        if (!gridManger.HasInt(DAILY_EPOCH_YEAR))
        {
            DateTime today = DateTime.Now.Date;
            gridManger.SetInt(DAILY_EPOCH_YEAR, today.Year);
            gridManger.SetInt(DAILY_EPOCH_MONTH, today.Month);
            gridManger.SetInt(DAILY_EPOCH_DAY, today.Day);
        }

        DateTime epoch = new DateTime(
            gridManger.GetInt(DAILY_EPOCH_YEAR),
            gridManger.GetInt(DAILY_EPOCH_MONTH),
            gridManger.GetInt(DAILY_EPOCH_DAY));

        return Mathf.Max(1, (int)(DateTime.Now.Date - epoch).TotalDays + 1);
    }

    int dailySeed = 0;
    public void DailyCheck()
    {
        ui.SetDailyPuzzleLabel("Puzzle #" + GetDailyPuzzleNumber());

        Vector3Int v = new Vector3Int();
        currentTime = DateTime.Now;
        v.x = currentTime.Month;
        v.y = currentTime.Day;
        v.z = currentTime.Year;
        dateVector = v;

        int day = gridManger.GetInt(WordGridManager.DAY);
        int month = gridManger.GetInt(WordGridManager.MONTH);
        int year = gridManger.GetInt(WordGridManager.YEAR);

        if (v.x == month && v.y == day && v.z == year)
        {
            DailyDisabled = true;
            ui.SetDailyPlayable(false);
        }
        else
        {
            DailyDisabled = false;
            dailySeed = v.GetHashCode();
            ui.SetDailyPlayable(true);
        }
    }

    public static Vector3Int dateVector;
    public static int selectedLength = 5, numberOfGuesses = 6;
    public static Dictionary<int, List<string>> commonWords = new Dictionary<int, List<string>>();
    public static Dictionary<int, List<string>> answerWords = new Dictionary<int, List<string>>();
    public static bool initYet = false;

    public List<WordBank> wordbanks = new List<WordBank>();

    public string GetWord()
    {
        if (!initYet) InitializeWordBanks();

        if (!commonWords.ContainsKey(selectedLength) || commonWords[selectedLength].Count == 0)
        {
            Debug.LogError($"[GameMasterManager] No words available for word length {selectedLength}. " +
                $"Check that a WordBank asset with Word Length = {selectedLength} exists in the Wordbanks list, " +
                "and that its Common Word Text field is assigned to a non-empty text file.");
            return null;
        }

        return commonWords[selectedLength].PickRandom();
    }

    /// <summary>Now takes the ACTUAL word length (1-8), matching the +/- stepper — not a dropdown index.</summary>
    public void SetLength(int length)
    {
        selectedLength = Mathf.Clamp(length, WordGameUIController.MinWordLength, WordGameUIController.MaxWordLength);
        gridManger.SetInt(WordGridManager.SELECTEDLENGTH, selectedLength);
    }

    /// <summary>Now takes the ACTUAL guess count (1-10), matching the +/- stepper — not a dropdown index.</summary>
    public void SetNumberOfGuesses(int guesses)
    {
        numberOfGuesses = Mathf.Clamp(guesses, WordGameUIController.MinGuesses, WordGameUIController.MaxGuesses);
        gridManger.SetInt(WordGridManager.NUMBEROFGUESSES, numberOfGuesses);
    }

    public void InitializeWordBanks()
    {
        commonWords = new Dictionary<int, List<string>>();
        answerWords = new Dictionary<int, List<string>>();
        foreach (WordBank b in wordbanks)
        {
            int len = b.wordLength;
            if (!commonWords.ContainsKey(len)) commonWords.Add(len, new List<string>());
            if (!answerWords.ContainsKey(len)) answerWords.Add(len, new List<string>());

            string contents = b.commonWordText.text;
            string[] w = contents.Split("\n");
            foreach (string s in w)
            {
                string sss = s.ToUpper();
                sss = sss.Substring(0, b.wordLength);
                commonWords[len].Add(sss);
                answerWords[len].Add(sss);
            }

            if (b.answerWordText != null)
            {
                contents = b.answerWordText.text;
                w = contents.Split("\n");
                foreach (string s in w)
                {
                    string sss = s.ToUpper();
                    sss = sss.Substring(0, b.wordLength);
                    answerWords[len].Add(sss);
                }
            }
        }
        initYet = true;
    }

    public static string jsonSaveFileName = "LASTPUZZLE.json";

    public string GetLastPuzzleSavePath()
    {
        return Application.persistentDataPath + "/" + jsonSaveFileName;
    }

    public LastPuzzle GetDefaultLastPuzzle()
    {
        LastPuzzle last = new LastPuzzle();
        last.grid = new WORDBUTTONSTATE[5, 6];
        return last;
    }

    // NOTE: the old menuDisplayPrefab/spawnedImages "last puzzle preview" grid on the
    // main menu isn't part of the new UXML layout. If you want it back, add a small
    // VisualElement grid to main-menu-panel and populate it here the same way
    // WordGridManager.BuildGrid()/SetTile() populate the gameplay grid.

    public void SetHardMode(bool b)
    {
        gridManger.SetBool(WordGridManager.HARDMODE, b);
    }

    public void SetDict(bool b)
    {
        gridManger.SetBool(WordGridManager.DICTIONARYCHECK, b);
    }

    public void InitializeStringVars(List<string> s)
    {
        foreach (string ss in s)
        {
            if (!gridManger.HasInt(ss)) gridManger.SetInt(ss, 0);
        }
    }

    public void SetStringVars(List<string> s, int val)
    {
        foreach (string ss in s) gridManger.SetInt(ss, val);
    }

    private void Update()
    {
        controller.Update();
    }

    public void GoToMainMenu()
    {
        controller.ChangeState(new MainMenuState());
        ui.ShowMainMenu();
        DailyCheck();
    }

    public void GoToOptionsMenu()
    {
        controller.ChangeState(new OptionsState());
        ui.ShowOptions();
    }

    public void GoToGameplayMenu(bool isDaily)
    {
        string word = GetWord();
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogError("[GameMasterManager] Could not start a game — no word available (see previous error). Staying on current screen.");
            return;
        }

        controller.ChangeState(new GameState());
        ui.ShowGameplay();
        if (isDaily)
        {
            UnityEngine.Random.InitState(dailySeed);
            gridManger.SetInt(WordGridManager.DAY, dateVector.y);
            gridManger.SetInt(WordGridManager.MONTH, dateVector.x);
            gridManger.SetInt(WordGridManager.YEAR, dateVector.z);
            gridManger.Setup(word, true);
        }
        else
        {
            UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
            gridManger.Setup(word, false);
        }
    }

    public void DeleteProgress()
    {
        SetStringVars(new List<string>() { WordGridManager.CURRENTSTREAK, WordGridManager.LARGESTSTREAK, WordGridManager.NUMBEROFPUZZLESPLAYED, WordGridManager.NUMBERWINS, WordGridManager.WIN1, WordGridManager.WIN2, WordGridManager.WIN3, WordGridManager.WIN4, WordGridManager.WIN5, WordGridManager.WIN6, WordGridManager.DAY, WordGridManager.MONTH, WordGridManager.YEAR }, 0);

        PlayerPrefs.DeleteKey(DAILY_EPOCH_YEAR);
        PlayerPrefs.DeleteKey(DAILY_EPOCH_MONTH);
        PlayerPrefs.DeleteKey(DAILY_EPOCH_DAY);
    }

    public void SaveLastPuzzle(LastPuzzle l)
    {
        File.WriteAllText(GetLastPuzzleSavePath(), JsonTool.ObjectToString(l));
    }
}

public class MainMenuState : State<GameMasterManager>
{
    public override void Update(StateMachine<GameMasterManager> obj)
    {
        if (obj.target.DailyDisabled)
        {
            DateTime current = DateTime.Now;
            DateTime tomorrow = current.AddDays(1).Date;
            int secondsUntilMidnight = (int)(tomorrow - current).TotalSeconds;

            int hours = secondsUntilMidnight / 3600;
            int minutes = (secondsUntilMidnight / 60) % 60;
            int seconds = secondsUntilMidnight % 60;

            obj.target.ui.SetDailyTimerText("Next in " + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00"));

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                obj.target.DailyCheck();
            }
        }
    }
}

public class GameState : State<GameMasterManager> { }

public class OptionsState : State<GameMasterManager> { }

[System.Serializable]
public class LastPuzzle
{
    public WORDBUTTONSTATE[,] grid;
}