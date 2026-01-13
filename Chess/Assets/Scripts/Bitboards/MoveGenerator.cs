using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MoveGenerator
{
    //non sliding pieces lookup tables
    public static ulong[] KnightLookup = new ulong[64];
    public static ulong[] KingLookup = new ulong[64];
    public static ulong[] WhitePawnLookup = new ulong[64];
    public static ulong[] BlackPawnLookup = new ulong[64];

    //sliding pieces - magic bitboards
    private static ulong[][] _rookAttackTable = new ulong[64][];
    private static ulong[][] _bishopAttackTable = new ulong[64][];
    private static ulong[] _rookMasks = new ulong[64];
    private static ulong[] _bishopMasks = new ulong[64];

    //magic numbers and shifts
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

    static MoveGenerator()
    {
        InitialiseNonSlidingLookupTables();
        InitialiseRookAndBishopMagics();
    }
#region public utils
    public static void GeneratePseudoLegalMoves(
        ulong pawns, ulong knights, ulong bishops, ulong rooks, ulong queens, ulong kings,
        ulong allPieces, ulong ownPieces, ulong enemyPieces,
        bool isWhiteTurn, List<Move> moveList)
    {
        //non sliding pieces
        GenerateKnightMoves(knights, ownPieces, enemyPieces, moveList);
        GenerateKingMoves(kings, ownPieces, enemyPieces, moveList);
        GeneratePawnMoves(pawns, allPieces, enemyPieces, isWhiteTurn, moveList);

        //sliding pieces
        //doesnt matter if white or black piece type as attacks are the same
        GenerateSlidingMoves(bishops, Piece.WhiteBishop, allPieces, ownPieces, enemyPieces, moveList);
        GenerateSlidingMoves(rooks, Piece.WhiteRook, allPieces, ownPieces, enemyPieces, moveList);
        GenerateSlidingMoves(queens, Piece.WhiteQueen, allPieces, ownPieces, enemyPieces, moveList);
    }

    public static List<Move> GetMovesFromSquare(int square, Piece pieceType, 
        ulong allPieces, ulong ownPieces, ulong enemyPieces,
        bool isWhiteTurn)
    {
        List<Move> moves = new List<Move>();

        ulong mask = 1UL << square;


        switch (pieceType)
        {
            case Piece.WhiteBishop:
            case Piece.BlackBishop:
                GenerateSlidingMoves(mask, Piece.WhiteBishop, allPieces, ownPieces, enemyPieces, moves);
                break;

            case Piece.WhiteRook:
            case Piece.BlackRook:
                GenerateSlidingMoves(mask, Piece.WhiteRook, allPieces, ownPieces, enemyPieces, moves);
                break;

            case Piece.WhiteQueen:
            case Piece.BlackQueen:
                GenerateSlidingMoves(mask, Piece.WhiteQueen, allPieces, ownPieces, enemyPieces, moves);
                break;

            case Piece.WhiteKnight:
            case Piece.BlackKnight:
                GenerateKnightMoves(mask, ownPieces, enemyPieces, moves);
                break;

            case Piece.WhiteKing:
            case Piece.BlackKing:
                GenerateKingMoves(mask, ownPieces, enemyPieces, moves);
                break;

            case Piece.WhitePawn:
            case Piece.BlackPawn:
                GeneratePawnMoves(mask, allPieces, enemyPieces, isWhiteTurn, moves);
                break;
        }

        return moves;
    }
#endregion

#region Magic Bitboards
    //Public accesor 
    public static ulong GetSliderAttacks(Piece piece, int square, ulong allPieces)
    {
        ulong attacks = 0;

        //rooks and queens
        if (piece == Piece.WhiteRook || piece == Piece.BlackRook || piece == Piece.WhiteQueen || piece == Piece.BlackQueen)
        {
            ulong occupancy = allPieces & _rookMasks[square];
            int index = (int)((occupancy * _rookMagics[square]) >> _rookShifts[square]);
            attacks |= _rookAttackTable[square][index];
        }

        //bishops and queens
        if (piece == Piece.WhiteBishop || piece == Piece.BlackBishop || piece == Piece.WhiteQueen || piece == Piece.BlackQueen)
        {
            ulong occupancy = allPieces & _bishopMasks[square];
            int index = (int)((occupancy * _bishopMagics[square]) >> _bishopShifts[square]);
            attacks |= _bishopAttackTable[square][index];
        }

        return attacks;

    }
    // MAGIC BITBOARDS
    // code based on tord romstads implementation
    private static ulong MaskRookAttacks(int square)
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
    private static ulong MaskBishopAttacks(int square)
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
    private static ulong[] CreateAllBlockerBitboards(ulong mask)
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
            for (int bitIndex = 0; bitIndex < moveSquareIndices.Count; bitIndex++)
            {
                int bit = (patternIndex >> bitIndex) & 1;
                blockerBitboards[patternIndex] |= (ulong)bit << moveSquareIndices[bitIndex];

            }
        }

        return blockerBitboards;
    }

    //returns biboard of possible rook attacks
    private static ulong CalculateRookAttacks(int square, ulong blockers)
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
    private static ulong CalculateBishopAttacks(int square, ulong blockers)
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
    public static void InitialiseRookAndBishopMagics()
    {
        //allocate attack tables
        _rookAttackTable = new ulong[64][];
        _bishopAttackTable = new ulong[64][];

        for (int square = 0; square < 64; square++)
        {
            //rook
            _rookMasks[square] = MaskRookAttacks(square);
            int rookRelevantBits = BitboardHelpers.CountBits(_rookMasks[square]);
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
            int bishopRelevantBits = BitboardHelpers.CountBits(_bishopMasks[square]);
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
    #endregion

#region Move Generators
    private static void InitialiseNonSlidingLookupTables()
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
            possibleMoves = (startBit << NoEa & ~FILE_A) |
                            (startBit << NoWe & ~FILE_H) |
                            (startBit << EaEa & ~FILE_AB) |
                            (startBit << WoWe & ~FILE_GH) |
                            (startBit >> NoEa & ~FILE_H) |
                            (startBit >> NoWe & ~FILE_A) |
                            (startBit >> EaEa & ~FILE_GH) |
                            (startBit >> WoWe & ~FILE_AB);

            KnightLookup[squareIndex] = possibleMoves;

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

            KingLookup[squareIndex] = possibleMoves;

            //-----------------------------------------------------------------------Pawns

            ///white pawns
            ulong whiteAttacks = 0UL;
            whiteAttacks |= (startBit << 9) & ~FILE_A; //NE
            whiteAttacks |= (startBit << 7) & ~FILE_H; //NW

            WhitePawnLookup[squareIndex] = whiteAttacks;

            //black pawns
            ulong blackAttacks = 0UL;
            blackAttacks |= (startBit >> 7) & ~FILE_A; //SE
            blackAttacks |= (startBit >> 9) & ~FILE_H; //SW

            BlackPawnLookup[squareIndex] = blackAttacks;
        }
    }
    private static void GenerateSlidingMoves(ulong pieces, Piece pieceType, ulong allPieces, ulong ownPieces, ulong enemyPieces, List<Move> moveList)
    {
        while (pieces != 0)
        {
            int fromSquare = BitboardHelpers.PopLeastSignificantBit(ref pieces);

            ulong moves = GetSliderAttacks(pieceType, fromSquare, allPieces);

            moves &= ~ownPieces;

            while (moves != 0)
            {
                int toSquare = BitboardHelpers.PopLeastSignificantBit(ref moves);

                bool isCapture = (enemyPieces & (1UL << toSquare)) != 0;

                moveList.Add(new Move(fromSquare, toSquare, isCapture));
            }
        }
    }

    //pseudo legal knight moves
    private static void GenerateKnightMoves(ulong knights, ulong ownPieces, ulong enemyPieces, List<Move> moveList)
    {
        while (knights != 0)
        {
            //location of next knight
            int fromSquare = BitboardHelpers.PopLeastSignificantBit(ref knights);

            ulong moves = KnightLookup[fromSquare];

            //cant capture own pieces
            moves &= ~ownPieces;

            while (moves != 0)
            {
                int toSquare = BitboardHelpers.PopLeastSignificantBit(ref moves);

                //(1UL << toSquare)creates a bitmask for that specific square
                bool isCapture = (enemyPieces & (1UL << toSquare)) != 0;

                moveList.Add(new Move(fromSquare, toSquare, isCapture));
            }
        }
    }

    //pseudo legal king moves 
    //TODO: add castling
    private static void GenerateKingMoves(ulong kings, ulong ownPieces, ulong enemyPieces, List<Move> moveList)
    {
        while (kings != 0)
        {
            //location of next king
            int fromSquare = BitboardHelpers.PopLeastSignificantBit(ref kings);

            ulong moves = KingLookup[fromSquare];

            //cant capture own pieces
            moves &= ~ownPieces;

            while (moves != 0)
            {
                int toSquare = BitboardHelpers.PopLeastSignificantBit(ref moves);
                bool isCapture = (enemyPieces & (1UL << toSquare)) != 0;
                moveList.Add(new Move(fromSquare, toSquare, isCapture));
            }
        }
    }

    //pseudo legal pawn moves
    //TODO: add en passant and promotions
    private static void GeneratePawnMoves(ulong pawns, ulong allPieces, ulong enemyPieces, bool isWhite, List<Move> moveList)
    {
        ulong emptySquares = ~allPieces;

        //prevents wrap arounds when capturing
        ulong notAFile = 0xFEFEFEFEFEFEFEFEUL;
        ulong notHFile = 0x7F7F7F7F7F7F7F7FUL;

        if (isWhite)
        {
            ulong singlePush = (pawns << 8) & emptySquares;

            ulong rank3Mask = 0x0000000000FF0000UL;
            ulong doublePush = ((singlePush & rank3Mask) << 8) & emptySquares;

            ulong captureLeft = ((pawns & notAFile) << 7) & enemyPieces;
            ulong captureRight = ((pawns & notHFile) << 9) & enemyPieces;

            GetMovesFromBitboard(singlePush, 8, false, moveList);  //single push
            GetMovesFromBitboard(doublePush, 16, false, moveList); //double push
            GetMovesFromBitboard(captureLeft, 7, true, moveList);  //capture
            GetMovesFromBitboard(captureRight, 9, true, moveList); //capture

            //pawn promotions
            ulong rank8Mask = 0xFF00000000000000UL;
            ulong promotions = singlePush & rank8Mask;
            if(promotions != 0)
            {
                //A pawn has promoted
            }
        }
        else
        {
            ulong singlePush = (pawns >> 8) & emptySquares;

            ulong rank6Mask = 0x0000FF0000000000UL;
            ulong doublePush = ((singlePush & rank6Mask) >> 8) & emptySquares;

            //right for black is left for the white POV
            ulong captureRight = ((pawns & notHFile) >> 7) & enemyPieces;
            ulong captureLeft = ((pawns & notAFile) >> 9) & enemyPieces;
            
            GetMovesFromBitboard(singlePush, -8, false, moveList);  //single push
            GetMovesFromBitboard(doublePush, -16, false, moveList); //double push
            GetMovesFromBitboard(captureLeft, -9, true, moveList);  //capture
            GetMovesFromBitboard(captureRight, -7, true, moveList); //capture
        }
    }

    private static void GetMovesFromBitboard(ulong bitboard, int offset, bool isCapture, List<Move> moveList)
    {
        while (bitboard != 0)
        {
            //get the index of the target square
            int toIndex = BitboardHelpers.PopLeastSignificantBit(ref bitboard);

            //calculate where the pawn came from
            int fromIndex = toIndex - offset;

            moveList.Add(new Move(fromIndex, toIndex, isCapture));
        }
    }
    #endregion

    public static bool IsSquareAttacked(int square, bool byWhite, ulong allPieces, ulong knights, ulong bishops, ulong rooks, ulong queens, ulong kings, ulong pawns)
    {
        if ((KnightLookup[square] & knights) != 0) return true;

        if ((KingLookup[square] & kings) != 0) return true;

        ulong pawnAttacks = byWhite ? BlackPawnLookup[square] : WhitePawnLookup[square];
        if ((pawnAttacks & pawns) != 0) return true;

        //queen has the same attacks as rook and bishop combined
        ulong bishopAttacks = GetSliderAttacks(Piece.WhiteBishop, square, allPieces);
        if ((bishopAttacks & (bishops | queens)) != 0) return true;

        ulong rookAttacks = GetSliderAttacks(Piece.WhiteRook, square, allPieces);
        if ((rookAttacks & (rooks | queens)) != 0) return true;

        return false;
    }
}
