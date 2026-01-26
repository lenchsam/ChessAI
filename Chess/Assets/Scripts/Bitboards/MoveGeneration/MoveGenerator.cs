using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
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

    static MoveGenerator()
    {
        InitialiseNonSlidingLookupTables();
        InitialiseRookAndBishopMagics();
    }
#region public utils
    public static void GeneratePseudoLegalMoves(
        Material playerMaterial,
        ulong allPieces, ulong ownPieces, ulong enemyPieces,
        bool isWhiteTurn, 
        ushort enPassantMask,
        Castling castlingRights,
        CustomMovesList moveList)
    {
        //non sliding pieces
        GenerateKnightMoves(playerMaterial.Knights, ownPieces, enemyPieces, moveList);
        GenerateKingMoves(playerMaterial.King, ownPieces, enemyPieces, moveList, castlingRights, allPieces);
        GeneratePawnMoves(playerMaterial.Pawns, allPieces, enemyPieces, isWhiteTurn, moveList, enPassantMask);

        //sliding pieces
        //doesnt matter if white or black piece type as attacks are the same
        GenerateSlidingMoves(playerMaterial.Bishops, Piece.WhiteBishop, allPieces, ownPieces, enemyPieces, moveList);
        GenerateSlidingMoves(playerMaterial.Rooks, Piece.WhiteRook, allPieces, ownPieces, enemyPieces, moveList);
        GenerateSlidingMoves(playerMaterial.Queens, Piece.WhiteQueen, allPieces, ownPieces, enemyPieces, moveList);
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
            int index = (int)((occupancy * Magics.RookMagics[square]) >> Magics.RookShifts[square]);
            attacks |= _rookAttackTable[square][index];
        }

        //bishops and queens
        if (piece == Piece.WhiteBishop || piece == Piece.BlackBishop || piece == Piece.WhiteQueen || piece == Piece.BlackQueen)
        {
            ulong occupancy = allPieces & _bishopMasks[square];
            int index = (int)((occupancy * Magics.BishopMagics[square]) >> Magics.BishopShifts[square]);
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
            Magics.RookShifts[square] = 64 - rookRelevantBits;

            ulong[] rookBlockers = CreateAllBlockerBitboards(_rookMasks[square]);
            _rookAttackTable[square] = new ulong[rookBlockers.Length];

            for (int i = 0; i < rookBlockers.Length; i++)
            {
                ulong occupancy = rookBlockers[i];
                int index = (int)((occupancy * Magics.RookMagics[square]) >> Magics.RookShifts[square]);
                _rookAttackTable[square][index] = CalculateRookAttacks(square, occupancy);
            }

            //bishop
            _bishopMasks[square] = MaskBishopAttacks(square);
            int bishopRelevantBits = BitboardHelpers.CountBits(_bishopMasks[square]);
            Magics.BishopShifts[square] = 64 - bishopRelevantBits;

            ulong[] bishopBlockers = CreateAllBlockerBitboards(_bishopMasks[square]);
            _bishopAttackTable[square] = new ulong[bishopBlockers.Length];

            for (int i = 0; i < bishopBlockers.Length; i++)
            {
                ulong occupancy = bishopBlockers[i];
                int index = (int)((occupancy * Magics.BishopMagics[square]) >> Magics.BishopShifts[square]);
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
    private static void GenerateSlidingMoves(ulong pieces, Piece pieceType, ulong allPieces, ulong ownPieces, ulong enemyPieces, CustomMovesList moveList)
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

                // Use Code 4 (Capture) or Code 0 (Quiet)
                int flag = isCapture ? MoveFlag.Capture : MoveFlag.None;

                moveList.Add(new Move(fromSquare, toSquare, flag));
            }
        }
    }

    //pseudo legal knight moves
    private static void GenerateKnightMoves(ulong knights, ulong ownPieces, ulong enemyPieces, CustomMovesList moveList)
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

                int flag = isCapture ? MoveFlag.Capture : MoveFlag.None;
                moveList.Add(new Move(fromSquare, toSquare, flag));
            }
        }
    }

    //pseudo legal king moves 
    private static void GenerateKingMoves(ulong kings, ulong ownPieces, ulong enemyPieces, CustomMovesList moveList, Castling castlingRights, ulong allPieces)
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

                int flag = isCapture ? MoveFlag.Capture : MoveFlag.None;
                moveList.Add(new Move(fromSquare, toSquare, flag));
            }

            GenerateCastlingMoves(fromSquare, moveList, castlingRights, allPieces);
        }
    }

    private static void GenerateCastlingMoves(int kingSquare, CustomMovesList moveList, Castling castlingRights, ulong allPieces)
    {
        bool isWhite = kingSquare == 4;
        bool isBlack = kingSquare == 60;

        if (!isWhite && !isBlack) return;

        //white
        if (isWhite)
        {
            //king side
            if ((castlingRights & Castling.WhiteKing) != 0)
            {
                if (((allPieces >> 5) & 1) == 0 &&
                    ((allPieces >> 6) & 1) == 0)
                {
                    moveList.Add(new Move(4, 6, MoveFlag.CastleKingSide));
                }
            }

            //queen side
            if ((castlingRights & Castling.WhiteQueen) != 0)
            {
                if (((allPieces >> 1) & 1) == 0 &&
                    ((allPieces >> 2) & 1) == 0 &&
                    ((allPieces >> 3) & 1) == 0)
                {
                    moveList.Add(new Move(4, 2, MoveFlag.CastleQueenSide));
                }
            }
        }

        //black
        if (isBlack)
        {
            //king side
            if ((castlingRights & Castling.BlackKing) != 0)
            {
                if (((allPieces >> 61) & 1) == 0 &&
                    ((allPieces >> 62) & 1) == 0)
                {
                    moveList.Add(new Move(60, 62, MoveFlag.CastleKingSide));
                }
            }

            //queen side
            if ((castlingRights & Castling.BlackQueen) != 0)
            {
                if (((allPieces >> 57) & 1) == 0 &&
                    ((allPieces >> 58) & 1) == 0 &&
                    ((allPieces >> 59) & 1) == 0)
                {
                    moveList.Add(new Move(60, 58, MoveFlag.CastleQueenSide));
                }
            }
        }
    }

    //pseudo legal pawn moves
    private static void GeneratePawnMoves(ulong pawns, ulong allPieces, ulong enemyPieces, bool isWhite, CustomMovesList moveList, ushort enPassantFlag)
    {
        ulong emptySquares = ~allPieces;

        //prevents wrap arounds when capturing
        ulong notAFile = 0xFEFEFEFEFEFEFEFEUL;
        ulong notHFile = 0x7F7F7F7F7F7F7F7FUL;

        ulong rank8Mask = 0xFF00000000000000UL;
        ulong rank1Mask = 0x00000000000000FFUL;
        
        ulong notRank8 = ~rank8Mask;
        ulong notRank1 = ~rank1Mask;

        if (isWhite)
        {
            ulong singlePush = (pawns  << 8) & emptySquares;

            ulong rank3Mask = 0x0000000000FF0000UL;
            ulong doublePush = ((singlePush & rank3Mask) << 8) & emptySquares;

            ulong quietPush = singlePush & notRank8;
            ulong promotionPush = singlePush & rank8Mask;

            GetMovesFromBitboard(quietPush, 8, MoveFlag.None, moveList);  //single push, no promotion
            AddPromotionMoves(promotionPush, 8, false, moveList);

            //cannot promote from double push so no need to account for it here
            GetMovesFromBitboard(doublePush, 16, MoveFlag.PawnDoublePush, moveList); //double push

            ulong captureLeft = ((pawns & notAFile) << 7) & enemyPieces;
            ulong captureRight = ((pawns & notHFile) << 9) & enemyPieces;

            GetMovesFromBitboard(captureLeft & notRank8, 7, MoveFlag.Capture, moveList);  //capture
            GetMovesFromBitboard(captureRight & notRank8, 9, MoveFlag.Capture, moveList); //capture

            AddPromotionMoves(captureLeft & rank8Mask, 7, true, moveList);  //promotion from capture
            AddPromotionMoves(captureRight & rank8Mask, 9, true, moveList); //promotion from capture

            //en passant moves
            if (enPassantFlag != 0)
            {
                //get file from mask
                ulong maskCopy = enPassantFlag;
                int enPassantFile = BitboardHelpers.PopLeastSignificantBit(ref maskCopy);

                //white captures on rank 6 which is index 5
                //* 8 + file gives square index
                int epTargetSquare = (5 * 8) + enPassantFile;

                //rank 5 mask
                //if a pawn isnt on rank5, it's impossible for it to en passant
                ulong rank5Mask = 0x000000FF00000000UL;
                ulong rank5Pawns = pawns & rank5Mask;

                while (rank5Pawns != 0)
                {
                    int fromSquare = BitboardHelpers.PopLeastSignificantBit(ref rank5Pawns);
                    int fromFile = fromSquare % 8;

                    //check if pawn is adjacent to en passant file
                    if (Math.Abs(fromFile - enPassantFile) == 1)
                    {
                        moveList.Add(new Move(fromSquare, epTargetSquare, MoveFlag.EnPassantCapture));
                    }
                }
            }
        }
        else
        {
            ulong singlePush = (pawns >> 8) & emptySquares;

            ulong rank6Mask = 0x0000FF0000000000UL;
            ulong doublePush = ((singlePush & rank6Mask) >> 8) & emptySquares;

            ulong quietPush = singlePush & notRank1;
            ulong promotionPush = singlePush & rank1Mask;

            GetMovesFromBitboard(quietPush, -8, MoveFlag.None, moveList);  //single push, no promotion
            AddPromotionMoves(promotionPush, -8, false, moveList);

            //cannot promote from double push so no need to account for it here
            GetMovesFromBitboard(doublePush, -16, MoveFlag.PawnDoublePush, moveList); //double push

            //right for black is left for the white POV
            ulong captureRight = ((pawns & notHFile) >> 7) & enemyPieces;
            ulong captureLeft = ((pawns & notAFile) >> 9) & enemyPieces;
            
            GetMovesFromBitboard(captureLeft & notRank1, -9, MoveFlag.Capture, moveList);  //capture
            GetMovesFromBitboard(captureRight & notRank1, -7, MoveFlag.Capture, moveList); //capture

            AddPromotionMoves(captureLeft & rank1Mask, -9, true, moveList);  //promotion from capture
            AddPromotionMoves(captureRight & rank1Mask, -7, true, moveList); //promotion from capture

            if (enPassantFlag != 0)
            {
                ulong maskCopy = enPassantFlag;
                int enPassantFile = BitboardHelpers.PopLeastSignificantBit(ref maskCopy);

                //black captures on rank 3 which is index 2
                //* 8 + file gives square index
                int epTargetSquare = (2 * 8) + enPassantFile;

                //rank 4 mask
                //if a pawn isnt on rank4, it's impossible for it to en passant
                ulong rank4Mask = 0x00000000FF000000UL;
                ulong rank4Pawns = pawns & rank4Mask;

                while (rank4Pawns != 0)
                {
                    int fromSquare = BitboardHelpers.PopLeastSignificantBit(ref rank4Pawns);
                    int fromFile = fromSquare % 8;

                    //check if pawn is adjacent to en passant file
                    if (Math.Abs(fromFile - enPassantFile) == 1)
                    {
                        moveList.Add(new Move(fromSquare, epTargetSquare, MoveFlag.EnPassantCapture));
                    }
                }
            }
        }
    }

    private static void AddPromotionMoves(ulong promotionPawns, int offset, bool isCapture, CustomMovesList movesList)
    {
        while (promotionPawns != 0)
        {
            int toIndex = BitboardHelpers.PopLeastSignificantBit(ref promotionPawns);
            int fromIndex = toIndex - offset;

            // If it's a capture, we use flags 12-15. If not, we use 8-11.
            if (isCapture)
            {
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteQueenCapture));
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteRookCapture));
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteBishopCapture));
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteKnightCapture));
            }
            else
            {
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteQueen));
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteRook));
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteBishop));
                movesList.Add(new Move(fromIndex, toIndex, MoveFlag.PromoteKnight));
            }
        }
    }
    private static void GetMovesFromBitboard(ulong bitboard, int offset, int flag, CustomMovesList moveList)
    {
        while (bitboard != 0)
        {
            //get the index of the target square
            int toIndex = BitboardHelpers.PopLeastSignificantBit(ref bitboard);

            //calculate where the pawn came from
            int fromIndex = toIndex - offset;

            moveList.Add(new Move(fromIndex, toIndex, flag));
        }
    }
    #endregion
}
