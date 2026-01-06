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

    public Move(int from, int to, bool isCapture)
    {
        StartingPos = from;
        EndingPos = to;
        IsCapture = isCapture;
    }
}
public class Bitboards
{
    public UnityEvent<ulong> MovingPieceEvent = new UnityEvent<ulong>();

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

    public void FENtoBitboards(string FEN)
    {
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
    }

    public Piece GetPieceOnSquare(int squareIndex)
    {
        return _boardSquares[squareIndex];
    }

    public bool MovePiece(int from, int to)
    {
        if (from == to) return false;

        List<Move> moveList = new List<Move>();

        //correct bitboard based on colour
        ulong pawns, knights, bishops, rooks, queens, king;

        if (_isWhiteTurn)
        {
            pawns = _bitboards[(int)Piece.WhitePawn];
            knights = _bitboards[(int)Piece.WhiteKnight];
            bishops = _bitboards[(int)Piece.WhiteBishop];
            rooks = _bitboards[(int)Piece.WhiteRook];
            queens = _bitboards[(int)Piece.WhiteQueen];
            king = _bitboards[(int)Piece.WhiteKing];
        }
        else
        {
            pawns = _bitboards[(int)Piece.BlackPawn];
            knights = _bitboards[(int)Piece.BlackKnight];
            bishops = _bitboards[(int)Piece.BlackBishop];
            rooks = _bitboards[(int)Piece.BlackRook];
            queens = _bitboards[(int)Piece.BlackQueen];
            king = _bitboards[(int)Piece.BlackKing];
        }

        MoveGenerator.GeneratePseudoLegalMoves(
            pawns, knights, bishops, rooks, queens, king,
            _allPiecesBB,
            _isWhiteTurn ? _whitePiecesBB : _blackPiecesBB, //own pieces
            _isWhiteTurn ? _blackPiecesBB : _whitePiecesBB, //enemy pieces
            _isWhiteTurn,
            moveList
        );

        Move validMove = default;
        bool isvalid = false;

        //check if move is in valid moves list
        foreach (Move move in moveList)
        {
            if (move.StartingPos == from && move.EndingPos == to)
            {
                validMove = move;
                isvalid = true;
                break;
            }
        }

        //if it didnt get validated
        if (!isvalid) return false;

        //if it got here, the move is valid so execute the move

        ulong fromBit = 1UL << from;
        ulong toBit = 1UL << to;

        Piece movingPiece = _boardSquares[from];
        Piece targetPiece = _boardSquares[to];

        //capture
        if (validMove.IsCapture)
        {
            //TODO: Handle en passant captures here
            if (targetPiece != Piece.None)
            {
                _bitboards[(int)targetPiece] &= ~toBit;
            }
        }

        //update moving piece bitboard
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);

        _boardSquares[to] = movingPiece;
        _boardSquares[from] = Piece.None;

        UpdateTotalBitboards();

        _isWhiteTurn = !_isWhiteTurn; //toggle turn

        return true;
    }

    public void InvokeEvent(int from)
    {
        MovingPieceEvent.Invoke(GetLookupFromSquare(from));
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

    private ulong GetLookupFromSquare(int square)
    {
        //TODO: Change to generated moves so only legal moves show
        //that will also fix that pawns only shows the diagonals that they can attack
        Piece piece = GetPieceOnSquare(square);

        switch (piece)
        {
            case Piece.WhitePawn:
                return MoveGenerator.WhitePawnLookup[square];
            case Piece.BlackPawn:
                return MoveGenerator.BlackPawnLookup[square];
            case Piece.WhiteKnight:
            case Piece.BlackKnight:
                return MoveGenerator.KnightLookup[square];
            case Piece.WhiteKing:
            case Piece.BlackKing:
                return MoveGenerator.KingLookup[square];
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
}
