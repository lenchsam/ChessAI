using System.Collections.Generic;
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
