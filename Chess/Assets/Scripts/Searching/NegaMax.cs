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
            return QuiescenceSearch(alpha, beta);
            //return evaluator.Evaluate(bitboard);
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

        //if static evaluation already >= beta no need to search captures
        if (staticEval >= beta)
        {
            return staticEval;
        }

        if (staticEval > alpha)
        {
            alpha = staticEval;
        }

        CustomMovesList movesList = new CustomMovesList();
        bitboard.GenerateLegalMoves(movesList, true);

        for (int i = 0; i < movesList.Length; i++)
        {
            Move move = movesList.Moves[i];

            bitboard.MakeMove(move);
            int score = -QuiescenceSearch(-beta, -alpha);
            bitboard.UndoMove();

            //beta cutoff
            if (score >= beta)
            {
                return score;
            }
            
            //found better move
            if (score > alpha)
            {
                alpha = score;
            }
        }

        //return the best score found
        return alpha;
    }
}
