using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;

namespace ChessGame
{
public class AIPlayer : MonoBehaviour
{
    public static AIPlayer Instance;

    GameManager gameManager;
    MovesHandler movesHandler;
    Board board;
    BoardUI boardUI;

    Move moveToDo;

    // Windows (Editor) path - unchanged
    Process process;

    // Android path - Java ProcessBuilder based
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject androidProcess;
    AndroidJavaObject androidWriter;
    System.Threading.Thread readThread;
    volatile bool keepReading = false;
#endif

    volatile string pendingBestMoveNotation;
    volatile bool hasPendingBestMove = false;

    bool stockfishAvailable = false;

    int lastRequestedDepth = 5;

    public OpeningMoveSequence[] openingMoveSequences;
    Move[] openingMoves;

    public float minWaitTime = 2f, maxWaitTime = 2.5f;
    float lastStockfishMoveTime = 0f;
    float stockfishCurrentDelay;

    public int searchDepth = 3;
    public int quiescenceDepth = 4;

    bool shoudlMakeStockfishMove = false;

    private void Start() {
        Instance = this;

        gameManager = GetComponent<GameManager>();
        movesHandler = GetComponent<MovesHandler>();
        board = GetComponent<Board>();
        boardUI = GetComponent<BoardUI>();

        SetRandomOpeningSequence();
        SetupStockfish();
    }

    private void Update() {
        if (hasPendingBestMove){
            hasPendingBestMove = false;
            NotationToMove(pendingBestMoveNotation);
        }

        if (shoudlMakeStockfishMove && Time.time - lastStockfishMoveTime > stockfishCurrentDelay){
            MakeMove();
            shoudlMakeStockfishMove = false;
        }
    }

    private void OnDestroy(){
#if UNITY_ANDROID && !UNITY_EDITOR
        keepReading = false;
        try { androidProcess?.Call("destroy"); } catch {}
#else
        try { process?.Kill(); } catch {}
#endif
    }

    public void SetRandomOpeningSequence(){
        openingMoves = openingMoveSequences[Random.Range(0, openingMoveSequences.Length)].moves;
    }

    public void FlushPendingResponse(){
        hasPendingBestMove = false;
        pendingBestMoveNotation = null;
        shoudlMakeStockfishMove = false;
    }

    public void RequestStop(){
        if (!stockfishAvailable) return;
        try { SendLine("stop"); } catch { /* best-effort only */ }
    }

    public void MakeStockfishMove(int depth){
        lastRequestedDepth = depth;
        lastStockfishMoveTime = Time.time;
        stockfishCurrentDelay = Random.Range(minWaitTime, Mathf.Min(maxWaitTime, minWaitTime + board.moveIndex * 0.25f));

        if (!stockfishAvailable){
            UnityEngine.Debug.LogError("Stockfish is not available — falling back to MakeAIMove().");
            MakeAIMove();
            return;
        }

        GetStockfishMove(board.BoardToFenString(), depth);
    }

    public void MakeAIMove(){
        bool isWhiteAI = gameManager.isWhitesTurn;
        List<Move> moves = board.GetAllLegalMoves(movesHandler, isWhiteAI);

        if (moves.Count == 0){
            UnityEngine.Debug.Log("No moves found for ai");
            return;
        }

        int bestEval = isWhiteAI ? int.MinValue : int.MaxValue;
        List<Move> bestMoves = new List<Move>();
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        OrderMoves(moves);

        foreach (Move move in moves){
            int[] tempSquares = board.squares.Clone() as int[];
            board.TempMovePiece(move);

            int eval = Minimax(searchDepth - 1, alpha, beta, !isWhiteAI);

            board.squares = tempSquares;

            if (isWhiteAI){
                if (eval > bestEval){ bestEval = eval; bestMoves.Clear(); bestMoves.Add(move); }
                else if (eval == bestEval){ bestMoves.Add(move); }
                alpha = Mathf.Max(alpha, eval);
            } else {
                if (eval < bestEval){ bestEval = eval; bestMoves.Clear(); bestMoves.Add(move); }
                else if (eval == bestEval){ bestMoves.Add(move); }
                beta = Mathf.Min(beta, eval);
            }
        }

        moveToDo = bestMoves[Random.Range(0, bestMoves.Count)];
        UnityEngine.Debug.Log("AI is moving from " + moveToDo.ToString() + " (eval: " + bestEval + ")");

        Invoke("MakeMove", Random.Range(minWaitTime, Mathf.Min(maxWaitTime, minWaitTime + board.moveIndex * 0.25f)));
    }

    private int Minimax(int depth, int alpha, int beta, bool isWhiteToMove){
        if (depth == 0){
            return Quiescence(alpha, beta, isWhiteToMove, quiescenceDepth);
        }

        List<Move> moves = board.GetAllLegalMoves(movesHandler, isWhiteToMove);

        if (moves.Count == 0){
            return isWhiteToMove ? -100000 - depth : 100000 + depth;
        }

        OrderMoves(moves);

        if (isWhiteToMove){
            int maxEval = int.MinValue;
            foreach (Move move in moves){
                int[] tempSquares = board.squares.Clone() as int[];
                board.TempMovePiece(move);
                int eval = Minimax(depth - 1, alpha, beta, false);
                board.squares = tempSquares;

                if (eval > maxEval) maxEval = eval;
                if (eval > alpha) alpha = eval;
                if (beta <= alpha) break;
            }
            return maxEval;
        } else {
            int minEval = int.MaxValue;
            foreach (Move move in moves){
                int[] tempSquares = board.squares.Clone() as int[];
                board.TempMovePiece(move);
                int eval = Minimax(depth - 1, alpha, beta, true);
                board.squares = tempSquares;

                if (eval < minEval) minEval = eval;
                if (eval < beta) beta = eval;
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    private int Quiescence(int alpha, int beta, bool isWhiteToMove, int qDepth){
        int standPat = EvaluateBoard();

        if (qDepth <= 0) return standPat;

        if (isWhiteToMove){
            if (standPat >= beta) return beta;
            if (alpha < standPat) alpha = standPat;
        } else {
            if (standPat <= alpha) return alpha;
            if (beta > standPat) beta = standPat;
        }

        List<Move> moves = board.GetAllLegalMoves(movesHandler, isWhiteToMove);
        List<Move> captures = new List<Move>();
        foreach (Move m in moves){
            int targetPiece = board.GetPieceFromCoord(m.to);
            if (targetPiece != Piece.Empty && targetPiece != Piece.OutOfBounds) captures.Add(m);
        }

        OrderMoves(captures);

        foreach (Move move in captures){
            int[] tempSquares = board.squares.Clone() as int[];
            board.TempMovePiece(move);
            int eval = Quiescence(alpha, beta, !isWhiteToMove, qDepth - 1);
            board.squares = tempSquares;

            if (isWhiteToMove){
                if (eval > alpha) alpha = eval;
                if (alpha >= beta) return beta;
            } else {
                if (eval < beta) beta = eval;
                if (beta <= alpha) return alpha;
            }
        }

        return isWhiteToMove ? alpha : beta;
    }

    private void OrderMoves(List<Move> moves){
        moves.Sort((a, b) => GetMoveScore(b).CompareTo(GetMoveScore(a)));
    }

    private int GetMoveScore(Move move){
        int captured = board.GetPieceFromCoord(move.to);
        if (captured == Piece.Empty || captured == Piece.OutOfBounds) return 0;
        int attacker = board.GetPieceFromCoord(move.from);
        return GetPieceValue(captured) * 10 - GetPieceValue(attacker);
    }

    private int EvaluateBoard(){
        int score = 0;
        for (int i = 0; i < board.squares.Length; i++){
            int piece = board.squares[i];
            if (piece == Piece.Empty || piece == Piece.OutOfBounds) continue;

            int type = Piece.PieceType(piece);
            int color = Piece.PieceColor(piece);
            int value = GetPieceValue(piece);
            int pstIndex = (color == Piece.White) ? i : ((7 - (i / 8)) * 8 + (i % 8));
            value += GetPieceSquareValue(type, pstIndex);

            score += (color == Piece.White) ? value : -value;
        }
        return score;
    }

    private int GetPieceSquareValue(int type, int index){
        switch (type){
            case Piece.Pawn: return PawnTable[index];
            case Piece.Knight: return KnightTable[index];
            case Piece.Bishop: return BishopTable[index];
            case Piece.Rook: return RookTable[index];
            case Piece.Queen: return QueenTable[index];
            case Piece.King: return KingTable[index];
            default: return 0;
        }
    }

    private static readonly int[] PawnTable = {
         0,  0,  0,  0,  0,  0,  0,  0,
         5, 10, 10,-20,-20, 10, 10,  5,
         5, -5,-10,  0,  0,-10, -5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5,  5, 10, 25, 25, 10,  5,  5,
        10, 10, 20, 30, 30, 20, 10, 10,
        50, 50, 50, 50, 50, 50, 50, 50,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    private static readonly int[] KnightTable = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    };

    private static readonly int[] BishopTable = {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    };

    private static readonly int[] RookTable = {
         0,  0,  0,  5,  5,  0,  0,  0,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
         5, 10, 10, 10, 10, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    private static readonly int[] QueenTable = {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    private static readonly int[] KingTable = {
         20, 30, 10,  0,  0, 10, 30, 20,
         20, 20,  0,  0,  0,  0, 20, 20,
        -10,-20,-20,-20,-20,-20,-20,-10,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30
    };

    private int GetPieceValue(int piece){
        int type = Piece.PieceType(piece);
        if (type == Piece.Pawn) return 100;
        if (type == Piece.Knight) return 320;
        if (type == Piece.Bishop) return 330;
        if (type == Piece.Rook) return 500;
        if (type == Piece.Queen) return 900;
        if (type == Piece.King) return 20000;
        return 0;
    }

    private Move TryGetOpeningMove(bool isWhiteAI){
        int index = isWhiteAI ? (board.moveIndex + 1) / 2 : board.moveIndex / 2;
        if (index >= openingMoves.Length) return null;

        int fromRank = isWhiteAI ? openingMoves[index].from.rank : 7 - openingMoves[index].from.rank;
        int fromFile = isWhiteAI ? openingMoves[index].from.file : 7 - openingMoves[index].from.file;
        int toRank = isWhiteAI ? openingMoves[index].to.rank : 7 - openingMoves[index].to.rank;
        int toFile = isWhiteAI ? openingMoves[index].to.file : 7 - openingMoves[index].to.file;

        return new Move(new Coord(fromRank, fromFile), new Coord(toRank, toFile));
    }

    // ─────────────────────────────────────────────────────────────────────
    // STOCKFISH SETUP
    // ─────────────────────────────────────────────────────────────────────

    private void SetupStockfish(){
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            string nativeLibDir;
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject applicationInfo = currentActivity.Call<AndroidJavaObject>("getApplicationInfo"))
            {
                nativeLibDir = applicationInfo.Get<string>("nativeLibraryDir");
            }
            string filepath = nativeLibDir + "/libstockfish.so";

            AndroidJavaObject processBuilder = new AndroidJavaObject("java.lang.ProcessBuilder", new object[] { new string[] { filepath } });
            processBuilder.Call<AndroidJavaObject>("redirectErrorStream", true);
            androidProcess = processBuilder.Call<AndroidJavaObject>("start");

            AndroidJavaObject outputStream = androidProcess.Call<AndroidJavaObject>("getOutputStream");
            AndroidJavaObject inputStream = androidProcess.Call<AndroidJavaObject>("getInputStream");

            androidWriter = new AndroidJavaObject("java.io.PrintWriter", new object[] { outputStream, true });

            AndroidJavaObject bufferedReader = new AndroidJavaObject("java.io.BufferedReader",
                new AndroidJavaObject("java.io.InputStreamReader", inputStream));

            keepReading = true;
            readThread = new System.Threading.Thread(() => AndroidReadLoop(bufferedReader));
            readThread.IsBackground = true;
            readThread.Start();

            stockfishAvailable = true;

            SendLine("uci");
            SendLine("isready");

            UnityEngine.Debug.Log("Stockfish (Java ProcessBuilder) started OK: " + filepath);
        }
        catch (System.Exception e)
        {
            stockfishAvailable = false;
            UnityEngine.Debug.LogError("STOCKFISH FAILED (Java ProcessBuilder) - " + e.GetType().Name + ": " + e.Message);
        }
#else
        string filepath = System.IO.Directory.GetCurrentDirectory() + "\\Assets\\stockfish\\stockfish.exe";

        try
        {
            process = new Process();
            ProcessStartInfo si = new ProcessStartInfo()
            {
                FileName = filepath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            process.StartInfo = si;
            process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            stockfishAvailable = true;

            SendLine("uci");
            SendLine("isready");

            UnityEngine.Debug.Log("Stockfish (Windows) started OK: " + filepath);
        }
        catch (System.Exception e)
        {
            stockfishAvailable = false;
            process = null;
            UnityEngine.Debug.LogError("STOCKFISH FAILED TO START (Windows) - " + e.GetType().Name + ": " + e.Message);
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void AndroidReadLoop(AndroidJavaObject bufferedReader){
        AndroidJNI.AttachCurrentThread();
        try
        {
            while (keepReading)
            {
                string line = bufferedReader.Call<string>("readLine");
                if (line == null) break;

                if (line.Contains("bestmove"))
                {
                    HandleBestMoveLine(line);
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Stockfish Android read thread error: " + e.Message);
        }
        finally
        {
            AndroidJNI.DetachCurrentThread();
        }
    }
#endif

    private void GetStockfishMove(string forsythEdwardsNotationString, int depth){
        SendLine("ucinewgame");
        SendLine("position fen " + forsythEdwardsNotationString);
        SendLine("go depth " + depth);
    }

    private void SendLine(string command)
    {
        if (!stockfishAvailable){
            UnityEngine.Debug.LogError("Stockfish not available, cannot send: " + command);
            return;
        }

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            androidWriter.Call("println", command);
            androidWriter.Call("flush");
#else
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
#endif
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Stockfish write failed (" + e.GetType().Name + "): " + e.Message);
            stockfishAvailable = false;
        }
    }

    // Only used on the Windows/Editor path (System.Diagnostics.Process event)
    public static void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e){
        string data = e.Data;
        if (string.IsNullOrEmpty(data)) return;

        if (data.Contains("bestmove")){
            AIPlayer.Instance.HandleBestMoveLine(data);
        }
    }

    private void HandleBestMoveLine(string line){
        int idx = line.IndexOf("bestmove") + 9;
        if (idx + 4 > line.Length) return;

        string notation = line.Substring(idx, 4);
        pendingBestMoveNotation = notation;
        hasPendingBestMove = true;
    }

    public void NotationToMove(string notation){
        int fromRank = notation[0] - 96;
        int fromFile = int.Parse(notation[1].ToString());
        int toRank = notation[2] - 96;
        int toFile = int.Parse(notation[3].ToString());

        moveToDo = new Move(new Coord(fromRank - 1, fromFile - 1), new Coord(toRank - 1, toFile - 1));
        shoudlMakeStockfishMove = true;
    }

    private void MakeMove(){
        // FIXED: this used to check gameManager.gameRunning, which is still
        // FALSE for the very first move of every new match (it only becomes
        // true once the first move successfully completes). That meant if a
        // stale response from the previous match's engine search happened to
        // arrive right as a new match began, this function would exit here
        // and silently do nothing - no move, no retry - permanently
        // freezing the game. matchInProgress is true as soon as Start is
        // pressed, so the very first move can now be validated/retried
        // properly, while responses after a real match end are still
        // correctly ignored.
        if (!gameManager.matchInProgress) return;

        // Validate the move against the CURRENT board before applying it.
        // If it's not actually legal right now (leftover response from an
        // old/ended match), discard it and immediately ask for a fresh move.
        bool sideToMoveIsWhite = gameManager.isWhitesTurn;
        List<Move> legalMoves = movesHandler.GetLegalMoves(board, moveToDo.from, sideToMoveIsWhite);

        bool isValid = false;
        if (legalMoves != null){
            foreach (Move m in legalMoves){
                if (m.to.rank == moveToDo.to.rank && m.to.file == moveToDo.to.file){
                    moveToDo = m; // use the fully-formed legal move (carries castling rook info / en passant flag)
                    isValid = true;
                    break;
                }
            }
        }

        if (!isValid){
            UnityEngine.Debug.LogWarning("Discarding a stale/invalid engine move, requesting a fresh one.");
            MakeStockfishMove(lastRequestedDepth);
            return;
        }

        board.MovePiece(moveToDo);

        if (board.CanPromote(moveToDo.to)){
            board.Promote(moveToDo.to, Piece.Queen);
        }

        gameManager.MoveMade();
    }
}

[System.Serializable]
public class OpeningMoveSequence
{
    public string openingName;
    public Move[] moves;
}
}