using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
        }
        UpdateTotalBitboards();
    }

    public Piece GetPieceOnSquare(int squareIndex)
    {
        return _boardSquares[squareIndex];
    }

    public bool MovePiece(Vector2Int from, Vector2Int to)
    {
        if (from == to) return false;

        //get square indexes
        int fromIndex = from.y * 8 + from.x;
        int toIndex = to.y * 8 + to.x;

        ulong fromBit = 1UL << fromIndex;
        ulong toBit = 1UL << toIndex;

        Piece movingPiece = _boardSquares[fromIndex];
        Piece targetPiece = _boardSquares[toIndex];

        //validate move based on piece type
        bool isValid = false;

        if (movingPiece == Piece.WhiteKnight || movingPiece == Piece.BlackKnight)
        {
            isValid = ValidateKnightMove(fromIndex, toIndex, movingPiece);
        }
        else if (movingPiece == Piece.BlackKing || movingPiece == Piece.WhiteKing)
        {
            isValid = ValidateKingMove(fromIndex, toIndex, movingPiece);
        }
        else if (movingPiece == Piece.WhitePawn || movingPiece == Piece.BlackPawn)
        {
            isValid = ValidatePawnMove(fromIndex, toIndex, movingPiece);
        }
        else if (movingPiece == Piece.WhiteRook || movingPiece == Piece.BlackRook)
        {
            isValid = ValidateRookMove(fromIndex, toIndex, movingPiece);
        }else if (movingPiece == Piece.WhiteBishop || movingPiece == Piece.BlackBishop)
        {
            isValid = ValidateBishopMove(fromIndex, toIndex, movingPiece);
        }
        else
        {
            //other pieces not implemented yet
            return false;
        }

        //if it didnt get validated
        if (!isValid) return false;


        //do move

        //capture
        if (targetPiece != Piece.None)
        {
            _bitboards[(int)targetPiece] &= ~toBit;
        }

        //update moving piece bitboard
        _bitboards[(int)movingPiece] ^= (fromBit | toBit);

        _boardSquares[toIndex] = movingPiece;
        _boardSquares[fromIndex] = Piece.None;;

        UpdateTotalBitboards();

        return true;
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

        //-----------------------------------------------------------------------Knights

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            ulong startBit = 1UL << squareIndex;
            ulong possibleMoves = 0UL;

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

            //-----------------------------------------------------------------------King

            possibleMoves = 0UL;

            //up down
            possibleMoves |= (startBit << 8); // North
            possibleMoves |= (startBit >> 8); // South

            //horizontal
            possibleMoves |= (startBit << 1) & ~FILE_A;
            possibleMoves |= (startBit >> 1) & ~FILE_H;

            //diagonal
            possibleMoves |= (startBit << 9) & ~FILE_A; //NE
            possibleMoves |= (startBit << 7) & ~FILE_H; //NW
            possibleMoves |= (startBit >> 7) & ~FILE_A; //SE
            possibleMoves |= (startBit >> 9) & ~FILE_H; //SW

            _kingLookup[squareIndex] = possibleMoves;

            //-----------------------------------------------------------------------Pawns

            ///white pawns
            ulong whiteAttacks = 0UL;
            whiteAttacks |= (startBit << 9) & ~FILE_A; //NE
            whiteAttacks |= (startBit << 7) & ~FILE_H; //NW

            _whitePawnLookup[squareIndex] = whiteAttacks;

            //black pawns
            ulong blackAttacks = 0UL;
            blackAttacks |= (startBit >> 7) & ~FILE_A; //SE
            blackAttacks |= (startBit >> 9) & ~FILE_H; //SW

            _blackPawnLookup[squareIndex] = blackAttacks;
        }
    }

    // MAGIC BITBOARDS
    // code based on tord romstads implementation
    private ulong MaskRookAttacks(int square)
    {
        ulong attacks = 0UL;

        int rank = square / 8;
        int file = square % 8;

        //generate attacks
        //up
        for (int r = rank + 1; r <= 6; r++)
            attacks |= 1UL << (r * 8 + file);

        //down
        for (int r = rank - 1; r >= 1; r--)
            attacks |= 1UL << (r * 8 + file);

        //left
        for (int f = file - 1; f >= 1; f--)
            attacks |= 1UL << (rank * 8 + f);

        //right
        for (int f = file + 1; f <= 6; f++)
            attacks |= 1UL << (rank * 8 + f);

        return attacks;
    }
    private ulong MaskBishopAttacks(int square)
    {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;
        //generate attacks
        //northeast
        for (int r = rank + 1, f = file + 1; r <= 6 && f <= 6; r++, f++)
            attacks |= 1UL << (r * 8 + f);

        //northwest
        for (int r = rank + 1, f = file - 1; r <= 6 && f >= 1; r++, f--)
            attacks |= 1UL << (r * 8 + f);

        //southeast
        for (int r = rank - 1, f = file + 1; r >= 1 && f <= 6; r--, f++)
            attacks |= 1UL << (r * 8 + f);

        //southwest
        for (int r = rank - 1, f = file - 1; r >= 1 && f >= 1; r--, f--)
            attacks |= 1UL << (r * 8 + f);
        return attacks;
    }




    //function from sebastian lague
    //https://youtu.be/_vqlIPDR2TU?t=1905
    private ulong[] CreateAllBlockerBitboards(ulong mask)
    {
        List<int> moveSquareIndices = new();
        for (int i = 0; i < 64; i++)
        {
            if (((mask >> i) & 1UL) == 1UL)
            {
                moveSquareIndices.Add(i);
            }
        }

        int numPatterns = 1 << moveSquareIndices.Count;
        ulong[] blockerBitboards = new ulong[numPatterns];

        for (int patternIndex = 0; patternIndex < numPatterns; patternIndex++)
        {
            for(int bitIndex = 0; bitIndex < moveSquareIndices.Count; bitIndex++) 
            {
                int bit = (patternIndex >> bitIndex) & 1;
                blockerBitboards[patternIndex] |= (ulong)bit << moveSquareIndices[bitIndex];

            }
        }

        return blockerBitboards;
    }




    //returns biboard of possible rook attacks
    private ulong CalculateRookAttacks(int square, ulong blockers)
    {
        ulong attacks = 0;

        int r = square / 8;
        int f = square % 8;

        //north
        for (int r2 = r + 1; r2 < 8; r2++)
        {
            ulong bit = 1UL << (r2 * 8 + f);
            attacks |= bit;
            // If we hit a blocker cant go further.
            if ((blockers & bit) != 0) break;
        }

        //east
        for (int f2 = f + 1; f2 < 8; f2++)
        {
            ulong bit = 1UL << (r * 8 + f2);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        //south
        for (int r2 = r - 1; r2 >= 0; r2--)
        {
            ulong bit = 1UL << (r2 * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        //west
        for (int f2 = f - 1; f2 >= 0; f2--)
        {
            ulong bit = 1UL << (r * 8 + f2);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }

    private ulong CalculateBishopAttacks(int square, ulong blockers)
    {
        ulong attacks = 0;

        int r = square / 8;
        int f = square % 8;

        // north-east
        for (int r2 = r + 1, f2 = f + 1; r2 < 8 && f2 < 8; r2++, f2++)
        {
            ulong bit = 1UL << (r2 * 8 + f2);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // north-west
        for (int r2 = r + 1, f2 = f - 1; r2 < 8 && f2 >= 0; r2++, f2--)
        {
            ulong bit = 1UL << (r2 * 8 + f2);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // south-east
        for (int r2 = r - 1, f2 = f + 1; r2 >= 0 && f2 < 8; r2--, f2++)
        {
            ulong bit = 1UL << (r2 * 8 + f2);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // south-west
        for (int r2 = r - 1, f2 = f - 1; r2 >= 0 && f2 >= 0; r2--, f2--)
        {
            ulong bit = 1UL << (r2 * 8 + f2);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }



    public void DebugBitboard(ulong bb, string label = "")
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(label))
            sb.AppendLine(label);

        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                sb.Append(((bb >> square) & 1UL) == 1UL ? "1 " : ". ");
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }





    private bool ValidateKnightMove(int startSquare, int targetSquare, Piece movingPiece)
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
    private bool ValidateKingMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;

        //is the target valid, if no return false
        if ((_kingLookup[startSquare] & targetMask) == 0UL)
        {
            return false;
        }

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
    private bool ValidatePawnMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;
        bool isWhite = (int)movingPiece == (int)Piece.WhitePawn;


        bool isTargetOccupied = false;
        if ((_allPiecesBB & targetMask) != 0UL) isTargetOccupied = true;

        //captures
        if (isWhite)
        {
            //check if target is in attack lookup
            if ((_whitePawnLookup[startSquare] & targetMask) != 0UL)
            {
                //if enemy piece is there
                if ((_blackPiecesBB & targetMask) != 0UL) return true;
                return false;
            }
        }
        else
        {
            if ((_blackPawnLookup[startSquare] & targetMask) != 0UL)
            {
                if ((_whitePiecesBB & targetMask) != 0UL) return true;
                return false;
            }
        }

        //push pawns, this is not in lookup tables
        if (isTargetOccupied) return false;

        if (isWhite)
        {
            if (targetSquare == startSquare + 8) return true;

            //double push only on home rank
            if (targetSquare == startSquare + 16)
            {
                if (startSquare >= 8 && startSquare <= 15)
                {
                    ulong skipMask = 1UL << (startSquare + 8);
                    if ((_allPiecesBB & skipMask) == 0UL) return true;
                }
            }
        }
        else
        {
            if (targetSquare == startSquare - 8) return true;

            //double push only on home rank
            if (targetSquare == startSquare - 16)
            {
                if (startSquare >= 48 && startSquare <= 55)
                {
                    ulong skipMask = 1UL << (startSquare - 8);
                    if ((_allPiecesBB & skipMask) == 0UL) return true;
                }
            }
        }

        return false;
    }
    private bool ValidateRookMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;

        ulong possibleAttacks = CalculateRookAttacks(startSquare, _allPiecesBB);

        if ((possibleAttacks & targetMask) == 0UL)
            return false;

        bool isWhite = (int)movingPiece <= (int)Piece.WhiteKing;
        ulong friendlyPieces = isWhite ? _whitePiecesBB : _blackPiecesBB;

        if ((friendlyPieces & targetMask) != 0UL)
            return false;

        return true;
    }
    private bool ValidateBishopMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;

        ulong possibleAttacks = CalculateBishopAttacks(startSquare, _allPiecesBB);

        if ((possibleAttacks & targetMask) == 0UL)
            return false;

        bool isWhite = (int)movingPiece <= (int)Piece.WhiteKing;
        ulong friendlyPieces = isWhite ? _whitePiecesBB : _blackPiecesBB;

        if ((friendlyPieces & targetMask) != 0UL)
            return false;

        return true;
    }
}

