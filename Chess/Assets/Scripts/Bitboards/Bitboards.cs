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
public enum Castling : byte
{
    BlackKing = 1,  //0001
    BlackQueen = 2, //0010
    WhiteKing = 4,  //0100
    WhiteQueen = 8, //1000

    AllWhite = WhiteKing | WhiteQueen,  //1100
    AllBlack = BlackKing | BlackQueen, //0011

    All = AllWhite | AllBlack,  //1111
    None = 0
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

    Castling CastlingRights = Castling.All;

    ushort _enPassantMask = 0;

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

    public bool MovePiece(int from, int to, out int movedFlag, PawnPromotion chosenPromotion = PawnPromotion.None)
    {
        movedFlag = MoveFlag.None;

        //is move valid
        Move validMove = default;
        bool isValid = false;

        //is move legal
        foreach (Move move in _currentLegalMoves.Moves)
        {
            if (move.StartingPos == from && move.EndingPos== to)
            {
                if (move.IsPromotion)
                {
                    //if its a promotion, check the chosen promotion type
                    Piece targetPieceType = GetPieceFromPromotionEnum(chosenPromotion);

                    if (move.PromotionPieceType == Piece.WhiteQueen && targetPieceType == Piece.WhiteQueen) isValid = true;
                    else if (move.PromotionPieceType == Piece.WhiteRook && targetPieceType == Piece.WhiteRook) isValid = true;
                    else if (move.PromotionPieceType == Piece.WhiteBishop && targetPieceType == Piece.WhiteBishop) isValid = true;
                    else if (move.PromotionPieceType == Piece.WhiteKnight && targetPieceType == Piece.WhiteKnight) isValid = true;

                    if (isValid)
                    {
                        validMove = move;
                        movedFlag = validMove.Flag;
                        break;
                    }
                }
                else
                {
                    //normal move
                    validMove = move;
                    isValid = true;
                    movedFlag = validMove.Flag;
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

        CheckCastlingRights(movingPiece, from);

        //captures
        if (validMove.Flag == MoveFlag.EnPassantCapture)
        {
            //capture behind the destination square
            int offset = _isWhiteTurn ? -8 : 8;
            int captureSquare = to + offset;

            Piece enemyPawn = _isWhiteTurn ? Piece.BlackPawn : Piece.WhitePawn;

            //remove captured pawn
            _bitboards[(int)enemyPawn] &= ~(1UL << captureSquare);
            _boardSquares[captureSquare] = Piece.None;
        }
        else if (validMove.IsCapture)
        {
            //standard capture
            _bitboards[(int)targetPiece] &= ~toBit;
        }
        #region castling
        if (validMove.Flag == MoveFlag.CastleKingSide)
        {
            if (_isWhiteTurn)
            {
                MoveRook(7, 5); // h1 -> f1
            }
            else
            {
                MoveRook(63, 61); // h8 -> f8
            }
        }
        if (validMove.Flag == MoveFlag.CastleQueenSide)
        {
            if (_isWhiteTurn)
            {
                MoveRook(0, 3); // a1 -> d1
            }
            else
            {
                MoveRook(56, 59); // a8 -> d8
            }
        }
        #endregion

        //update moving piece bitboard
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);
        _boardSquares[to] = movingPiece;
        _boardSquares[from] = Piece.None;

        #region pawns
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

        if (validMove.Flag == MoveFlag.PawnDoublePush)
        {
            int file = to % 8;
            //set file bit
            _enPassantMask = (ushort)(1 << file);
        }
        else
        {
            _enPassantMask = 0; //if not double push reset
        }

        #endregion

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
    private void MoveRook(int from, int to)
    {
        ulong fromBit = 1UL << from;
        ulong toBit = 1UL << to;

        Piece rook = _boardSquares[from];

        _bitboards[(int)rook] ^= (fromBit | toBit);
        _boardSquares[from] = Piece.None;
        _boardSquares[to] = rook;
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

    private void CheckCastlingRights(Piece movingPiece, int fromIndex)
    {
        switch (movingPiece)
        {
            case Piece.WhiteKing:
                CastlingRights &= ~Castling.AllWhite;
                break;

            case Piece.BlackKing:
                CastlingRights &= ~Castling.AllBlack;
                break;

            case Piece.WhiteRook:
                if (fromIndex == 0) CastlingRights &= ~Castling.WhiteQueen; //a1
                if (fromIndex == 7) CastlingRights &= ~Castling.WhiteKing;  //h1
                break;

            case Piece.BlackRook:
                if (fromIndex == 56) CastlingRights &= ~Castling.BlackQueen; //a8
                if (fromIndex == 63) CastlingRights &= ~Castling.BlackKing;  //h8
                break;
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
        _currentLegalMoves.Clear();
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
            _enPassantMask,
            CastlingRights,
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
    private bool MakeMoveAndCheckLegality(Move move)
    {
        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;
        Piece movingPiece = _boardSquares[move.StartingPos];
        Piece capturedPiece = _boardSquares[move.EndingPos];

        ulong savedWhiteBB = _whitePiecesBB;
        ulong savedBlackBB = _blackPiecesBB;
        ulong savedAllBB = _allPiecesBB;
        ulong savedMovingPieceBB = _bitboards[(int)movingPiece];

        //special variables for en passant
        //destination square is not where the captured pawn is
        Piece epCapturedPawn = Piece.None;
        int epCaptureSquare = -1;
        ulong savedEpPawnBB = 0;

        if (move.Flag == MoveFlag.EnPassantCapture)
        {
            //what pawn is being captured
            epCaptureSquare = _isWhiteTurn ? (move.EndingPos - 8) : (move.EndingPos + 8);
            epCapturedPawn = _boardSquares[epCaptureSquare];
            savedEpPawnBB = _bitboards[(int)epCapturedPawn];

            //delete it
            _bitboards[(int)epCapturedPawn] &= ~(1UL << epCaptureSquare);
            _boardSquares[epCaptureSquare] = Piece.None;
        }
        else if (capturedPiece != Piece.None)
        {
            //normal capture
            _bitboards[(int)capturedPiece] &= ~toBit;
        }

        //move piece
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);
        _boardSquares[move.EndingPos] = movingPiece;
        _boardSquares[move.StartingPos] = Piece.None;

        UpdateTotalBitboards();

        //is move legal (is king in check?)

        int kingSquare = GetKingSquare(_isWhiteTurn);

        bool isKingAttacked = MoveGenerator.IsSquareAttacked(
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

        //put everything back

        //put moving piece back
        _bitboards[(int)movingPiece] = savedMovingPieceBB;

        //put captured piece back
        if (move.Flag == MoveFlag.EnPassantCapture)
        {
            //put en passant captured pawn back
            _bitboards[(int)epCapturedPawn] = savedEpPawnBB;
            _boardSquares[epCaptureSquare] = epCapturedPawn;

            //make sure destination square is empty
            _boardSquares[move.EndingPos] = Piece.None;
        }
        else
        {
            //put captured piece back
            if (capturedPiece != Piece.None)
            {
                //add piece to board array
                _boardSquares[move.EndingPos] = capturedPiece;

                //re add the bitboard bit
                _bitboards[(int)capturedPiece] |= toBit;
            }
            else
            {
                _boardSquares[move.EndingPos] = Piece.None;
            }
        }

        _boardSquares[move.StartingPos] = movingPiece;

        _whitePiecesBB = savedWhiteBB;
        _blackPiecesBB = savedBlackBB;
        _allPiecesBB = savedAllBB;

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
