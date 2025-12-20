using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Bitboards BitboardScript;
    [SerializeField] private Board _board;
    [SerializeField] private PlayerController _playerController;

    [Range(2053, 2100)]
    [SerializeField] private int blockerIndex;

    ulong[] possiblities;

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
