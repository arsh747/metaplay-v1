using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{

public class Board : MonoBehaviour
{
    // Script references
    GameManager gm;
    UIManager ui;

    // Board squares
    public int[] squares;

    [HideInInspector]
    public int moveIndex = 0;

    // The square a pawn can be captured on via en passant (null if not available
    // this move). Set after a pawn double-move, cleared every other move.
    public Coord enPassantTarget = null;

    // Counts half-moves since the last pawn move or capture.
    // 100 half-moves (50 full moves) with no reset = draw.
    public int halfMoveClock = 0;

    // History of position keys (squares + turn + en passant), used to detect
    // threefold repetition.
    public List<string> positionHistory = new List<string>();

    // List of previous board positions
    // Used to go back in time
    public List<int[]> previousSquares = new List<int[]>();

    private void Start(){
        // Assign script reference
        gm = GetComponent<GameManager>();
        ui = GetComponent<UIManager>();
    }

    public void PositionFromFen(string fen){
        // Reset squares variable
        squares = new int[64];
        enPassantTarget = null;
        halfMoveClock = 0;
        positionHistory.Clear();

        // Split position string into ranks
        string[] fenParts = fen.Split(' ');
        string[] fenRanks = fenParts[0].Split('/');

        // Loop through rank strings
        for (int rank = 7; rank >= 0; rank--)
        {
            string fenRank = fenRanks[7 - rank];
            int fileOffset = 0;

            // Loop through rank string
            for (int file = 0; file < 8; file++)
            {
                // Check file is shorter than rank string length
                if (file >= fenRank.Length) break;
                
                // Get character from string
                char fenChar = fenRank[file];
                // Get piece color based on lowercase or uppercase
                int color = char.IsUpper(fenChar) ? Piece.White : Piece.Black;
                // Make character lower case
                fenChar = char.ToLower(fenChar);

                // Check what piece character repressents and set squares to piece and color
                switch (fenChar){
                    case 'p':
                        squares[rank * 8 + file + fileOffset] = color + Piece.Pawn;
                        break;
                    case 'n':
                        squares[rank * 8 + file + fileOffset] = color + Piece.Knight;
                        break;
                    case 'b':
                        squares[rank * 8 + file + fileOffset] = color + Piece.Bishop;
                        break;
                    case 'r':
                        squares[rank * 8 + file + fileOffset] = color + Piece.Rook;
                        break;
                    case 'q':
                        squares[rank * 8 + file + fileOffset] = color + Piece.Queen;
                        break;
                    case 'k':
                        squares[rank * 8 + file + fileOffset] = color + Piece.King;
                        break;
                    default:
                        // Go forwards amount of steps if character is number
                        int emptySquares = int.Parse(fenChar.ToString());
                        for (int i = 0; i < emptySquares; i++)
                        {
                            squares[rank * 8 + file + fileOffset + i] = Piece.Empty;
                        }
                        fileOffset += emptySquares - 1;
                        break;
                }
            }
        }

        // Whose turn?
        if (fenParts.Length >= 2 && gm != null){
            if (fenParts[1] == "w"){
                gm.isWhitesTurn = true;
            } else {
                gm.isWhitesTurn = false;
            }
        }
    }

    public string BoardToFenString(){
        // Create empty position string
        string fen = "";

        // Loop through each rank
        for (int rank = 7; rank >= 0; rank--)
        {
            int emptySquares = 0;
            // Loop through each file
            for (int file = 0; file < 8; file++)
            {
                int piece = squares[rank * 8 + file];
                if(piece == Piece.Empty){
                    emptySquares++;
                } else {
                    if(emptySquares > 0){
                        fen += emptySquares.ToString();
                        emptySquares = 0;
                    }

                    fen += Piece.PieceToFenChar(piece).ToString();
                }
            }

            // Add number of empty squares as number
            if(emptySquares > 0){
                fen += emptySquares.ToString();
            }

            // Add slash to make new rank in position string
            if(rank > 0){
                fen += "/";
            }
        }

        // Add whose turn to position string
        fen += (gm.isWhitesTurn ? " w" : " b");

        // Castling rights aren't tracked as a separate flag in this project -
        // legality is fully validated by MovesHandler before any move is made.
        fen += " -";

        // En passant target square - lets Stockfish/the AI know a capture is available
        fen += " " + (enPassantTarget != null ? CoordToFenSquare(enPassantTarget) : "-");

        // Halfmove clock / fullmove number - safe defaults for UCI
        fen += " 0 1";

        return fen;
    }

    private string CoordToFenSquare(Coord coord){
        char fileChar = (char)('a' + coord.rank);
        int rankNumber = coord.file + 1;
        return $"{fileChar}{rankNumber}";
    }

    public void MovePiece(Move move){
        moveIndex++;
        // Store current board positions
        previousSquares.Add(squares);

        // Move the piece
        int piece = squares[move.from.file * 8 + move.from.rank];
        int pieceType = Piece.PieceType(piece);

        bool isCapture = false;
        bool isPawnMove = (pieceType == Piece.Pawn);

        // En passant capture: the captured pawn is NOT on the "to" square
        if (move.isEnPassant){
            isCapture = true;
            int capturedIndex = move.from.file * 8 + move.to.rank;
            int capturedPawn = squares[capturedIndex];

            if (capturedPawn != Piece.Empty && capturedPawn != Piece.OutOfBounds){
                int captureColor = Piece.PieceColor(capturedPawn);
                ui.AddCapturedPiece(capturedPawn, captureColor == Piece.White);
            }

            squares[capturedIndex] = Piece.Empty;
        }

        squares[move.from.file * 8 + move.from.rank] = Piece.Empty;
        int capture = squares[move.to.file * 8 + move.to.rank];
        squares[move.to.file * 8 + move.to.rank] = piece;

        if (capture != Piece.Empty && capture != Piece.OutOfBounds){
            isCapture = true;
            int captureColor = Piece.PieceColor(capture);
            ui.AddCapturedPiece(capture, captureColor == Piece.White);
        }
        
        // Move secondary piece if it exists
        // Example is the rook when castling
        if (move.from2 != null && move.to2 != null){
            int piece2 = squares[move.from2.file * 8 + move.from2.rank];
            squares[move.from2.file * 8 + move.from2.rank] = Piece.Empty;
            squares[move.to2.file * 8 + move.to2.rank] = piece2;
        }

        // 50-move rule tracking: resets on pawn move or capture, otherwise increments
        if (isPawnMove || isCapture)
            halfMoveClock = 0;
        else
            halfMoveClock++;

        UpdateEnPassantTarget(move, piece);
    }

    // Sets/clears the en passant target square. Only a pawn double-move creates
    // one, and it only stays valid for the very next move.
    private void UpdateEnPassantTarget(Move move, int movedPiece){
        enPassantTarget = null;

        int type = Piece.PieceType(movedPiece);
        if (type == Piece.Pawn){
            int fileDelta = move.to.file - move.from.file;
            if (fileDelta == 2 || fileDelta == -2){
                int midFile = (move.from.file + move.to.file) / 2;
                enPassantTarget = new Coord(move.from.rank, midFile);
            }
        }
    }

    public void TempMovePiece(Move move){
        // Move the piece
        int piece = squares[move.from.file * 8 + move.from.rank];

        if (move.isEnPassant){
            int capturedIndex = move.from.file * 8 + move.to.rank;
            squares[capturedIndex] = Piece.Empty;
        }

        squares[move.from.file * 8 + move.from.rank] = Piece.Empty;
        squares[move.to.file * 8 + move.to.rank] = piece;

        // Move secondary piece if it exists
        // Example is the rook when castling
        if (move.from2 != null && move.to2 != null){
            int piece2 = squares[move.from2.file * 8 + move.from2.rank];
            squares[move.from2.file * 8 + move.from2.rank] = Piece.Empty;
            squares[move.to2.file * 8 + move.to2.rank] = piece2;
        }
    }

    public int GetPieceFromCoord(Coord coord){
        // Return out of bounds if outside board
        if (coord.file < 0 || coord.file > 7 || coord.rank < 0 || coord.rank > 7){
            return Piece.OutOfBounds;
        }

        // Get square index from file and rank
        int index = coord.file * 8 + coord.rank;
        
        // Return piece on square
        return squares[index];
    }

    public bool CanPromote(Coord coord){
        // Get piece from coord
        int piece = GetPieceFromCoord(coord);
        // Get piece type
        int pieceType = Piece.PieceType(piece);
        // Get piece color
        int pieceColor = Piece.PieceColor(piece);

        // If piece if pawn
        if(pieceType == Piece.Pawn){
            // If white and on last file
            if(pieceColor == Piece.White && coord.file == 7){
                return true;
            } 
            // If black and on first file
            else if(pieceColor == Piece.Black && coord.file == 0){
                return true;
            }
        }

        return false;
    }

    public void Promote(Coord coord, int newPiece){
        // Get piece from coord
        int piece = GetPieceFromCoord(coord);
        // Get color of piece
        int pieceColor = Piece.PieceColor(piece);
        // Replace piece with promoted piece of same color
        squares[coord.file * 8 + coord.rank] = pieceColor + newPiece;
    }

    // SAFETY NET: scans the whole board and force-promotes any pawn that has
    // somehow ended up sitting on the back rank without being converted.
    // This guarantees a pawn can never get permanently stuck as a pawn on
    // the last row, regardless of which code path caused it. Doesn't
    // interfere with deliberate human underpromotion (Knight/Rook/Bishop),
    // since by the time this runs the human's chosen piece is already placed.
    public void AutoPromoteStrandedPawns(){
        for (int i = 0; i < squares.Length; i++){
            int piece = squares[i];
            if (piece == Piece.Empty || piece == Piece.OutOfBounds) continue;
            if (Piece.PieceType(piece) != Piece.Pawn) continue;

            int fileIndex = i / 8; // matches Coord.file (0-7, vertical position)
            int color = Piece.PieceColor(piece);

            bool onLastRank = (color == Piece.White && fileIndex == 7) || (color == Piece.Black && fileIndex == 0);
            if (onLastRank){
                squares[i] = color + Piece.Queen;
            }
        }
    }

    public List<Move> GetAllLegalMoves(MovesHandler movesHandler, bool isWhite){
        // Get value of color
        int color = (isWhite ? Piece.White : Piece.Black);
        
        // Create empty moves list
        List<Move> moves = new List<Move>();

        // Loop through squares
        for (int i = 0; i < squares.Length; i++)
        {
            // Get color from square
            int pieceColor = Piece.PieceColor(squares[i]);

            // If colors match
            if (pieceColor == color){
                // Get legal moves from MovesHandler script
                List<Move> movesFromSquare = movesHandler.GetLegalMoves(this, new Coord(i % 8, i / 8), isWhite);
                
                // Add list to moves list if not null
                if (movesFromSquare != null){
                    moves.AddRange(movesFromSquare);
                }
            }

        }

        // Return moves list 
        return moves;
    }

    // Finds the king's coordinate for the given color. Returns null if somehow missing.
    public Coord FindKing(bool isWhite){
        int color = isWhite ? Piece.White : Piece.Black;
        for (int i = 0; i < squares.Length; i++){
            if (Piece.PieceType(squares[i]) == Piece.King && Piece.PieceColor(squares[i]) == color){
                return new Coord(i % 8, i / 8);
            }
        }
        return null;
    }

    // Builds a key representing the current position (pieces + whose turn +
    // en passant availability) - used for threefold repetition detection.
    public string GetPositionKey(bool isWhiteTurn){
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < squares.Length; i++){
            sb.Append(squares[i]);
            sb.Append(',');
        }
        sb.Append(isWhiteTurn ? 'w' : 'b');
        sb.Append(enPassantTarget != null ? (enPassantTarget.rank + "-" + enPassantTarget.file) : "-");
        return sb.ToString();
    }

    public void RecordPosition(bool isWhiteTurn){
        positionHistory.Add(GetPositionKey(isWhiteTurn));
    }

    public int CountPositionOccurrences(string key){
        int count = 0;
        foreach (string pos in positionHistory){
            if (pos == key) count++;
        }
        return count;
    }
}
}
