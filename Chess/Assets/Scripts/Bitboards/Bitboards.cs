using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

public struct Material
{
    public ulong Pawns;
    public ulong Knights;
    public ulong Queens;
    public ulong King;
    public ulong Bishops;
    public ulong Rooks;

    public Material(ulong pawns, ulong knights, ulong bishop, ulong rooks, ulong queens, ulong king)
    {
        Pawns = pawns;
        Knights = knights;
        Queens = queens;
        King = king;
        Bishops = bishop;
        Rooks = rooks;
    }

    public int SumOfMaterialNumbers(int pawnValue, int knightValue, int bishopValue, int rookValue, int queenValue )
    {
        int sum = 0;

        int pawnsEval = BitboardHelpers.CountBits(Pawns) * pawnValue;
        int knightsEval = BitboardHelpers.CountBits(Knights) * knightValue;
        int queensEval = BitboardHelpers.CountBits(Queens) * queenValue;
        int numKing = BitboardHelpers.CountBits(King);
        int bishopsEval = BitboardHelpers.CountBits(Bishops) * bishopValue;
        int rooksEval = BitboardHelpers.CountBits(Rooks) * rookValue;

        sum = pawnsEval + knightsEval + queensEval + bishopsEval + rooksEval;

        return sum;
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
public enum EndingState
{
    Playing,
    Checkmate,
    Stalemate
}


[System.Serializable]
public class Bitboards
{
    public UnityEvent<ulong> MovingPieceEvent = new UnityEvent<ulong>();
    public UnityEvent<EndingState, bool> GameEnded = new UnityEvent<EndingState, bool>();

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

    public Castling CastlingRights = Castling.All;

    public ushort EnPassantMask = 0;

    private Stack<GameState> _gameStateHistory = new Stack<GameState>();

    private void ResetGame()
    {
        _isWhiteTurn = true;
        _gameStateHistory.Clear();

        for (int i = 0; i < _bitboards.Length; i++) 
        {
            _bitboards[i] = 0UL;
        }

        _whitePiecesBB = 0UL;
        _blackPiecesBB = 0UL;
        _allPiecesBB = 0UL;

    }

    public Material GetMaterialForColour(bool isWhite)
    {
        ulong pawns = _bitboards[isWhite ? (int)Piece.WhitePawn : (int)Piece.BlackPawn];
        ulong knights = _bitboards[isWhite ? (int)Piece.WhiteKnight : (int)Piece.BlackKnight];
        ulong bishops = _bitboards[isWhite ? (int)Piece.WhiteBishop : (int)Piece.BlackBishop];
        ulong rooks = _bitboards[isWhite ? (int)Piece.WhiteRook : (int)Piece.BlackRook];
        ulong queens = _bitboards[isWhite ? (int)Piece.WhiteQueen : (int)Piece.BlackQueen];
        ulong king = _bitboards[isWhite ? (int)Piece.WhiteKing : (int)Piece.BlackKing];

        return new Material(pawns, knights, bishops, rooks, queens, king);
    }
    public Move[] GetCurrentLegalMoves()
    {
        return _currentLegalMoves.Moves;
    }

    public bool GetTurn()
    {
        return _isWhiteTurn;
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
            EnPassantMask = (ushort)(1 << file);
        }
        else
        {
            EnPassantMask = 0; //if not double push reset
        }

        #endregion

        UpdateTotalBitboards();

        _isWhiteTurn = !_isWhiteTurn;

        //have to generate legal moves after turn switch immediately
        GenerateLegalMoves(_currentLegalMoves);
        EndingState state = CheckGameState();
        if (state != EndingState.Playing)
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
    public ulong GetBitboard(Piece piece)
    {
        return _bitboards[(int)piece];
    }
    public ulong GetAllPieces()
    {
        return _allPiecesBB;
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
        moves.Clear();
        CustomMovesList pseudoMoves = new CustomMovesList();

        //get correct bitboards based on turn


        Material mat = GetMaterialForColour(_isWhiteTurn);
        MoveGenerator.GeneratePseudoLegalMoves(
            mat,
            _allPiecesBB,
            _isWhiteTurn ? _whitePiecesBB : _blackPiecesBB,
            _isWhiteTurn ? _blackPiecesBB : _whitePiecesBB,
            _isWhiteTurn,
            EnPassantMask,
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
    public bool IsSquareAttacked(int square, bool byWhite)
    {
        //knights
        ulong knightAttacks = MoveGenerator.KnightLookup[square];
        ulong enemyKnights = _bitboards[byWhite ? (int)Piece.WhiteKnight : (int)Piece.BlackKnight];
        if ((knightAttacks & enemyKnights) != 0) return true;

        //king
        ulong kingAttacks = MoveGenerator.KingLookup[square];
        ulong enemyKing = _bitboards[byWhite ? (int)Piece.WhiteKing : (int)Piece.BlackKing];
        if ((kingAttacks & enemyKing) != 0) return true;

        //pawns
        ulong pawnAttacks = byWhite ? MoveGenerator.BlackPawnLookup[square] : MoveGenerator.WhitePawnLookup[square];
        ulong enemyPawns = _bitboards[byWhite ? (int)Piece.WhitePawn : (int)Piece.BlackPawn];
        if ((pawnAttacks & enemyPawns) != 0) return true;

        //bishops/queens
        ulong bishopAttacks = MoveGenerator.GetSliderAttacks(Piece.WhiteBishop, square, _allPiecesBB);
        ulong enemyBishops = _bitboards[byWhite ? (int)Piece.WhiteBishop : (int)Piece.BlackBishop];
        ulong enemyQueens = _bitboards[byWhite ? (int)Piece.WhiteQueen : (int)Piece.BlackQueen];

        if ((bishopAttacks & (enemyBishops | enemyQueens)) != 0) return true;

        //rook/queens
        ulong rookAttacks = MoveGenerator.GetSliderAttacks(Piece.WhiteRook, square, _allPiecesBB);
        ulong enemyRooks = _bitboards[byWhite ? (int)Piece.WhiteRook : (int)Piece.BlackRook];

        if ((rookAttacks & (enemyRooks | enemyQueens)) != 0) return true;

        return false;
    }
    private bool MakeMoveAndCheckLegality(Move move)
    {
        int kingSquare = GetKingSquare(_isWhiteTurn);
        Piece movingPiece = _boardSquares[move.StartingPos];

        //validate castling move
        if (move.Flag == MoveFlag.CastleKingSide || move.Flag == MoveFlag.CastleQueenSide)
        {
            //cant castle out of check
            if (IsSquareAttacked(kingSquare, !_isWhiteTurn)) return false;

            //cant castle through check
            int crossingSquare = -1;
            if (move.Flag == MoveFlag.CastleKingSide)
                crossingSquare = _isWhiteTurn ? 5 : 61;
            else
                crossingSquare = _isWhiteTurn ? 3 : 59;

            if (IsSquareAttacked(crossingSquare, !_isWhiteTurn)) return false;
        }

        //save 
        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;
        Piece capturedPiece = _boardSquares[move.EndingPos];

        ulong savedWhiteBB = _whitePiecesBB;
        ulong savedBlackBB = _blackPiecesBB;
        ulong savedAllBB = _allPiecesBB;
        ulong savedMovingPieceBB = _bitboards[(int)movingPiece];
        ulong savedCapturedPieceBB = (capturedPiece != Piece.None) ? _bitboards[(int)capturedPiece] : 0;

        //en passant capture handling
        Piece epCapturedPawn = Piece.None;
        int epCaptureSquare = -1;
        ulong savedEpPawnBB = 0;

        //captures
        if (move.Flag == MoveFlag.EnPassantCapture)
        {
            epCaptureSquare = _isWhiteTurn ? (move.EndingPos - 8) : (move.EndingPos + 8);
            epCapturedPawn = _boardSquares[epCaptureSquare];
            savedEpPawnBB = _bitboards[(int)epCapturedPawn];

            //remove en passant pawn
            _bitboards[(int)epCapturedPawn] &= ~(1UL << epCaptureSquare);
            _boardSquares[epCaptureSquare] = Piece.None;
        }
        else if (capturedPiece != Piece.None)
        {
            //remove captured piece
            _bitboards[(int)capturedPiece] &= ~toBit;
        }

        //handle castling
        if (move.Flag == MoveFlag.CastleKingSide)
        {
            if (_isWhiteTurn) MoveRook(7, 5); else MoveRook(63, 61);
        }
        else if (move.Flag == MoveFlag.CastleQueenSide)
        {
            if (_isWhiteTurn) MoveRook(0, 3); else MoveRook(56, 59);
        }

        //move piece
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);
        _boardSquares[move.EndingPos] = movingPiece;
        _boardSquares[move.StartingPos] = Piece.None;

        UpdateTotalBitboards();

        //if king moved need to check new king position
        int squareToCheck = (movingPiece == Piece.WhiteKing || movingPiece == Piece.BlackKing) ? move.EndingPos : kingSquare;
        bool isKingAttacked = IsSquareAttacked(squareToCheck, !_isWhiteTurn);

        //restore moving piece
        _bitboards[(int)movingPiece] = savedMovingPieceBB;
        _boardSquares[move.StartingPos] = movingPiece;

        //restoring captured piece
        if (move.Flag == MoveFlag.EnPassantCapture)
        {
            //restore en passant capture
            _bitboards[(int)epCapturedPawn] = savedEpPawnBB;
            _boardSquares[epCaptureSquare] = epCapturedPawn;
            _boardSquares[move.EndingPos] = Piece.None;
        }
        else
        {
            //restore capture
            if (capturedPiece != Piece.None)
            {
                _bitboards[(int)capturedPiece] = savedCapturedPieceBB;
                _boardSquares[move.EndingPos] = capturedPiece;
            }
            else
            {
                _boardSquares[move.EndingPos] = Piece.None;
            }
        }

        //undo rook castling move
        if (move.Flag == MoveFlag.CastleKingSide)
        {
            if (_isWhiteTurn) MoveRook(5, 7); else MoveRook(61, 63);
        }
        else if (move.Flag == MoveFlag.CastleQueenSide)
        {
            if (_isWhiteTurn) MoveRook(3, 0); else MoveRook(59, 56);
        }

        //restore bitboards
        _whitePiecesBB = savedWhiteBB;
        _blackPiecesBB = savedBlackBB;
        _allPiecesBB = savedAllBB;

        return !isKingAttacked;
    }
    public EndingState CheckGameState()
    {
        if (_currentLegalMoves.Length > 0)
        {
            return EndingState.Playing;
        }

        int kingSquare = GetKingSquare(_isWhiteTurn);

        bool isInCheck = IsSquareAttacked(kingSquare, !_isWhiteTurn);

        if (isInCheck)
        {
            return EndingState.Checkmate;
        }
        else
        {
            return EndingState.Stalemate;
        }   
    }
    
    #region search methods
    public bool IsInCheck()
    {
        int kingSquare = GetKingSquare(_isWhiteTurn);
        return IsSquareAttacked(kingSquare, !_isWhiteTurn);
    }
    public void MakeMove(Move move)
    {
        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;
        Piece movingPiece = _boardSquares[move.StartingPos];
        Piece capturedPiece = _boardSquares[move.EndingPos];

        if (move.Flag == MoveFlag.EnPassantCapture)
        {
            int capturePos = _isWhiteTurn ? (move.EndingPos - 8) : (move.EndingPos + 8);
            capturedPiece = _boardSquares[capturePos]; //what is actually captured
            Piece pawn = _isWhiteTurn ? Piece.BlackPawn : Piece.WhitePawn;

            _bitboards[(int)pawn] &= ~(1UL << capturePos);
            _boardSquares[capturePos] = Piece.None;
        }
        else if (capturedPiece != Piece.None)
        {
            Debug.Log(capturedPiece);
            _bitboards[(int)capturedPiece] &= ~toBit;
        }


        //save history
        _gameStateHistory.Push(new GameState(capturedPiece, CastlingRights, EnPassantMask));

        CheckCastlingRights(movingPiece, move.StartingPos);

        //handle castling rights if rook is captured
        if (capturedPiece == Piece.WhiteRook)
        {
            if (move.EndingPos == 0) CastlingRights &= ~Castling.WhiteQueen; //Capture a1
            if (move.EndingPos == 7) CastlingRights &= ~Castling.WhiteKing;  //Capture h1
        }
        else if (capturedPiece == Piece.BlackRook)
        {
            if (move.EndingPos == 56) CastlingRights &= ~Castling.BlackQueen; //Capture a8
            if (move.EndingPos == 63) CastlingRights &= ~Castling.BlackKing;  //Capture h8
        }

        _bitboards[(int)movingPiece] &= ~fromBit;
        _boardSquares[move.StartingPos] = Piece.None;

        //handle promotion
        Piece pieceOnTarget = movingPiece;
        if (move.IsPromotion)
        {
            pieceOnTarget = move.PromotionPieceType;
            if (!_isWhiteTurn) pieceOnTarget = (Piece)((int)pieceOnTarget + 6); //black offset
        }
        else
        {
            //remove from old position only if not promotion
            _bitboards[(int)movingPiece] ^= toBit;
        }

        _bitboards[(int)pieceOnTarget] |= toBit;
        _boardSquares[move.EndingPos] = pieceOnTarget;

        //handle castling move
        if (move.Flag == MoveFlag.CastleKingSide)
        {
            if (_isWhiteTurn) MoveRook(7, 5); else MoveRook(63, 61);
        }
        else if (move.Flag == MoveFlag.CastleQueenSide)
        {
            if (_isWhiteTurn) MoveRook(0, 3); else MoveRook(56, 59);
        }

        //handle en passant mask
        if (move.Flag == MoveFlag.PawnDoublePush)
        {
            int file = move.EndingPos % 8;
            EnPassantMask = (ushort)(1 << file);
        }
        else
        {
            EnPassantMask = 0;
        }

        UpdateTotalBitboards();
        _isWhiteTurn = !_isWhiteTurn;
    }

    public void UndoMove(Move move)
    {
        _isWhiteTurn = !_isWhiteTurn;

        //get old state
        GameState oldState = _gameStateHistory.Pop();
        CastlingRights = oldState.CastlingRights;
        EnPassantMask = oldState.EnPassantMask;

        ulong fromBit = 1UL << move.StartingPos;
        ulong toBit = 1UL << move.EndingPos;

        Piece movedPiece = _boardSquares[move.EndingPos];

        if (move.IsPromotion)
        {
            _bitboards[(int)movedPiece] &= ~toBit; //remove promoted piece
            movedPiece = _isWhiteTurn ? Piece.WhitePawn : Piece.BlackPawn; //was a pawn originally
        }
        else
        {
            Debug.Log("moved piece = " + movedPiece);
            //for non promotion moves remove from destination square
            _bitboards[(int)movedPiece] &= ~toBit;
        }

        if (move.Flag == MoveFlag.CastleKingSide)
        {
            if (_isWhiteTurn) MoveRook(5, 7); else MoveRook(61, 63);
        }
        else if (move.Flag == MoveFlag.CastleQueenSide)
        {
            if (_isWhiteTurn) MoveRook(3, 0); else MoveRook(59, 56);
        }

        //piece back on original square
        _bitboards[(int)movedPiece] |= fromBit;
        _boardSquares[move.StartingPos] = movedPiece;
        _boardSquares[move.EndingPos] = Piece.None;

        //put back captured piece
        if (oldState.CapturedPiece != Piece.None)
        {
            int captureSq = move.EndingPos;

            if (move.Flag == MoveFlag.EnPassantCapture)
            {
                captureSq = _isWhiteTurn ? (move.EndingPos - 8) : (move.EndingPos + 8);
            }

            _bitboards[(int)oldState.CapturedPiece] |= (1UL << captureSq);
            _boardSquares[captureSq] = oldState.CapturedPiece;
        }
        UpdateTotalBitboards();
    }
    #endregion
}
