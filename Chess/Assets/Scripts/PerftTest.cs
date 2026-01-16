using UnityEngine;

public class PerftTest : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [Range(1, 6)]
    [SerializeField] int depth;
    [ContextMenu("Run Perft")]
    void RunPerft()
    {
        ulong num = Perft();
        Debug.Log(num);
    }
    ulong Perft()
    {
        CustomMovesList movesList = new CustomMovesList();
        int moves, i;
        ulong nodes = 0;

        if (depth == 0)
        {
            return 1UL;
        }

        moves = _gameManager.BitboardScript.GenerateLegalMoves(movesList);

        //generate legal moves

        for(i = 0; i < moves; i++){
            _gameManager.BitboardScript.MakeMove(movesList.Moves[i]);
            depth--;
            nodes += Perft();
            _gameManager.BitboardScript.UndoMove(movesList.Moves[i]);
        }
        return nodes;
    }
}
