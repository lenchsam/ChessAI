using UnityEngine;

public class NegaMax
{
    Bitboards bitboard;
    Evaluation evaluator;

    public NegaMax(Bitboards board)
    {
        bitboard = board;
        evaluator = new Evaluation();
    }
    public Move FindBestMove(int depth)
    {
        CustomMovesList possibleMoves = new CustomMovesList();
        bitboard.GenerateLegalMoves(possibleMoves);

        //get random moves first
        //return possibleMoves.Moves[Random.Range(0, possibleMoves.Length)];

        Move bestMove = new Move();
        int maxEval = int.MinValue;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);

            int eval = -Search(depth - 1);

            bitboard.UndoMove(move);

            if (eval > maxEval)
            {
                maxEval = eval;
                bestMove = move;
            }
        }
        return bestMove;
    }
    private int Search(int depth)
    {
        if(depth == 0)
        {
            //return evaluation
            return 0;
        }

        CustomMovesList possibleMoves = new CustomMovesList();
        bitboard.GenerateLegalMoves(possibleMoves);

        if(possibleMoves.Length == 0)
        {
            //checkmate or stalemate
            if(bitboard.IsInCheck())
            {
                //checkmate
                return int.MinValue;
            }
            //stalemate
            return 0;
        }

        int maxEval = int.MinValue;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);
            int eval = -Search(depth - 1);
            maxEval = Mathf.Max(maxEval, eval);
            bitboard.UndoMove(move);
        }
        return maxEval;
    }
}
