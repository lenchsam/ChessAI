using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

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
        _searchDepth = depth;
        CustomMovesList possibleMoves = new CustomMovesList();
        bitboard.GenerateLegalMoves(possibleMoves);

        Move bestMove = new Move();
        int maxEval = int.MinValue;

        int alpha = -int.MaxValue;
        int beta = int.MaxValue;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);

            int eval = -Search(depth - 1, -beta, -alpha);

            bitboard.UndoMove();

            if (eval > maxEval)
            {
                maxEval = eval;
                bestMove = move;
            }

            if (eval > alpha) alpha = eval;
        }
        return bestMove;
    }
    private int Search(int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            //TODO: quiescence search
            return evaluator.Evaluate(bitboard);
        }

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

        int maxEval = int.MinValue;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            maxEval = Mathf.Max(maxEval, eval);
            bitboard.UndoMove();

            //alpha beta pruning
            if (eval >= beta)
            {
                //move is too good so opponent will avoid it
                return beta;
            }

            if (eval > alpha)
            {
                alpha = eval;
            }
        }
        return alpha;
    }

    private int QuiescenceSearch(int alpha, int beta)
    {
        int staticEval = evaluator.Evaluate(bitboard);

        int bestValue = staticEval;
        if(bestValue >= beta)
        {
            return bestValue;
        }
        if (bestValue > alpha)
        {
            alpha = bestValue;
        }

        //get all capture moves
        //while still have capture moves to look at
        //makemove
        //score = -QuiescenceSearch(-alpha, -beta)
        //undomove
        //
        //if (score >= beta)
        //{
        //    return score;
        //}
        //if (score > alpha)
        //{
        //    alpha = score;
        //}

        return bestValue;
    }
}
