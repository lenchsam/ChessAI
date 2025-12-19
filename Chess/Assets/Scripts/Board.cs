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

    private GameObject[,] _pieceObjects = new GameObject[8, 8];
    private GameObject[,] _squareObjects = new GameObject[8, 8];

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
                _pieceObjects[x, y] = instance;
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

    public GameObject GetPieceFromPosition(Vector2Int pos)
    {
        return _pieceObjects[pos.x, pos.y];
    }

    public void MovePieceVisual(Vector2Int oldCords, Vector2Int newCords)
    {
        GameObject pieceToMove = _pieceObjects[oldCords.x, oldCords.y];

        //if they didnt move the piece
        if (oldCords == newCords)
        {
            pieceToMove.transform.position = new Vector3(oldCords.x, oldCords.y, -1);
            return;
        }

        pieceToMove.transform.position = new Vector3(newCords.x, newCords.y, -1);

        if (_pieceObjects[newCords.x, newCords.y] != null)
        {
            Destroy(_pieceObjects[newCords.x, newCords.y]);
        }

        _pieceObjects[newCords.x, newCords.y] = _pieceObjects[oldCords.x, oldCords.y];
        _pieceObjects[oldCords.x, oldCords.y] = null;
    }

    private void ClearHighlights()
    {
        foreach (var h in _activeHighlights)
            Destroy(h);

        _activeHighlights.Clear();
    }
    public void ShowBitboardOverlay(ulong bb, Color colour)
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

            highlight.GetComponent<SpriteRenderer>().color = colour;
            _activeHighlights.Add(highlight);
        }
    }

}
