using System.Collections.Generic;
using Unity.VisualScripting;
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

    //pre computed data for magic bitboards
    private static readonly int[] _rookShifts = new int[64]
    {
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12
    };
    private static readonly int[] _bishopShifts = new int[64]
    {
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
    };
    private static readonly ulong[] _rookMagics = new ulong[64]
    {
        0x8a80104000800020UL,
        0x140002000100040UL,
        0x2801880a0017001UL,
        0x100081001000420UL,
        0x200020010080420UL,
        0x3001c0002010008UL,
        0x8480008002000100UL,
        0x2080088004402900UL,
        0x800098204000UL,
        0x2024401000200040UL,
        0x100802000801000UL,
        0x120800800801000UL,
        0x208808088000400UL,
        0x2802200800400UL,
        0x2200800100020080UL,
        0x801000060821100UL,
        0x80044006422000UL,
        0x100808020004000UL,
        0x12108a0010204200UL,
        0x140848010000802UL,
        0x481828014002800UL,
        0x8094004002004100UL,
        0x4010040010010802UL,
        0x20008806104UL,
        0x100400080208000UL,
        0x2040002120081000UL,
        0x21200680100081UL,
        0x20100080080080UL,
        0x2000a00200410UL,
        0x20080800400UL,
        0x80088400100102UL,
        0x80004600042881UL,
        0x4040008040800020UL,
        0x440003000200801UL,
        0x4200011004500UL,
        0x188020010100100UL,
        0x14800401802800UL,
        0x2080040080800200UL,
        0x124080204001001UL,
        0x200046502000484UL,
        0x480400080088020UL,
        0x1000422010034000UL,
        0x30200100110040UL,
        0x100021010009UL,
        0x2002080100110004UL,
        0x202008004008002UL,
        0x20020004010100UL,
        0x2048440040820001UL,
        0x101002200408200UL,
        0x40802000401080UL,
        0x4008142004410100UL,
        0x2060820c0120200UL,
        0x1001004080100UL,
        0x20c020080040080UL,
        0x2935610830022400UL,
        0x44440041009200UL,
        0x280001040802101UL,
        0x2100190040002085UL,
        0x80c0084100102001UL,
        0x4024081001000421UL,
        0x20030a0244872UL,
        0x12001008414402UL,
        0x2006104900a0804UL,
        0x1004081002402UL
    };
    private static readonly ulong[] _bishopMagics = new ulong[64]
    {
        0x40040844404084UL,
        0x2004208a004208UL,
        0x10190041080202UL,
        0x108060845042010UL,
        0x581104180800210UL,
        0x2112080446200010UL,
        0x1080820820060210UL,
        0x3c0808410220200UL,
        0x4050404440404UL,
        0x21001420088UL,
        0x24d0080801082102UL,
        0x1020a0a020400UL,
        0x40308200402UL,
        0x4011002100800UL,
        0x401484104104005UL,
        0x801010402020200UL,
        0x400210c3880100UL,
        0x404022024108200UL,
        0x810018200204102UL,
        0x4002801a02003UL,
        0x85040820080400UL,
        0x810102c808880400UL,
        0xe900410884800UL,
        0x8002020480840102UL,
        0x220200865090201UL,
        0x2010100a02021202UL,
        0x152048408022401UL,
        0x20080002081110UL,
        0x4001001021004000UL,
        0x800040400a011002UL,
        0xe4004081011002UL,
        0x1c004001012080UL,
        0x8004200962a00220UL,
        0x8422100208500202UL,
        0x2000402200300c08UL,
        0x8646020080080080UL,
        0x80020a0200100808UL,
        0x2010004880111000UL,
        0x623000a080011400UL,
        0x42008c0340209202UL,
        0x209188240001000UL,
        0x400408a884001800UL,
        0x110400a6080400UL,
        0x1840060a44020800UL,
        0x90080104000041UL,
        0x201011000808101UL,
        0x1a2208080504f080UL,
        0x8012020600211212UL,
        0x500861011240000UL,
        0x180806108200800UL,
        0x4000020e01040044UL,
        0x300000261044000aUL,
        0x802241102020002UL,
        0x20906061210001UL,
        0x5a84841004010310UL,
        0x4010801011c04UL,
        0xa010109502200UL,
        0x4a02012000UL,
        0x500201010098b028UL,
        0x8040002811040900UL,
        0x28000010020204UL,
        0x6000020202d0240UL,
        0x8918844842082200UL,
        0x4010011029020020UL
    };

    private ulong[][] _rookAttackTable = new ulong[64][];
    private ulong[][] _bishopAttackTable = new ulong[64][];
    private ulong[] _rookMasks = new ulong[64];
    private ulong[] _bishopMasks = new ulong[64];

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
        else if (movingPiece == Piece.WhiteQueen || movingPiece == Piece.BlackQueen)
        {
            isValid = ValidateQueenMove(fromIndex, toIndex, movingPiece);
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

    public void InitialiseRookAndBishopMagics()
    {
        //allocate attack tables
        _rookAttackTable = new ulong[64][];
        _bishopAttackTable = new ulong[64][];

        for (int square = 0; square < 64; square++)
        {
            //rook
            _rookMasks[square] = MaskRookAttacks(square);
            int rookRelevantBits = CountBits(_rookMasks[square]);
            _rookShifts[square] = 64 - rookRelevantBits;

            ulong[] rookBlockers = CreateAllBlockerBitboards(_rookMasks[square]);
            _rookAttackTable[square] = new ulong[rookBlockers.Length];

            for (int i = 0; i < rookBlockers.Length; i++)
            {
                ulong occupancy = rookBlockers[i];
                int index = (int)((occupancy * _rookMagics[square]) >> _rookShifts[square]);
                _rookAttackTable[square][index] = CalculateRookAttacks(square, occupancy);
            }

            //bishop
            _bishopMasks[square] = MaskBishopAttacks(square);
            int bishopRelevantBits = CountBits(_bishopMasks[square]);
            _bishopShifts[square] = 64 - bishopRelevantBits;

            ulong[] bishopBlockers = CreateAllBlockerBitboards(_bishopMasks[square]);
            _bishopAttackTable[square] = new ulong[bishopBlockers.Length];

            for (int i = 0; i < bishopBlockers.Length; i++)
            {
                ulong occupancy = bishopBlockers[i];
                int index = (int)((occupancy * _bishopMagics[square]) >> _bishopShifts[square]);
                _bishopAttackTable[square][index] = CalculateBishopAttacks(square, occupancy);
            }
        }
    }

    //Brian Kernighan’s algorithm
    private int CountBits(ulong bb)
    {
        int count = 0;
        while (bb != 0)
        {
            bb &= bb - 1; //clear the least significant bit set
            count++;
        }
        return count;
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

        ulong occupancy = _allPiecesBB & _rookMasks[startSquare];
        int index = (int)((occupancy * _rookMagics[startSquare]) >> _rookShifts[startSquare]);

        //lookup
        ulong possibleAttacks = _rookAttackTable[startSquare][index];

        if ((possibleAttacks & targetMask) == 0UL) return false;

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

        return true;
    }
    private bool ValidateBishopMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;

        ulong occupancy = _allPiecesBB & _bishopMasks[startSquare];
        int index = (int)((occupancy * _bishopMagics[startSquare]) >> _bishopShifts[startSquare]);

        //lookup
        ulong possibleAttacks = _bishopAttackTable[startSquare][index];

        if ((possibleAttacks & targetMask) == 0UL) return false;

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

        return true;
    }
    private bool ValidateQueenMove(int startSquare, int targetSquare, Piece movingPiece)
    {
        ulong targetMask = 1UL << targetSquare;

        //rook attacks
        ulong rookOccupancy = _allPiecesBB & _rookMasks[startSquare];
        int rookIndex = (int)((rookOccupancy * _rookMagics[startSquare]) >> _rookShifts[startSquare]);
        ulong rookAttacks = _rookAttackTable[startSquare][rookIndex];

        //bishop attacks
        ulong bishopOccupancy = _allPiecesBB & _bishopMasks[startSquare];
        int bishopIndex = (int)((bishopOccupancy * _bishopMagics[startSquare]) >> _bishopShifts[startSquare]);
        ulong bishopAttacks = _bishopAttackTable[startSquare][bishopIndex];

        //combine
        ulong queenAttacks = rookAttacks | bishopAttacks;

        if ((queenAttacks & targetMask) == 0UL) return false;

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

        return true;
    }
}

