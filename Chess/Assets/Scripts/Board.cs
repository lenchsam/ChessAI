using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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

    [SerializeField] private GameObject _squarePrefab;

    private GameObject[] _pieceObjects = new GameObject[64];

    [Header("Piece Prefabs")]
    [SerializeField] GameObject[] _piecePrefabs = new GameObject[12];

    private GameObject _boardParent;
    private GameObject _piecesParent;

    Color _whiteMaterial;
    Color _blackMaterial;

    private void Awake()
    {
        _boardParent = new GameObject();
        _boardParent.name = "Board Squares";

        _piecesParent = new GameObject();
        _piecesParent.name = "Pieces";
    }
    public void SetBoardColour(Color whiteColour, Color blackColour)
    {
        _whiteMaterial = whiteColour;
        _blackMaterial = blackColour;
    }
    public void CreateBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            for(int y = 0; y < 8; y++)
            {
                bool isWhite = (x + y) % 2 != 0;

                Color squareColor;
                if (isWhite)
                {
                    squareColor = _whiteMaterial;
                }
                else
                {
                    squareColor = _blackMaterial;
                }
                Vector2 position = new Vector2(x, y);

                DrawSquare(squareColor, position);
            }
        }
    }

    public void ClearPieces()
    {
        foreach (Transform childTransform in _piecesParent.transform)
        {
            Destroy(childTransform.gameObject);
        }
    }
    public void DisplayPieces(Bitboards bitboards)
    {
        ClearPieces();
        for (int i = 0; i < 64; i++)
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


}
