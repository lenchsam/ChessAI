using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Bitboards BitboardScript;
    public Evaluation Eval;
    [SerializeField] private Board _board;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private BoardSettings _boardSettings;
    [SerializeField] private VisualiseBitboard _visualiseBitboard;

    [Header("Promotion UI Elements")]
    [SerializeField] private GameObject _promotionUI;
    [SerializeField] private Button _queenPromotion;
    [SerializeField] private Button _rookPromotion;
    [SerializeField] private Button _bishopPromotion;
    [SerializeField] private Button _knightPromotion;

    private int _pendingPromotionFrom;
    private int _pendingPromotionTo;

    public UnityEvent<int, int> OnMoveRequested = new UnityEvent<int, int>();
    void Awake()
    {
        BitboardScript = new Bitboards();
        Eval = new Evaluation();
        _board.SetBoardColour(_boardSettings.whiteColor, _boardSettings.blackColor);
        _visualiseBitboard.SetHighlightColour(_boardSettings.highlightColor);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnMoveRequested.AddListener(OnMoveRequestedHandler);

        _queenPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteQueen));
        _rookPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteRook));
        _bishopPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteBishop));
        _knightPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteKnight));

        //uppercase = white lowercase = black
        BitboardScript.FENtoBitboards("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R");

        _board.CreateBoard();
        _board.DisplayPieces(BitboardScript);
    }

    void OnMoveRequestedHandler(int from, int to)
    {
        //check for promotion
        if (BitboardScript.IsPromotionMove(from, to))
        {
            _pendingPromotionFrom = from;
            _pendingPromotionTo = to;

            _promotionUI.SetActive(true);

            return;
        }

        //normal move
        FinalizeMove(from, to, PawnPromotion.None);
    }
    private void OnPromotionButton(PawnPromotion type)
    {
        _promotionUI.SetActive(false);
        FinalizeMove(_pendingPromotionFrom, _pendingPromotionTo, type);
    }

    public void FinalizeMove(int from, int to, PawnPromotion promotionType)
    {
        int moveFlag;

        bool wasMoveMade = BitboardScript.MovePiece(from, to, out moveFlag, promotionType);

        if (wasMoveMade)
        {
            Piece visualPiece = Piece.None;

            // Convert logic enum to visual enum
            if (promotionType != PawnPromotion.None)
            {
                bool isWhite = (to >= 56);
                visualPiece = GetPieceFromPromotion(promotionType, isWhite);
            }

            _board.MovePieceVisual(from, to, visualPiece, moveFlag);

            _playerController.ToggleIsWhite();
        }
        else
        {
            //move not legal
            //reset piece position
            int fromX = from % 8;
            int fromY = from / 8;
            GameObject piece = _board.GetPieceFromPosition(from);
            if (piece != null) piece.transform.position = new Vector2(fromX, fromY);
        }

        Eval.Evaluate(BitboardScript);
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