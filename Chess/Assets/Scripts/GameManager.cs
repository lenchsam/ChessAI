using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public enum AiSide
{
    None,
    Black,
    White,
    Both
}
public class GameManager : MonoBehaviour
{

    //public get, private set
    //allow other scripts to read but not modify
    public Bitboards BitboardScript { get; private set; }
    public Evaluation Eval { get; private set; }
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

    private NegaMax1 _whiteEngine;
    private NegaMax _blackEngine;

    [Header("AI Settings")]
    [Range(1, 6)]
    [SerializeField] private int _whiteDepth = 5;
    [Range(1, 6)]
    [SerializeField] private int _blackDepth = 5;
    [SerializeField] private AiSide _aiSide = AiSide.Both;
    private int whiteWins, blackWins, draws;
    private TestingManager _testingManager;

    public UnityEvent<int, int> OnMoveRequested = new UnityEvent<int, int>();
    void Awake()
    {
        _testingManager = FindObjectOfType<TestingManager>();

        BitboardScript = new Bitboards();
        Eval = new Evaluation();

        _whiteEngine = new NegaMax1(BitboardScript);
        _blackEngine = new NegaMax(BitboardScript);

        _board.SetBoardColour(_boardSettings.whiteColor, _boardSettings.blackColor);
        _visualiseBitboard.SetHighlightColour(_boardSettings.highlightColor);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BitboardScript.GameEnded.AddListener(OnGameEnded);
        OnMoveRequested.AddListener(OnMoveRequestedHandler);

        _queenPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteQueen));
        _rookPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteRook));
        _bishopPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteBishop));
        _knightPromotion.onClick.AddListener(() => OnPromotionButton(PawnPromotion.PromoteKnight));;

        RestartGame();
    }

    private void OnGameEnded(EndingState state, bool whiteWon)
    {
        if (state == EndingState.Checkmate)
        {
            if (whiteWon) 
                whiteWins++;
            else 
                blackWins++;
        }
        else
        {
            draws++;
        }
    }

    void OnMoveRequestedHandler(int from, int to)
    {
        bool isWhiteTurn = BitboardScript.GetTurn();
        if (ShouldAiMove(isWhiteTurn)) return; //stop player from moving on AIs turn

        //check for promotion
        if (BitboardScript.IsPromotionMove(from, to))
        {
            _pendingPromotionFrom = from;
            _pendingPromotionTo = to;

            _promotionUI.SetActive(true);

            return;
        }

        if(from == to)
        {
            //clicked on same square
            //reset piece position
            int fromX = from % 8;
            int fromY = from / 8;
            GameObject piece = _board.GetPieceFromPosition(from);
            if (piece != null) piece.transform.position = new Vector2(fromX, fromY);
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
            _visualiseBitboard.ShowBitboardOverlay(0);
            _testingManager?.OnMovePlayed();

            Piece visualPiece = Piece.None;

            if (promotionType != PawnPromotion.None)
            {
                bool isWhite = (to >= 56);
                visualPiece = GetPieceFromPromotion(promotionType, isWhite);
            }

            _board.MovePieceVisual(from, to, visualPiece, moveFlag);

            _playerController.ToggleIsWhite();

            //should AI move now
            bool isNewTurnWhite = BitboardScript.GetTurn();
            if (ShouldAiMove(isNewTurnWhite))
            {
                StartCoroutine(PerformAiMove());
            }

            SFXManager.Instance.PlaySound();
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
    }

    private IEnumerator PerformAiMove()
    {
        //wait 1 frame so UI updates before searching
        yield return null;

        //perform search
        bool whiteToMove = BitboardScript.GetTurn();

        IChessEngine engine = whiteToMove ? (IChessEngine)_whiteEngine : (IChessEngine)_blackEngine;
        int depth = whiteToMove ? _whiteDepth : _blackDepth;

        Move bestMove = engine.FindBestMove(depth);


        //is valid move found
        if (bestMove.StartingPos != bestMove.EndingPos) 
        {
            //promotion handling
            PawnPromotion promo = PawnPromotion.None;
            if (bestMove.IsPromotion)
            {
                //map moveflag to pawnpromotion enum
                if (bestMove.PromotionPieceType == Piece.WhiteQueen) promo = PawnPromotion.PromoteQueen;
                else if (bestMove.PromotionPieceType == Piece.WhiteRook) promo = PawnPromotion.PromoteRook;
                else if (bestMove.PromotionPieceType == Piece.WhiteBishop) promo = PawnPromotion.PromoteBishop;
                else if (bestMove.PromotionPieceType == Piece.WhiteKnight) promo = PawnPromotion.PromoteKnight;
            }

            //do move
            FinalizeMove(bestMove.StartingPos, bestMove.EndingPos, promo);
        }
    }
    private bool ShouldAiMove(bool isWhiteTurn)
    {
        if (_aiSide == AiSide.None) return false;
        if (_aiSide == AiSide.White && isWhiteTurn) return true;
        if (_aiSide == AiSide.Black && !isWhiteTurn) return true;
        if (_aiSide == AiSide.Both) return true;
        return false;
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

        BitboardScript.FENtoBitboards("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        _board.CreateBoard();
        _board.DisplayPieces(BitboardScript);

        //if AI plays white make the move
        if (ShouldAiMove(true))
        {
            StartCoroutine(PerformAiMove());
        }
    }
}