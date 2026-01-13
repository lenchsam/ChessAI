using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static BitboardHelpers;
using static MoveGenerator;

public struct Move
{

    public int StartingPos;
    public int EndingPos;

    public bool IsCapture;

    public PawnPromotion PawnPromotion;

    public Move(int from, int to, bool isCapture, PawnPromotion promotion)
    {
        StartingPos = from;
        EndingPos = to;
        IsCapture = isCapture;
        PawnPromotion = promotion;
    }
}
public enum PawnPromotion : byte
{
    None = 0,           //0000
    PromoteQueen = 1,   //0001
    PromoteRook = 2,    //0010
    PromoteBishop = 4,  //0100
    PromoteKnight = 8,  //1000
}
public enum Castling : byte {
    None = 0,       //0000
    WhiteKing =  1, //0001
    WhiteQueen = 2, //0010
    BlackKing =  4, //0100
    BlackQueen = 8, //1000

    AnyWhite = 3,   //0011
    AnyBlack = 12,  //1100
    All = 15,       //1111
}

public enum GameState
{
    Playing,
    Checkmate,
    Stalemate
}
public class Bitboards
{
    public UnityEvent<ulong> MovingPieceEvent = new UnityEvent<ulong>();
    public UnityEvent<GameState, bool> GameEnded = new UnityEvent<GameState, bool>();

    // ulong is a 64-bit unsigned integer
    // two hex digits (8 bits/1 byte) represents each row
    private ulong[] _bitboards = new ulong[12];
    private ulong _whitePiecesBB, _blackPiecesBB, _allPiecesBB;

    bool _isWhiteTurn = true;

    //used for fast lookups of which piece is on which square
    private Piece[] _boardSquares = new Piece[64];

    //FEN character to piece index
    private Dictionary<char, int> _fenToPiece = new Dictionary<char, int>() {
    {'P', (int)Piece.WhitePawn},   {'N', (int)Piece.WhiteKnight},
    {'B', (int)Piece.WhiteBishop}, {'R', (int)Piece.WhiteRook},
    {'Q', (int)Piece.WhiteQueen},  {'K', (int)Piece.WhiteKing},
    {'p', (int)Piece.BlackPawn},   {'n', (int)Piece.BlackKnight},
    {'b', (int)Piece.BlackBishop}, {'r', (int)Piece.BlackRook},
    {'q', (int)Piece.BlackQueen},  {'k', (int)Piece.BlackKing}
    };

    private List<Move> _currentLegalMoves = new List<Move>();

    Castling _whiteCastlingRights = Castling.AnyWhite;
    Castling _blackCastlingRights = Castling.AnyBlack;

    private void ResetGame()
    {
        _isWhiteTurn = true;

        for (int i = 0; i < _bitboards.Length; i++) 
        {
            _bitboards[i] = 0UL;
        }

        _whitePiecesBB = 0UL;
        _blackPiecesBB = 0UL;
        _allPiecesBB = 0UL;

    }

    public List<Move> GetCurrentLegalMoves()
    {
        return _currentLegalMoves;
    }

    public void SetTurn(bool isWhite)
    {
        _isWhiteTurn = isWhite;
    }

    public void FENtoBitboards(string FEN)
    {
        ResetGame();

        int row = 7;
        int col = 0;

        for (int i = 0; i < 64; i++) _boardSquares[i] = Piece.None;

        foreach (char c in FEN)
        {
            //done so it doesnt go into turn data.
            if (c == ' ') break;

            if (char.IsDigit(c))
            {
                //empty squares
                col += (c - '0');
            }
            else if (c == '/')
            {
                //new row
                row--;
                col = 0;
            }
            else
            {
                if (_fenToPiece.ContainsKey(c))
                {
                    int pieceIndex = _fenToPiece[c];
                    int squareIndex = row * 8 + col; // 0 to 63
                    ulong squareBit = 1UL << squareIndex;

                    _bitboards[pieceIndex] |= squareBit;

                    _boardSquares[squareIndex] = (Piece)pieceIndex;
                }
                col++;
            }
        }
        UpdateTotalBitboards();

        GenerateLegalMoves();
    }

    public Piece GetPieceOnSquare(int squareIndex)
    {
        return _boardSquares[squareIndex];
    }

    public bool MovePiece(int from, int to, PawnPromotion chosenPromotion = PawnPromotion.None)
    {
        //is move valid
        Move validMove = default;
        bool isValid = false;

        foreach (Move move in _currentLegalMoves)
        {
            if (move.StartingPos == from && move.EndingPos == to)
            {
                //if its promoting check chosen promotion type
                if (move.PawnPromotion != PawnPromotion.None)
                {
                    if (move.PawnPromotion == chosenPromotion)
                    {
                        validMove = move;
                        isValid = true;
                        break;
                    }
                }
                else //not promoting
                {
                    validMove = move;
                    isValid = true;
                    break;
                }
            }
        }

        if (!isValid) return false;

        //if it got here, the move is valid so execute the move

        //update game state
        ulong fromBit = 1UL << from;
        ulong toBit = 1UL << to;
        Piece movingPiece = _boardSquares[from];
        Piece targetPiece = _boardSquares[to];

        //removes castling rights if king moves
        switch (movingPiece)
        {
            case Piece.WhiteKing:
                _whiteCastlingRights = Castling.None;
                break;
            case Piece.BlackKing:
                _blackCastlingRights = Castling.None;
                break;
            case Piece.WhiteRook:
                if(from == 0)
                {
                    _whiteCastlingRights &= ~Castling.WhiteQueen;
                } 
                if(from == 7)
                {
                    _whiteCastlingRights &= ~Castling.WhiteKing;
                }
                break;
            case Piece.BlackRook:
                if (from == 56) 
                { 
                    _blackCastlingRights &= ~Castling.BlackQueen; 
                }
                if (from == 63)
                {
                    _blackCastlingRights &= ~Castling.BlackKing;
                }
                break;
        }

        //captures
        if (validMove.IsCapture && targetPiece != Piece.None)
        {
            _bitboards[(int)targetPiece] &= ~toBit;
        }

        //update moving piece bitboard
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);
        _boardSquares[to] = movingPiece;
        _boardSquares[from] = Piece.None;

        if (validMove.PawnPromotion != PawnPromotion.None)
        {
            //remove the pawn from the destination bitboard
            _bitboards[(int)movingPiece] &= ~toBit;

            Piece promotedPieceType = GetPromotedPiece(validMove.PawnPromotion, _isWhiteTurn);

            _bitboards[(int)promotedPieceType] |= toBit;

            _boardSquares[to] = promotedPieceType;
        }

        UpdateTotalBitboards();

        _isWhiteTurn = !_isWhiteTurn;

        //have to generate legal moves after turn switch immediately
        GenerateLegalMoves();

        GameState state = CheckGameState();
        if (state != GameState.Playing)
        {
            GameEnded.Invoke(state, !_isWhiteTurn);
        }

        return true;
    }

    private Piece GetPromotedPiece(PawnPromotion promotion, bool isWhite)
    {
        switch (promotion)
        {
            case PawnPromotion.PromoteQueen:
                return isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
            case PawnPromotion.PromoteRook:
                return isWhite ? Piece.WhiteRook : Piece.BlackRook;
            case PawnPromotion.PromoteBishop:
                return isWhite ? Piece.WhiteBishop : Piece.BlackBishop;
            case PawnPromotion.PromoteKnight:
                return isWhite ? Piece.WhiteKnight : Piece.BlackKnight;
            default:
                return isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
        }
    }

    public void InvokeEvent(int from)
    {
        ulong moves = 0UL;

        foreach (Move move in _currentLegalMoves)
        {
            if (move.StartingPos == from)
            {
                moves |= 1UL << move.EndingPos;
            }
        }

        MovingPieceEvent.Invoke(moves);
    }
    public ulong GetPositionBitboardFromPiece(Piece piece)
    {
        switch (piece) {
            case Piece.WhitePawn:
            case Piece.BlackPawn:
                return _bitboards[(int)Piece.WhitePawn] | _bitboards[(int)Piece.BlackPawn];
            case Piece.WhiteKnight:
            case Piece.BlackKnight:
                return _bitboards[(int)Piece.WhiteKnight] | _bitboards[(int)Piece.BlackKnight];
            case Piece.WhiteKing:
            case Piece.BlackKing:
                return _bitboards[(int)Piece.WhiteKing] | _bitboards[(int)Piece.BlackKing];
            case Piece.WhiteBishop:
            case Piece.BlackBishop:
                return _bitboards[(int)Piece.WhiteBishop] | _bitboards[(int)Piece.BlackBishop];
            case Piece.WhiteRook:
            case Piece.BlackRook:
                return _bitboards[(int)Piece.WhiteRook] | _bitboards[(int)Piece.BlackRook];
            case Piece.WhiteQueen:
            case Piece.BlackQueen:
                return _bitboards[(int)Piece.WhiteQueen] | _bitboards[(int)Piece.BlackQueen];
        }

        return 0UL;
    }
    private void UpdateTotalBitboards()
    {
        _whitePiecesBB = 0;
        _blackPiecesBB = 0;
        for (int i = 0; i <= 5; i++)
        {
            _whitePiecesBB |= _bitboards[i];
        }
        for (int i = 6; i <= 11; i++)
        {
            _blackPiecesBB |= _bitboards[i];
        }
        _allPiecesBB = _whitePiecesBB | _blackPiecesBB;
    }
    public bool IsPromotionMove(int from, int to)
    {
        Piece piece = _boardSquares[from];

        //is it a pawn
        if (piece != Piece.WhitePawn && piece != Piece.BlackPawn) return false;

        //is a pawn moving to last rank
        int toRank = to / 8;
        return toRank == 0 || toRank == 7;
    }

    #region Ending Conditions

    private int GetKingSquare(bool isWhite)
    {
        ulong kingBB = isWhite ? _bitboards[(int)Piece.WhiteKing] : _bitboards[(int)Piece.BlackKing];

        for (int i = 0; i < 64; i++)
        {
            if (((kingBB >> i) & 1UL) != 0UL)
                return i;
        }
        //not found
        return -1;
    }
    private void GenerateLegalMoves()
    {
        _currentLegalMoves.Clear();

        List<Move> pseudoMoves = new List<Move>();

        //get correct bitboards based on turn
        ulong pawns = _bitboards[_isWhiteTurn ? (int)Piece.WhitePawn : (int)Piece.BlackPawn];
        ulong knights = _bitboards[_isWhiteTurn ? (int)Piece.WhiteKnight : (int)Piece.BlackKnight];
        ulong bishops = _bitboards[_isWhiteTurn ? (int)Piece.WhiteBishop : (int)Piece.BlackBishop];
        ulong rooks = _bitboards[_isWhiteTurn ? (int)Piece.WhiteRook : (int)Piece.BlackRook];
        ulong queens = _bitboards[_isWhiteTurn ? (int)Piece.WhiteQueen : (int)Piece.BlackQueen];
        ulong king = _bitboards[_isWhiteTurn ? (int)Piece.WhiteKing : (int)Piece.BlackKing];

        MoveGenerator.GeneratePseudoLegalMoves(
            pawns, knights, bishops, rooks, queens, king,
            _allPiecesBB,
            _isWhiteTurn ? _whitePiecesBB : _blackPiecesBB,
            _isWhiteTurn ? _blackPiecesBB : _whitePiecesBB,
            _isWhiteTurn,
            ref _whiteCastlingRights, ref _blackCastlingRights,
            pseudoMoves
        );

        //filter out illegal moves that leave king in check
        foreach (var move in pseudoMoves)
        {
            if (MakeMoveAndCheckLegality(move))
            {
                _currentLegalMoves.Add(move);
            }
        }
    }
    private bool MakeMoveAndCheckLegality(Move move)
    {
        //save current state
        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;
        Piece movingPiece = _boardSquares[move.StartingPos];
        Piece capturedPiece = _boardSquares[move.EndingPos];

        ulong savedWhiteBB = _whitePiecesBB;
        ulong savedBlackBB = _blackPiecesBB;
        ulong savedAllBB = _allPiecesBB;
        ulong savedMovingPieceBB = _bitboards[(int)movingPiece];
        ulong savedCapturedPieceBB = (capturedPiece != Piece.None) ? _bitboards[(int)capturedPiece] : 0;


        //capture 
        if (capturedPiece != Piece.None)
        {
            _bitboards[(int)capturedPiece] &= ~toBit;
        }

        //make move
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);
        _boardSquares[move.EndingPos] = movingPiece;
        _boardSquares[move.StartingPos] = Piece.None;

        UpdateTotalBitboards();

        int kingSquare = GetKingSquare(_isWhiteTurn);

        //check if king is attacked
        bool isKingAttacked = MoveGenerator.IsSquareAttacked(
            kingSquare,
            !_isWhiteTurn, //by enemy
            _allPiecesBB,
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteKnight : (int)Piece.BlackKnight],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteBishop : (int)Piece.BlackBishop],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteRook : (int)Piece.BlackRook],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteQueen : (int)Piece.BlackQueen],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteKing : (int)Piece.BlackKing],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhitePawn : (int)Piece.BlackPawn]
        );

        //return state to normal
        _bitboards[(int)movingPiece] = savedMovingPieceBB;
        if (capturedPiece != Piece.None) _bitboards[(int)capturedPiece] = savedCapturedPieceBB;

        _whitePiecesBB = savedWhiteBB;
        _blackPiecesBB = savedBlackBB;
        _allPiecesBB = savedAllBB;

        _boardSquares[move.StartingPos] = movingPiece;
        _boardSquares[move.EndingPos] = capturedPiece;

        //return if king is safe
        return !isKingAttacked;
    }
    public GameState CheckGameState()
    {
        if (_currentLegalMoves.Count > 0)
        {
            return GameState.Playing;
        }

        int kingSquare = GetKingSquare(_isWhiteTurn);

        bool isInCheck = MoveGenerator.IsSquareAttacked(
            kingSquare,
            !_isWhiteTurn,
            _allPiecesBB,
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteKnight : (int)Piece.BlackKnight],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteBishop : (int)Piece.BlackBishop],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteRook : (int)Piece.BlackRook], 
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteQueen : (int)Piece.BlackQueen], 
            _bitboards[!_isWhiteTurn ? (int)Piece.WhiteKing : (int)Piece.BlackKing],
            _bitboards[!_isWhiteTurn ? (int)Piece.WhitePawn : (int)Piece.BlackPawn]
        );

        if (isInCheck)
        {
            return GameState.Checkmate;
        }
        else
        {
            return GameState.Stalemate;
        }
    }
    #endregion
}
