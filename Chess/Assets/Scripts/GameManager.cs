using TMPro.EditorUtilities;
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
        BitboardScript.FENtoBitboards("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

        _board.CreateBoard();
        _board.DisplayPieces(BitboardScript);
    }

    void OnMoveRequestedHandler(int from, int to)
    {
        //promotion handling
        PawnPromotion promotionType = PawnPromotion.None;

        foreach (var move in BitboardScript.GetCurrentLegalMoves())
        {
            if (move.StartingPos == from && move.EndingPos == to && move.PawnPromotion != PawnPromotion.None)
            {
                promotionType = move.PawnPromotion;
                break;
            }
        }

        //attempty move
        bool wasMoveMade = BitboardScript.MovePiece(from, to);
        if (wasMoveMade)
        {
            Piece promotedPieceVisual = Piece.None;

            if (promotionType != PawnPromotion.None)
            {
                //>=56 is rank 8
                bool isWhitePiece = (to >= 56);
                promotedPieceVisual = GetPieceFromPromotion(promotionType, isWhitePiece);
            }

            _board.MovePieceVisual(from, to, promotedPieceVisual);
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

    private Piece GetPieceFromPromotion(PawnPromotion promotion, bool isWhite)
    {
        switch (promotion)
        {
            case PawnPromotion.PromoteQueen: return isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
            case PawnPromotion.PromoteRook: return isWhite ? Piece.WhiteRook : Piece.BlackRook;
            case PawnPromotion.PromoteBishop: return isWhite ? Piece.WhiteBishop : Piece.BlackBishop;
            case PawnPromotion.PromoteKnight: return isWhite ? Piece.WhiteKnight : Piece.BlackKnight;
            default: return isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
        }
    }

    public void RestartGame()
    {
        BitboardScript.SetTurn(true);
        _playerController.IsPlayerWhite = true;
        BitboardScript.FENtoBitboards("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
        _board.DisplayPieces(BitboardScript);
    }
}