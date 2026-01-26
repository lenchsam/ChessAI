using System.Collections.Generic;

public static class FENParser
{
    //FEN character to piece index
    private static readonly Dictionary<char, Piece> _fenToPiece = new Dictionary<char, Piece>()
    {
        {'P', Piece.WhitePawn},   {'N', Piece.WhiteKnight},
        {'B', Piece.WhiteBishop}, {'R', Piece.WhiteRook},
        {'Q', Piece.WhiteQueen},  {'K', Piece.WhiteKing},
        {'p', Piece.BlackPawn},   {'n', Piece.BlackKnight},
        {'b', Piece.BlackBishop}, {'r', Piece.BlackRook},
        {'q', Piece.BlackQueen},  {'k', Piece.BlackKing}
    };


    public static void FENtoBitboards(string FEN, ulong[] bitboards, Piece[] boardSquares, ref bool isWhiteTurn, ref Castling castlingRights, ref ushort enPassantMask)
    {
        //reset to defaults
        isWhiteTurn = true;
        castlingRights = Castling.None;
        enPassantMask = 0;

        //clear arrays
        for (int i = 0; i < bitboards.Length; i++)
        {
            bitboards[i] = 0UL;
        }
        for (int i = 0; i < boardSquares.Length; i++)
        {
            boardSquares[i] = Piece.None;
        }

        string[] fenParts = FEN.Split(' ');

        ParsePiecePlacement(fenParts[0], bitboards, boardSquares);

        //get active colour
        if (fenParts.Length > 1)
        {
            isWhiteTurn = fenParts[1] == "w";
        }

        //get castling rights
        if (fenParts.Length > 2)
        {
            castlingRights = ParseCastlingRights(fenParts[2]);
        }

        //get en passant square
        if (fenParts.Length > 3)
        {
            enPassantMask = ParseEnPassantSquare(fenParts[3]);
        }

        //TODO: half move clock and full move number
    }

    private static void ParsePiecePlacement(string placement, ulong[] bitboards, Piece[] boardSquares)
    {
        int row = 7;
        int col = 0;

        foreach (char c in placement)
        {
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
            else if (_fenToPiece.ContainsKey(c))
            {
                int pieceIndex = (int)_fenToPiece[c];
                int squareIndex = row * 8 + col;
                ulong squareBit = 1UL << squareIndex;

                bitboards[pieceIndex] |= squareBit;
                boardSquares[squareIndex] = (Piece)pieceIndex;

                col++;
            }
        }
    }

    private static Castling ParseCastlingRights(string castlingString)
    {
        //stored as 
        //KQkg = full
        //- = none
        if (castlingString == "-")
        {
            return Castling.None;
        }

        Castling rights = Castling.None;

        foreach (char c in castlingString)
        {
            switch (c)
            {
                case 'K': rights |= Castling.WhiteKing; break;
                case 'Q': rights |= Castling.WhiteQueen; break;
                case 'k': rights |= Castling.BlackKing; break;
                case 'q': rights |= Castling.BlackQueen; break;
            }
        }

        return rights;
    }

    private static ushort ParseEnPassantSquare(string enPassantString)
    {
        if (enPassantString == "-")
        {
            return 0;
        }

        //en passant in fen is given as a square e.g. a3
        //need to convert this

        if (enPassantString.Length >= 2)
        {
            char file = enPassantString[0];
            int fileIndex = file - 'a'; //convert file letter to index 0-7

            if (fileIndex >= 0 && fileIndex < 8)
            {
                return (ushort)(1 << fileIndex);
            }
        }

        return 0;
    }
}
