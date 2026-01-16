using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static BitboardHelpers;
using static MoveGenerator;

public struct Move
{
    //raw data
    private ushort Value;

    public Move(int from, int to, int flag)
    {
        //to: bits 0-5
        //from: bits 6-11
        //flag: bits 12-15
        Value = (ushort)((to & 0x3F) | ((from & 0x3F) << 6) | ((flag & 0xF) << 12));
    }

    //decoding
    public int EndingPos => Value & 0x3F;
    public int StartingPos => (Value >> 6) & 0x3F;
    public int Flag => (Value >> 12) & 0xF;

    public bool IsCapture => (Flag & MoveFlag.CaptureMask) != 0;

    public bool IsPromotion => (Flag & MoveFlag.PromotionMask) != 0;

    //get promotion piece type directly from flag
    public Piece PromotionPieceType
    {
        get
        {
            switch (Flag)
            {
                case MoveFlag.PromoteQueen: case MoveFlag.PromoteQueenCapture: return Piece.WhiteQueen;
                case MoveFlag.PromoteRook: case MoveFlag.PromoteRookCapture: return Piece.WhiteRook;
                case MoveFlag.PromoteBishop: case MoveFlag.PromoteBishopCapture: return Piece.WhiteBishop;
                case MoveFlag.PromoteKnight: case MoveFlag.PromoteKnightCapture: return Piece.WhiteKnight;
                default: return Piece.None;
            }
        }
    }
}

public struct MoveFlag
{
    public const int None = 0;                  //quiet Move
    public const int PawnDoublePush = 1;        //double pawn push
    public const int CastleKingSide = 2;        //king side castling
    public const int CastleQueenSide = 3;       //queen side castling
    public const int Capture = 4;               //capture
    public const int EnPassantCapture = 5;      //en passant capture

    //promotion
    public const int PromoteKnight = 8;
    public const int PromoteBishop = 9;
    public const int PromoteRook = 10;
    public const int PromoteQueen = 11;

    //romotion with capture
    public const int PromoteKnightCapture = 12;
    public const int PromoteBishopCapture = 13;
    public const int PromoteRookCapture = 14;
    public const int PromoteQueenCapture = 15;

    //masks for faster checking
    public const int PromotionMask = 8; // 1000 binary
    public const int CaptureMask = 4;   // 0100 binary
}

public enum PawnPromotion : byte
{
    None = 0,           //0000
    PromoteQueen = 1,   //0001
    PromoteRook = 2,    //0010
    PromoteBishop = 4,  //0100
    PromoteKnight = 8,  //1000
}
public enum GameState
{
    Playing,
    Checkmate,
    Stalemate
}
[System.Serializable]
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

    private CustomMovesList _currentLegalMoves = new CustomMovesList();

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

    public Move[] GetCurrentLegalMoves()
    {
        return _currentLegalMoves.Moves;
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

        GenerateLegalMoves(_currentLegalMoves);
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

        foreach (Move move in _currentLegalMoves.Moves)
        {
            if (move.StartingPos == from && move.EndingPos== to)
            {
                if (move.IsPromotion)
                {
                    //if its a promotion, check the chosen promotion type
                    Piece targetPieceType = GetPieceFromPromotionEnum(chosenPromotion);

                    // Compare the move's promotion type (e.g. WhiteQueen) with the user's choice
                    if (move.PromotionPieceType == Piece.WhiteQueen && targetPieceType == Piece.WhiteQueen) isValid = true;
                    else if (move.PromotionPieceType == Piece.WhiteRook && targetPieceType == Piece.WhiteRook) isValid = true;
                    else if (move.PromotionPieceType == Piece.WhiteBishop && targetPieceType == Piece.WhiteBishop) isValid = true;
                    else if (move.PromotionPieceType == Piece.WhiteKnight && targetPieceType == Piece.WhiteKnight) isValid = true;

                    if (isValid)
                    {
                        validMove = move;
                        break;
                    }
                }
                else
                {
                    //normal move
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

        //captures
        if (validMove.IsCapture)
        {
            _bitboards[(int)targetPiece] &= ~toBit;
        }

        //update moving piece bitboard
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);
        _boardSquares[to] = movingPiece;
        _boardSquares[from] = Piece.None;

        if (validMove.IsPromotion)
        {
            //remove the pawn from the destination bitboard
            _bitboards[(int)movingPiece] &= ~toBit;

            //get promoted piece type
            Piece promotedPieceType = validMove.PromotionPieceType;

            //if its blacks turn, adjust piece type
            if (!_isWhiteTurn)
            {
                promotedPieceType = (Piece)((int)promotedPieceType + 6);
            }

            //place promoted piece
            _bitboards[(int)promotedPieceType] |= toBit;
            _boardSquares[to] = promotedPieceType;
        }

        UpdateTotalBitboards();

        _isWhiteTurn = !_isWhiteTurn;

        //have to generate legal moves after turn switch immediately
        GenerateLegalMoves(_currentLegalMoves);
        GameState state = CheckGameState();
        if (state != GameState.Playing)
        {
            GameEnded.Invoke(state, !_isWhiteTurn);
        }

        return true;
    }

    private Piece GetPieceFromPromotionEnum(PawnPromotion promo)
    {
        switch (promo)
        {
            case PawnPromotion.PromoteQueen: return Piece.WhiteQueen;
            case PawnPromotion.PromoteRook: return Piece.WhiteRook;
            case PawnPromotion.PromoteBishop: return Piece.WhiteBishop;
            case PawnPromotion.PromoteKnight: return Piece.WhiteKnight;
            default: return Piece.None;
        }
    }

    public void InvokeEvent(int from)
    {
        ulong moves = 0UL;

        foreach (Move move in _currentLegalMoves.Moves)
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
    //public for perft test
    public int GenerateLegalMoves(CustomMovesList moves)
    {
        CustomMovesList pseudoMoves = new CustomMovesList();

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
            pseudoMoves
        );

        for (int i = 0; i < pseudoMoves.Length; i++)
        {
            Move move = pseudoMoves.Moves[i];

            if (MakeMoveAndCheckLegality(move))
            {
                moves.Add(move);
            }
        }
        return moves.Length;
    }
    ulong savedWhiteBB;
    ulong savedBlackBB;
    ulong savedAllBB;
    ulong savedMovingPieceBB;
    ulong savedCapturedPieceBB;
    private bool MakeMoveAndCheckLegality(Move move)
    {
        //save current state
        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;
        Piece movingPiece = _boardSquares[move.StartingPos];
        Piece capturedPiece = _boardSquares[move.EndingPos];

        savedWhiteBB = _whitePiecesBB;
        savedBlackBB = _blackPiecesBB;
        savedAllBB = _allPiecesBB;
        savedMovingPieceBB = _bitboards[(int)movingPiece];
        savedCapturedPieceBB = (capturedPiece != Piece.None) ? _bitboards[(int)capturedPiece] : 0;


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
        if (_currentLegalMoves.Length > 0)
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
    #region perf test methods
    public void MakeMove(Move move)
    {
        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;

        //which piece is moving
        Piece movingPiece = _boardSquares[move.StartingPos];

        Piece capturedPiece = _boardSquares[move.EndingPos];

        //is capture then remove captured piece
        if (capturedPiece != Piece.None)
        {
            _bitboards[(int)capturedPiece] &= ~toBit;
        }
        //handle en passant (no move will be en passant yet as move generator doesnt generate en passnant moves yet)
        else if (move.Flag == MoveFlag.EnPassantCapture)
        {
            //en passant capture is behind the ending pos
            int capturePos = _isWhiteTurn ? (move.EndingPos - 8) : (move.EndingPos + 8);
            Piece epPawn = _isWhiteTurn ? Piece.BlackPawn : Piece.WhitePawn;

            _bitboards[(int)epPawn] &= ~(1UL << capturePos);
            _boardSquares[capturePos] = Piece.None;
        }

        //handle promotion
        if (move.IsPromotion)
        {
            //remove pawn from pawn bitboard
            _bitboards[(int)movingPiece] &= ~fromBit;
            _boardSquares[move.StartingPos] = Piece.None;

            //which piece to promote to
            Piece promotedPiece = move.PromotionPieceType;
            //black enum adjustment
            if (!_isWhiteTurn) promotedPiece = (Piece)((int)promotedPiece + 6);

            //add promoted piece to its bitboard
            _bitboards[(int)promotedPiece] |= toBit;
            _boardSquares[move.EndingPos] = promotedPiece;
        }
        else
        {
            //move piece
            _bitboards[(int)movingPiece] ^= (fromBit | toBit);

            _boardSquares[move.EndingPos] = movingPiece;
            _boardSquares[move.StartingPos] = Piece.None;
        }

        UpdateTotalBitboards();
        _isWhiteTurn = !_isWhiteTurn;
    }

    public void UndoMove(Move move, Piece capturedPiece)
    {
        _isWhiteTurn = !_isWhiteTurn;

        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;

        //handle promotion differently as we have to remove the promoted piece and add back the pawn
        if (move.IsPromotion)
        {
            //remove promoted piece
            Piece promotedPiece = _boardSquares[move.EndingPos];
            if (promotedPiece != Piece.None)
                _bitboards[(int)promotedPiece] &= ~toBit;

            //place pawn back
            Piece pawn = _isWhiteTurn ? Piece.WhitePawn : Piece.BlackPawn;
            _bitboards[(int)pawn] |= fromBit;
            _boardSquares[move.StartingPos] = pawn;
        }
        else
        {
            //undo move
            Piece movingPiece = _boardSquares[move.EndingPos];

            if (movingPiece != Piece.None)
            {
                _bitboards[(int)movingPiece] ^= (fromBit | toBit);
                _boardSquares[move.StartingPos] = movingPiece;
            }
        }

        //put back captured piece
        //just moving back origional piece isnt enough as captured piece still needs to be put back

        //en passant not done in move generator yet so no moves currently have this flag
        if (move.Flag == MoveFlag.EnPassantCapture)
        {
            _boardSquares[move.EndingPos] = Piece.None;

            int capturePos = _isWhiteTurn ? (move.EndingPos - 8) : (move.EndingPos + 8);
            Piece epPawn = _isWhiteTurn ? Piece.BlackPawn : Piece.WhitePawn;

            _bitboards[(int)epPawn] |= (1UL << capturePos);
            _boardSquares[capturePos] = epPawn;
        }
        else if (capturedPiece != Piece.None)
        {
            _bitboards[(int)capturedPiece] |= toBit;
            _boardSquares[move.EndingPos] = capturedPiece;
        }
        else
        {
            _boardSquares[move.EndingPos] = Piece.None;
        }

        UpdateTotalBitboards();
    }
    #endregion
}
