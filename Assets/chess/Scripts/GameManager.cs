using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChessGame
{

public class GameManager : MonoBehaviour
{
    // Script References
    Board board;
    BoardUI boardUI;
    MovesHandler movesHandler;
    AIPlayer aiPlayer;
    UIManager uiManager;

    // Game Modes
    public enum GameMode
    {
        Local,
        Computer,
        Stockfish,
        AIvsAI
    }

    public GameMode gameMode;

    // Start Positions
    const string FEN_START = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
    const string FEN_CASTLING = "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R";
    const string FEN_CHECK_TEST = "2K5/q5QQ/4k3/8/8/8/8/8 b - - 0 2";

    // Game running (timer/clock ticking - only becomes true once the first
    // move of the match actually completes)
    public bool gameRunning = false;
    public bool canStartGame = false;

    // True from the moment "Start" is pressed until the match ends
    // (resign/checkmate/draw/timeout). Unlike gameRunning, this is already
    // true for the very FIRST move of a new match - AIPlayer uses this
    // (not gameRunning) to decide whether a just-arrived engine response
    // should still be processed/retried, otherwise the first move of every
    // new match could get silently dropped if a stale response from the
    // previous match's engine search happened to arrive at the wrong time.
    public bool matchInProgress = false;

    // Whose turn
    public bool startPlayerIsWhite = true;
    public bool isWhitesTurn = true;

    public bool boardFlipped = false;

    // Match duration
    float startTime = 300f;
    float whitesTimeLeft, blacksTimeLeft;

    // Stockfish search depth
    int stockfishDepth = 5;

    // Depth used for "Computer" mode - kept lower than full Stockfish mode so it's
    // still strong (no basic blunders) without being literally engine-max strength.
    // Depth 8-10 already comfortably plays well above 1300 ELO while staying fast.
    public int computerModeDepth = 8;

    // ── AI vs AI Mode ──────────────────────────────────────────────────────
    // Both sides are Stockfish. White ~3000 ELO, Black at "Computer" mode
    // strength. Depth is randomized a bit each move (within a range) so
    // matches actually differ from each other instead of always playing the
    // exact same moves and ending in the same repetition draw every time.
    [Header("AI vs AI Mode")]
    [Tooltip("Base search depth for the Stockfish side playing White. Formula: rating/200, so depth 15 ≈ 3000 rating.")]
    public int aiVsAiStockfishDepth = 15;

    [Tooltip("Base search depth for the Stockfish side playing Black (same strength as 'Computer' mode).")]
    public int aiVsAiComputerDepth = 8;

    [Tooltip("Each move, both sides' depth is randomly varied by up to ± this amount.")]
    public int aiVsAiDepthRandomRange = 3;

    [Tooltip("Depths never go below this, no matter how much randomization is subtracted.")]
    public int aiVsAiMinDepth = 4;
    // ─────────────────────────────────────────────────────────────────────

    // ── Flip Board button ─────────────────────────────────────────────────
    [Header("Flip Board Button")]
    [Tooltip("Flip Board button — hidden in Computer / Stockfish, disabled once game starts.")]
    [SerializeField] UnityEngine.UI.Button flipBoardButton;

    // ── Resign buttons ────────────────────────────────────────────────────
    [Header("Resign Buttons")]
    [Tooltip("Single resign button — shown in Computer / Stockfish / AI vs AI modes.")]
    [SerializeField] GameObject resignButtonSingle;

    [Tooltip("Resign button for White — shown only in Local mode.")]
    [SerializeField] GameObject resignButtonWhite;

    [Tooltip("Resign button for Black — shown only in Local mode.")]
    [SerializeField] GameObject resignButtonBlack;
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        boardUI = FindObjectOfType<BoardUI>();
        board = GetComponent<Board>();
        movesHandler = GetComponent<MovesHandler>();
        aiPlayer = GetComponent<AIPlayer>();
        uiManager = GetComponent<UIManager>();

        boardUI.CreateBoardUI();
        SetupGame();
    }

    private void Update()
    {
        if (gameRunning)
            UpdateTimer();
    }

    public void SetupGame()
    {
        board.PositionFromFen(FEN_START);
        boardUI.UpdateBoard(board, boardFlipped);
        boardUI.ResetAllSquareColors();
        uiManager.FlipBoard(boardFlipped);

        whitesTimeLeft = startTime;
        blacksTimeLeft = startTime;
        uiManager.UpdateTimer(true, whitesTimeLeft);
        uiManager.UpdateTimer(false, blacksTimeLeft);

        UpdateResignButtons();
        UpdateFlipBoardButton();
    }

    // Whether the engine should auto-move for the given color right now
    // (true for AI vs AI regardless of color, true for Computer/Stockfish
    // only on the side the human isn't playing).
    bool ShouldAutoMove(bool sideIsWhite)
    {
        if (gameMode == GameMode.AIvsAI) return true;
        if (gameMode == GameMode.Local) return false;
        return sideIsWhite != startPlayerIsWhite;
    }

    public void StartButtonPressed()
    {
        // CRITICAL FIX: reset the actual board position for a fresh match.
        // This was previously only ever done once in Awake() - every match
        // after the very first one kept whatever board state was left over
        // from the last game (e.g. a checkmated position with zero legal
        // moves for whoever's turn it is).
        SetupGame();

        aiPlayer.FlushPendingResponse();

        canStartGame = true;
        isWhitesTurn = true;
        matchInProgress = true;

        if (ShouldAutoMove(isWhitesTurn))
        {
            TryTriggerAIMove();
        }
    }

    public void StartGame()
    {
        gameRunning = true;
        canStartGame = false;

        whitesTimeLeft = startTime;
        blacksTimeLeft = startTime;

        board.moveIndex = 0;
        board.previousSquares.Clear();
        board.halfMoveClock = 0;
        board.positionHistory.Clear();
        aiPlayer.SetRandomOpeningSequence();

        // Disable flip board once the game has begun
        UpdateFlipBoardButton();
    }

    public void MoveMade()
    {
        if (!gameRunning)
            StartGame();

        // Safety net: force-promote any pawn stuck on the back rank before
        // doing any legality/checkmate/draw checks.
        board.AutoPromoteStrandedPawns();

        // The side about to move next (turn hasn't flipped yet at this point)
        bool nextSideIsWhite = !isWhitesTurn;
        List<Move> nextSideMoves = board.GetAllLegalMoves(movesHandler, nextSideIsWhite);

        // Record this position for threefold repetition checking
        board.RecordPosition(nextSideIsWhite);

        // ── No legal moves: checkmate or stalemate ──────────────────────
        if (nextSideMoves.Count == 0)
        {
            gameRunning = false;
            canStartGame = true;
            matchInProgress = false;
            aiPlayer.RequestStop();

            Coord kingCoord = board.FindKing(nextSideIsWhite);
            bool inCheck = kingCoord != null && movesHandler.IsSquareAttacked(board, kingCoord, !nextSideIsWhite);

            // The side that ran out of moves lost; the other side won
            bool winnerIsWhite = !nextSideIsWhite;

            isWhitesTurn = true; // reset turn indicator for next game

            if (inCheck)
            {
                // Checkmate
                if (gameMode == GameMode.Local)
                {
                    if (winnerIsWhite)
                        uiManager.OpenWinMenu("White won!", "White won by checkmate");
                    else
                        uiManager.OpenWinMenu("Black won!", "Black won by checkmate");
                }
                else if (gameMode == GameMode.AIvsAI)
                {
                    // White = Stockfish (3000 ELO), Black = Stockfish (Computer strength)
                    string winnerName = winnerIsWhite ? "Stockfish" : "AI";
                    uiManager.OpenWinMenu(winnerName + " won!", winnerName + " won by checkmate");
                }
                else
                {
                    // Computer / Stockfish: figure out if the WINNER is the human
                    // player or the AI by comparing to startPlayerIsWhite, instead
                    // of assuming player is always White.
                    bool playerWon = (winnerIsWhite == startPlayerIsWhite);

                    if (playerWon)
                    {
                        uiManager.OpenWinMenu("You won!", "You won by checkmate");
                    }
                    else
                    {
                        string opponentName = (gameMode == GameMode.Computer) ? "AI" : "Stockfish";
                        uiManager.OpenWinMenu(opponentName + " won!", "It won by checkmate");
                    }
                }
            }
            else
            {
                // No legal moves but king not in check = stalemate = draw
                uiManager.OpenWinMenu("Draw!", "Draw by stalemate");
            }

            boardUI.UpdateBoard(board, boardFlipped);
            boardUI.ResetAllSquareColors();
            return;
        }

        // ── 50-move rule ─────────────────────────────────────────────────
        if (board.halfMoveClock >= 100)
        {
            gameRunning = false;
            canStartGame = true;
            matchInProgress = false;
            aiPlayer.RequestStop();
            uiManager.OpenWinMenu("Draw!", "Draw by 50-move rule");
            boardUI.UpdateBoard(board, boardFlipped);
            boardUI.ResetAllSquareColors();
            return;
        }

        // ── Threefold repetition ─────────────────────────────────────────
        string currentKey = board.GetPositionKey(nextSideIsWhite);
        if (board.CountPositionOccurrences(currentKey) >= 3)
        {
            gameRunning = false;
            canStartGame = true;
            matchInProgress = false;
            aiPlayer.RequestStop();
            uiManager.OpenWinMenu("Draw!", "Draw by threefold repetition");
            boardUI.UpdateBoard(board, boardFlipped);
            boardUI.ResetAllSquareColors();
            return;
        }

        // ── Normal move continuation ──────────────────────────────────────
        isWhitesTurn = !isWhitesTurn;

        boardUI.UpdateBoard(board, boardFlipped);
        boardUI.ResetAllSquareColors();

        if (ShouldAutoMove(isWhitesTurn))
        {
            TryTriggerAIMove();
        }
    }

    // Safely triggers the AI/Stockfish move so an internal exception can never
    // freeze MoveMade() or leave the board UI un-updated.
    void TryTriggerAIMove()
    {
        try
        {
            int depth;

            if (gameMode == GameMode.AIvsAI)
            {
                // Both sides use Stockfish. Depth is randomized each move
                // (within a range around each side's base depth) purely so
                // matches vary instead of always playing identical lines.
                if (isWhitesTurn)
                {
                    int minD = Mathf.Max(aiVsAiMinDepth, aiVsAiStockfishDepth - aiVsAiDepthRandomRange);
                    int maxD = aiVsAiStockfishDepth + aiVsAiDepthRandomRange;
                    depth = Random.Range(minD, maxD + 1);
                }
                else
                {
                    int minD = Mathf.Max(aiVsAiMinDepth, aiVsAiComputerDepth - aiVsAiDepthRandomRange);
                    int maxD = aiVsAiComputerDepth + aiVsAiDepthRandomRange;
                    depth = Random.Range(minD, maxD + 1);
                }
            }
            else
            {
                depth = (gameMode == GameMode.Computer) ? computerModeDepth : stockfishDepth;
            }

            aiPlayer.MakeStockfishMove(depth);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("AI move trigger failed: " + e.GetType().Name + " - " + e.Message);
        }
    }

    // ── Resign ────────────────────────────────────────────────────────────

    public void Resign()
    {
        if (!gameRunning) return;
        gameRunning = false;
        matchInProgress = false;
        aiPlayer.RequestStop();

        switch (gameMode)
        {
            case GameMode.Local:
                ResignPlayer(isWhitesTurn);
                return;
            case GameMode.Computer:
                uiManager.OpenWinMenu("AI won!", "It won by resignation");
                break;
            case GameMode.Stockfish:
                uiManager.OpenWinMenu("Stockfish won!", "It won by resignation");
                break;
            case GameMode.AIvsAI:
                uiManager.OpenWinMenu("Match stopped", "The AI vs AI match was stopped");
                break;
        }
    }

    public void ResignWhite()
    {
        if (!gameRunning) return;
        gameRunning = false;
        matchInProgress = false;
        aiPlayer.RequestStop();
        ResignPlayer(true);
    }

    public void ResignBlack()
    {
        if (!gameRunning) return;
        gameRunning = false;
        matchInProgress = false;
        aiPlayer.RequestStop();
        ResignPlayer(false);
    }

    void ResignPlayer(bool whiteResigned)
    {
        if (whiteResigned)
            uiManager.OpenWinMenu("Black won!", "Black won by resignation");
        else
            uiManager.OpenWinMenu("White won!", "White won by resignation");
    }

    // ─────────────────────────────────────────────────────────────────────

    private void UpdateTimer()
    {
        if (isWhitesTurn)
        {
            whitesTimeLeft -= Time.deltaTime;
            if (whitesTimeLeft <= 0)
            {
                gameRunning = false;
                matchInProgress = false;
                aiPlayer.RequestStop();
                // White ran out of time -> Black won
                HandleTimeOutWin(winnerIsWhite: false);
            }
        }
        else
        {
            blacksTimeLeft -= Time.deltaTime;
            if (blacksTimeLeft <= 0)
            {
                gameRunning = false;
                matchInProgress = false;
                aiPlayer.RequestStop();
                // Black ran out of time -> White won
                HandleTimeOutWin(winnerIsWhite: true);
            }
        }

        uiManager.UpdateTimer(true, whitesTimeLeft);
        uiManager.UpdateTimer(false, blacksTimeLeft);
    }

    // Same "who's actually the human" fix applied here as in checkmate handling.
    void HandleTimeOutWin(bool winnerIsWhite)
    {
        if (gameMode == GameMode.Local)
        {
            uiManager.OpenWinMenu(winnerIsWhite ? "White won!" : "Black won!",
                                  (winnerIsWhite ? "White" : "Black") + " won by time");
        }
        else if (gameMode == GameMode.AIvsAI)
        {
            string winnerName = winnerIsWhite ? "Stockfish" : "AI";
            uiManager.OpenWinMenu(winnerName + " won!", winnerName + " won by time");
        }
        else
        {
            bool playerWon = (winnerIsWhite == startPlayerIsWhite);

            if (playerWon)
            {
                uiManager.OpenWinMenu("You won!", "You won by time");
            }
            else
            {
                string opponentName = (gameMode == GameMode.Computer) ? "AI" : "Stockfish";
                uiManager.OpenWinMenu(opponentName + " won!", "It won by time");
            }
        }
    }

    public void SetGameMode(string gamemode)
    {
        switch (gamemode)
        {
            case "Local":
                gameMode = GameMode.Local;
                break;
            case "Computer":
                gameMode = GameMode.Computer;
                break;
            case "Stockfish":
                gameMode = GameMode.Stockfish;
                break;
            case "AIvsAI":
                gameMode = GameMode.AIvsAI;
                break;
            default:
                return;
        }

        UpdateNames();
        UpdateResignButtons();
        UpdateFlipBoardButton();
    }

    public void SetStartColor(string color)
    {
        if (color == "white")
            startPlayerIsWhite = true;
        else if (color == "black")
            startPlayerIsWhite = false;
        else
            startPlayerIsWhite = Random.Range(0, 2) == 0;

        boardFlipped = !startPlayerIsWhite;
        boardUI.UpdateBoard(board, boardFlipped);
        uiManager.FlipBoard(boardFlipped);

        // Names depend on which color the human actually picked, so refresh
        // them here too (not just when the mode changes).
        UpdateNames();
    }

    // Assigns "Player" to whichever side (White/Black) the human is actually
    // playing, and the opponent's name to the other side. In AI vs AI mode
    // there's no human at all, so names are fixed: White = Stockfish, Black = AI.
    void UpdateNames()
    {
        switch (gameMode)
        {
            case GameMode.Local:
                uiManager.SetNames("Player White", "Player Black");
                break;

            case GameMode.Computer:
                if (startPlayerIsWhite)
                    uiManager.SetNames("Player", "AI");
                else
                    uiManager.SetNames("AI", "Player");
                break;

            case GameMode.Stockfish:
                if (startPlayerIsWhite)
                    uiManager.SetNames("Player", "Stockfish");
                else
                    uiManager.SetNames("Stockfish", "Player");
                break;

            case GameMode.AIvsAI:
                uiManager.SetNames("Stockfish", "AI");
                break;
        }
    }

    public void SetStartTime(float time) => startTime = time;
    public void SetStockfishDepth(float rating) => stockfishDepth = (int)(rating / 200);

    public void FlipBoard()
    {
        if (gameRunning || gameMode != GameMode.Local) return;

        boardFlipped = !boardFlipped;
        boardUI.UpdateBoard(board, boardFlipped);
        uiManager.FlipBoard(boardFlipped);
    }

    void UpdateFlipBoardButton()
    {
        if (flipBoardButton == null) return;

        bool isLocal = gameMode == GameMode.Local;

        flipBoardButton.gameObject.SetActive(isLocal);

        if (isLocal)
            flipBoardButton.interactable = !gameRunning;
    }

    void UpdateResignButtons()
    {
        bool isLocal = gameMode == GameMode.Local;

        if (resignButtonSingle != null)
            resignButtonSingle.SetActive(!isLocal);

        if (resignButtonWhite != null)
            resignButtonWhite.SetActive(isLocal);
        if (resignButtonBlack != null)
            resignButtonBlack.SetActive(isLocal);
    }
}
}
