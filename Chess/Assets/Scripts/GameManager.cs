using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public Bitboards BitboardScript;
    [SerializeField] private Board _board;

    public UnityEvent<Vector2Int, Vector2Int> OnMoveRequested = new UnityEvent<Vector2Int, Vector2Int>();
    void Awake()
    {
        BitboardScript = new Bitboards();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnMoveRequested.AddListener(OnMoveRequestedHandler);

        //uppercase = white lowercase = black
        BitboardScript.FENtoBitboards("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
        BitboardScript.GenerateLookupTables();

        _board.CreateBoard();
        _board.DisplayPieces(BitboardScript);
    }

    void OnMoveRequestedHandler(Vector2Int from, Vector2Int to)
    {
        bool wasMoveMade = BitboardScript.MovePiece(from, to);
        if (wasMoveMade)
        {
            _board.MovePieceVisual(from, to);
        }
        else
        {
            //resent piece to original position
            GameObject piece = _board.GetPieceFromPosition(from);
            piece.transform.position = new Vector2(from.x, from.y);
        }
    }
}
