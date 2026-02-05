using System;

public class MoveOrdering
{
    private int[] _moveScores = new int[256];

    public void OrderMoves(CustomMovesList movesList, Bitboards board)
    {
        int count = movesList.Length;

        for (int i = 0; i < count; i++)
        {
            //scored as negative due to Array.Sort sorting in ascending order
            _moveScores[i] = -GetMoveScore(movesList.Moves[i], board);
        }

        //sort moves based on _moveScores array. move with highest score will come first.
        Array.Sort(_moveScores, movesList.Moves, 0, count);
    }

    private int GetMoveScore(Move move, Bitboards board)
    {
        int score = 0;

        //prioritis captures using MVV-LVA
        if (move.IsCapture)
        {
            Piece attacker = board.GetPieceOnSquare(move.StartingPos);
            Piece victim = Piece.None;

            //en passant capture, victim is not on target square but is always a pawn
            if (move.Flag == MoveFlag.EnPassantCapture)
            {
                victim = board.GetTurn() ? Piece.BlackPawn : Piece.WhitePawn;
            }
            else
            {
                victim = board.GetPieceOnSquare(move.EndingPos);
            }

            //offset of 10000 to ensure all captures are searched before quiet moves
            // * 10 ensures that more weight is given to victims value than attackers value
            score = 10000 + (GetPieceValue(victim) * 10) - GetPieceValue(attacker);
        }

        //prioritise promotions
        if (move.IsPromotion)
        {
            score += 20000 + GetPieceValue(move.PromotionPieceType);
        }

        return score;
    }

    private int GetPieceValue(Piece piece)
    {
        //values not from evaluation function as eval function uses PeSTO
        switch (piece)
        {
            case Piece.WhitePawn: case Piece.BlackPawn: return 100;
            case Piece.WhiteKnight: case Piece.BlackKnight: return 300;
            case Piece.WhiteBishop: case Piece.BlackBishop: return 320;
            case Piece.WhiteRook: case Piece.BlackRook: return 500;
            case Piece.WhiteQueen: case Piece.BlackQueen: return 900;
            case Piece.WhiteKing: case Piece.BlackKing: return 10000;
            default: return 0;
        }
    }
}
