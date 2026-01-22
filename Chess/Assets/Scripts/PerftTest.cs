using System.Diagnostics;
using UnityEngine;

public class PerftTest : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [Range(1, 6)]
    [SerializeField] int _depth = 3;

    private CustomMovesList[] _moveLists;
    void Awake()
    {
        _moveLists = new CustomMovesList[10];
        for (int i = 0; i < _moveLists.Length; i++)
        {
            _moveLists[i] = new CustomMovesList();
        }
    }

    [ContextMenu("Run Perft")]
    void RunPerft()
    {
        //ensures a fare test
        System.GC.Collect(); 

        Stopwatch sw = new Stopwatch();
        sw.Start();

        ulong totalNodes = Perft(_depth);

        sw.Stop();

        long time = sw.ElapsedMilliseconds;
        if (sw.ElapsedMilliseconds == 0)
        {
            time = 1;
        }
        ulong nps = (totalNodes * 1000) / (ulong)(time);

        UnityEngine.Debug.Log($"Perft({_depth}) Nodes: {totalNodes} | Time: {sw.ElapsedMilliseconds}ms | NPS: {nps:N0}");
    }

    ulong Perft(int currentDepth)
    {
        if (currentDepth == 0) return 1UL;

        ulong nodes = 0;

        CustomMovesList movesList = _moveLists[currentDepth];
        movesList.Clear();

        _gameManager.BitboardScript.GenerateLegalMoves(movesList);

        //go through every legal move
        for (int i = 0; i < movesList.Length; i++)
        {
            Move move = movesList.Moves[i];

            //needed so we can undo captures correctly
            Piece capturedPiece = _gameManager.BitboardScript.GetPieceOnSquare(move.EndingPos);

            Castling oldCastlingRights = _gameManager.BitboardScript.CastlingRights;
            ushort oldEnPassantMask = _gameManager.BitboardScript.EnPassantMask;

            _gameManager.BitboardScript.MakeMove(move);
            nodes += Perft(currentDepth - 1);
            _gameManager.BitboardScript.UndoMove(move, capturedPiece);

            _gameManager.BitboardScript.CastlingRights = oldCastlingRights;
            _gameManager.BitboardScript.EnPassantMask = oldEnPassantMask;
        }

        return nodes;
    }
}
