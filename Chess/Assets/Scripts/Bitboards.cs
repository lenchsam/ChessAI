using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public class Bitboards
{
    // ulong is a 64-bit unsigned integer
    // two hex digits (8 bits/1 byte) represents each row
    private ulong[] _bitboards = new ulong[12];
    private ulong _whitePiecesBB, _blackPiecesBB, _allPiecesBB;

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

    //lookup tables of none sliding pieces
    private ulong[] _knightLookup = new ulong[64];
    private ulong[] _whitePawnLookup = new ulong[64];
    private ulong[] _blackPawnLookup = new ulong[64];
    private ulong[] _kingLookup = new ulong[64];

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
            UpdateTotalBitboards();
        }
    }

    public Piece GetPieceOnSquare(int squareIndex)
    {
        return _boardSquares[squareIndex];
    }

    public bool MovePiece(Vector2Int from, Vector2Int to)
    {
        if(from == to) return false;

        //get square indexes
        int fromIndex = from.y * 8 + from.x;
        int toIndex = to.y * 8 + to.x;

        //bitmaskk
        ulong fromBit = 1UL << fromIndex;
        ulong toBit = 1UL << toIndex;

        //combined mask for moving
        ulong moveMask = fromBit | toBit;

        Piece movingPiece = _boardSquares[fromIndex];

        //captures
        Piece targetPiece = _boardSquares[toIndex];

        if (targetPiece != Piece.None)
        {
            //removes catured piece from bitboard
            //~ = not operator
            //e.g. 1111 & ~0010 -> 1111 & 1101 -> 1101 (Bit 2 is cleared)
            _bitboards[(int)targetPiece] &= ~toBit;
        }

        if (movingPiece == Piece.WhiteKnight || movingPiece == Piece.BlackKnight)
        {
            bool isValid = ValidateKnightMove(fromIndex, toIndex, movingPiece);

            if (!isValid)
            {
                Debug.Log("Invalid Knight Move");
                return false;
            }

            //capture
            if (targetPiece != Piece.None)
            {
                _bitboards[(int)targetPiece] &= ~toBit;
            }

            _bitboards[(int)movingPiece] ^= (fromBit | toBit);

            _boardSquares[toIndex] = movingPiece;
            _boardSquares[fromIndex] = Piece.None;

            UpdateTotalBitboards();
            return true;
        }
        return false;
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

    public void GenerateLookupTables()
    {
        ulong FILE_A = 0x0101010101010101;
        ulong FILE_H = 0x8080808080808080;

        ulong FILE_AB = FILE_A | (FILE_A << 1);
        ulong FILE_GH = FILE_H | (FILE_H >> 1);


        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            ulong startBit = 1UL << squareIndex;
            ulong possibleMoves = 0x0000000000000000;

            //-----------------------------------------------------------------------Knights

            //directions of attack e.g. NoWe = north west (doesnt include south or north initial as it just shifts the other direction for this.
            int WoWe = 6;
            int NoWe = 15;
            int NoEa = 17;
            int EaEa = 10;

            //bitshifting to get all of the possible moves
            possibleMoves = (startBit << NoEa & ~FILE_A)  |
                            (startBit << NoWe & ~FILE_H)  |
                            (startBit << EaEa & ~FILE_AB) |
                            (startBit << WoWe & ~FILE_GH) |
                            (startBit >> NoEa & ~FILE_H)  |
                            (startBit >> NoWe & ~FILE_A)  |
                            (startBit >> EaEa & ~FILE_GH) |
                            (startBit >> WoWe & ~FILE_AB);

            _knightLookup[squareIndex] = possibleMoves;

            possibleMoves = 0x0000000000000000;
            //-----------------------------------------------------------------------Pawns

            //-----------------------------------------------------------------------King

        }
    }

    public bool ValidateKnightMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;

        //is the target valid, if no return false
        if ((_knightLookup[startSquare] & targetMask) == 0UL)
            return false;

        //is it occupied by a friendly piece
        bool isWhite = (int)movingPiece <= (int)Piece.WhiteKing;

        if (isWhite)
        {
            if ((_whitePiecesBB & targetMask) != 0UL)
                return false;
        }
        else
        {
            if ((_blackPiecesBB & targetMask) != 0UL)
                return false;
        }

        //the move is legal if both previous checks pass
        return true;
    }
}

