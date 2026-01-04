using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public Bitboards BitboardScript;
    [SerializeField] private Board _board;
    [SerializeField] private PlayerController _playerController;

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
        BitboardScript.FENtoBitboards("r1bq1rk1/pp1n1ppp/2p2n2/3p4/3P4/2NBPN2/PP3PPP/R1BQ1RK1");
        BitboardScript.GenerateLookupTables();
        BitboardScript.InitialiseRookAndBishopMagics();

        _board.CreateBoard();
        _board.DisplayPieces(BitboardScript);
    }
    void OnMoveRequestedHandler(Vector2Int from, Vector2Int to)
    {
        bool wasMoveMade = BitboardScript.MovePiece(from, to);
        if (wasMoveMade)
        {
            _board.MovePieceVisual(from, to);
            _playerController.ToggleIsWhite();
        }
        else
        {
            //resent piece to original position
            GameObject piece = _board.GetPieceFromPosition(from);
            piece.transform.position = new Vector2(from.x, from.y);
        }
    }
}
