using UnityEngine;

public class NegaMax
{
    Bitboards bitboard;
    Evaluation evaluator;

    private int _searchDepth;
    private const int CHECKMATE = 50000;
    private const int MAX_PLY = 64;

    private CustomMovesList[] moveLists;

    public NegaMax(Bitboards board)
    {
        bitboard = board;
        evaluator = new Evaluation();

        moveLists = new CustomMovesList[MAX_PLY];
        for (int i = 0; i < MAX_PLY; i++)
        {
            moveLists[i] = new CustomMovesList();
        }
    }
    public Move FindBestMove(int depth)
    {
        _searchDepth = depth;
        CustomMovesList possibleMoves = moveLists[0];
        possibleMoves.Clear();

        bitboard.GenerateLegalMoves(possibleMoves);

        Move bestMove = new Move();
        int maxEval = int.MinValue;

        int alpha = -100000;
        int beta = 100000;

        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move move = possibleMoves.Moves[i];
            bitboard.MakeMove(move);

            int eval = -Search(depth - 1, -beta, -alpha, 1);

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

    private int Search(int depth, int alpha, int beta, int ply)
    {
        if (ply >= MAX_PLY) return evaluator.Evaluate(bitboard);

        CustomMovesList possibleMoves = moveLists[ply];
        possibleMoves.Clear();

        if (depth == 0)
        {
            return QuiescenceSearch(alpha, beta, ply);
            //return evaluator.Evaluate(bitboard);
        }

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
            int eval = -Search(depth - 1, -beta, -alpha, ply + 1);
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
    private int QuiescenceSearch(int alpha, int beta, int ply)
    {
        if (ply >= MAX_PLY) return evaluator.Evaluate(bitboard);

        int staticEval = evaluator.Evaluate(bitboard);

        //if static evaluation already >= beta no need to search captures
        if (staticEval >= beta)
        {
            return beta;
        }

        if (staticEval > alpha)
        {
            alpha = staticEval;
        }

        CustomMovesList movesList = moveLists[ply];
        movesList.Clear();

        bitboard.GenerateLegalMoves(movesList, true);

        for (int i = 0; i < movesList.Length; i++)
        {
            Move move = movesList.Moves[i];

            bitboard.MakeMove(move);
            int score = -QuiescenceSearch(-beta, -alpha, ply + 1);
            bitboard.UndoMove();

            //beta cutoff
            if (score >= beta)
            {
                return beta;
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
