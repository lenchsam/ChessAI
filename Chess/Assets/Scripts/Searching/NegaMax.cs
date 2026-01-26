using UnityEngine;

public class NegaMax
{
    Bitboards bitboard;
    Evaluation evaluator;

    private int _searchDepth;
    private const int CHECKMATE = 50000;

    public NegaMax(Bitboards board)
    {
        bitboard = board;
        evaluator = new Evaluation();
    }
    public Move FindBestMove(int depth)
    {
        CustomMovesList possibleMoves = new CustomMovesList();
        bitboard.GenerateLegalMoves(possibleMoves);

        Move bestMove = new Move();
        int maxEval = int.MinValue;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);

            int eval = -Search(depth - 1);

            bitboard.UndoMove();

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
        CustomMovesList possibleMoves = new CustomMovesList();
        bitboard.GenerateLegalMoves(possibleMoves);

        if(possibleMoves.Length == 0)
        {
            //checkmate or stalemate
            if(bitboard.IsInCheck())
            {
                //checkmate
                //dist used to prefer faster checkmates
                int dist = (_searchDepth - depth);
                int mateScore = -CHECKMATE + dist;
                return mateScore;
            }
            //stalemate
            return 0;
        }

        if(depth == 0)
        {
            //TODO: quiescence search
            return evaluator.Evaluate(bitboard);
        }

        int maxEval = int.MinValue;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);
            int eval = -Search(depth - 1);
            maxEval = Mathf.Max(maxEval, eval);
            bitboard.UndoMove();
        }
        return maxEval;
    }
}
