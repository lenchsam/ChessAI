using System;
using System.Collections.Generic;
using UnityEngine;
public enum Piece
{
    WhitePawn = 0,
    WhiteKnight = 1, 
    WhiteBishop = 2, 
    WhiteRook = 3,
    WhiteQueen = 4, 
    WhiteKing = 5,
    BlackPawn = 6, 
    BlackKnight = 7, 
    BlackBishop = 8,
    BlackRook = 9, 
    BlackQueen = 10, 
    BlackKing = 11,
    None = 12
}
public class Board : MonoBehaviour
{
    [SerializeField] private BoardSettings _boardSettings;
    [SerializeField] private GameObject _squarePrefab;

    private GameObject[] _pieceObjects = new GameObject[64];
    private GameObject[] _squareObjects = new GameObject[64];

    [Header("Piece Prefabs")]
    [SerializeField] GameObject[] _piecePrefabs = new GameObject[12];

    private GameObject _boardParent;
    private GameObject _piecesParent;

    //bitboard overlay
    [SerializeField] private GameObject _highlightPrefab;
    private List<GameObject> _activeHighlights = new List<GameObject>();
    private void Awake()
    {
        _boardParent = new GameObject();
        _boardParent.name = "Board Squares";

        _piecesParent = new GameObject();
        _piecesParent.name = "Pieces";
    }
    public void CreateBoard()
    {
        Color whiteMaterial = _boardSettings.whiteColor;
        Color blackMaterial = _boardSettings.blackColor;
        for (int x = 0; x < 8; x++)
        {
            for(int y = 0; y < 8; y++)
            {
                bool isWhite = (x + y) % 2 != 0;

                Color squareColor;
                if (isWhite)
                {
                    squareColor = whiteMaterial;
                }
                else
                {
                    squareColor = blackMaterial;
                }
                Vector2 position = new Vector2(x, y);

                DrawSquare(squareColor, position);
            }
        }
    }
    public void DisplayPieces(Bitboards bitboards)
    {
        for(int i = 0; i < 64; i++)
        {
            Piece piece = bitboards.GetPieceOnSquare(i);
            if (piece != Piece.None)
            {
                int x = i % 8;
                int y = i / 8;
                Vector2 position = new Vector2(x, y);
                GameObject piecePrefab = GetPiecePrefab(piece);
                GameObject instance = Instantiate(piecePrefab, position, Quaternion.identity, _piecesParent.transform);
                _pieceObjects[i] = instance;
            }
        }
    }

    private GameObject GetPiecePrefab(Piece piece)
    {
        return _piecePrefabs[(int)piece];
    }

    void DrawSquare(Color colour, Vector2 position)
    {
        Instantiate(_squarePrefab, position, Quaternion.identity, _boardParent.transform).GetComponent<SpriteRenderer>().color = colour;
    }

    public GameObject GetPieceFromPosition(int pos)
    {
        return _pieceObjects[pos];
    }

    public void MovePieceVisual(int oldSquare, int newSquare)
    {
        GameObject pieceToMove = _pieceObjects[oldSquare];

        int oldX = oldSquare % 8;
        int oldY = oldSquare / 8;

        int newX = newSquare % 8;
        int newY = newSquare / 8;
        //if they didnt move the piece
        if (oldSquare == newSquare)
        {
            pieceToMove.transform.position = new Vector3(oldX, oldY, -1);
            return;
        }

        pieceToMove.transform.position = new Vector3(newX, newY, -1);

        if (_pieceObjects[newSquare] != null)
        {
            Destroy(_pieceObjects[newSquare]);
        }

        _pieceObjects[newSquare] = _pieceObjects[oldSquare];
        _pieceObjects[oldSquare] = null;
    }

    private void ClearHighlights()
    {
        foreach (var h in _activeHighlights)
            Destroy(h);

        _activeHighlights.Clear();
    }
    public void ShowBitboardOverlay(ulong bb)
    {
        ClearHighlights();

        for (int square = 0; square < 64; square++)
        {
            if (((bb >> square) & 1UL) == 0UL)
                continue;

            int x = square % 8;
            int y = square / 8;

            GameObject highlight = Instantiate(
                _highlightPrefab,
                new Vector3(x, y, -0.5f),
                Quaternion.identity,
                transform
            );

            _activeHighlights.Add(highlight);
        }
    }
}
