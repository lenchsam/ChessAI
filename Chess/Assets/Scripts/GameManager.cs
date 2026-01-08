using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public Bitboards BitboardScript;
    [SerializeField] private Board _board;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private BoardSettings _boardSettings;
    [SerializeField] private VisualiseBitboard _visualiseBitboard;

    public UnityEvent<int, int> OnMoveRequested = new UnityEvent<int, int>();
    void Awake()
    {
        BitboardScript = new Bitboards();
        _board.SetBoardColour(_boardSettings.whiteColor, _boardSettings.blackColor);
        _visualiseBitboard.SetHighlightColour(_boardSettings.highlightColor);

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnMoveRequested.AddListener(OnMoveRequestedHandler);

        //uppercase = white lowercase = black
        BitboardScript.FENtoBitboards("rnbqkbnr/pppp1ppp/8/8/8/8/PPPP1PPP/RNBQKBNR");

        _board.CreateBoard();
        _board.DisplayPieces(BitboardScript);
    }

    void OnMoveRequestedHandler(int from, int to)
    {
        bool wasMoveMade = BitboardScript.MovePiece(from, to);
        if (wasMoveMade)
        {
            _board.MovePieceVisual(from, to);
            _playerController.ToggleIsWhite();
        }
        else
        {
            int fromX = from % 8;
            int fromY = from / 8;
            //resent piece to original position
            GameObject piece = _board.GetPieceFromPosition(from);
            piece.transform.position = new Vector2(fromX, fromY);
        }
    }
}