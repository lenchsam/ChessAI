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

    Dictionary<Piece, GameObject> _piecePrefabDictionary;

    private GameObject[,] _pieceObjects = new GameObject[8, 8];
    private GameObject[,] _squareObjects = new GameObject[8, 8];

    [Header("Piece Prefabs")]
    [SerializeField] GameObject[] _piecePrefabs = new GameObject[12];

    private GameObject _boardParent;
    private GameObject _piecesParent;


    private Bitboards _bitboards;
    private void Start()
    {
        _bitboards = new Bitboards();

        //uppercase = white lowercase = black
        _bitboards.FENtoBitboards("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

        _boardParent = new GameObject();
        _boardParent.name = "Board Squares";

        _piecesParent = new GameObject();
        _piecesParent.name = "Pieces";

        CreateBoard();
        DisplayPieces();

    }
    void CreateBoard()
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
    void DisplayPieces()
    {
        for(int i = 0; i < 64; i++)
        {
            Piece piece = _bitboards.GetPieceOnSquare(i);
            if (piece != Piece.None)
            {
                int x = i % 8;
                int y = i / 8;
                Vector2 position = new Vector2(x, y);
                GameObject piecePrefab = GetPiecePrefab(piece);
                Instantiate(piecePrefab, position, Quaternion.identity, _piecesParent.transform);
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

    //public void MovePiece(Vector2Int oldPos, Vector2Int newPos, GameObject pieceObj)
    //{
    //    //remove any captured piece
    //    if (board[newPos.x, newPos.y] != '\0')
    //    {
    //        //if same colour then cannot capture
    //        //TODO: change this to check move is valid not just colour
    //        if (_pieceObjects[oldPos.x, oldPos.y].tag == _pieceObjects[newPos.x, newPos.y].tag)
    //        {
    //            pieceObj.transform.position = new Vector3 (oldPos.x, oldPos.y, -1);
    //            Debug.Log("Cannot capture your own piece!");
    //            return;
    //        }
    //        else
    //        {
    //            GameObject capturedPiece = _pieceObjects[newPos.x, newPos.y];
    //            Destroy(capturedPiece);
    //        }
    //    }

    //    //add piece to new position in board array
    //    board[newPos.x, newPos.y] = board[oldPos.x, oldPos.y];
    //    _pieceObjects[newPos.x, newPos.y] = pieceObj;

    //    //remove piece from old position in board array
    //    board[oldPos.x, oldPos.y] = '\0';
    //    _pieceObjects[oldPos.x, oldPos.y] = null;

    //    //snap piece to new position grid
    //    pieceObj.transform.position = new Vector3(newPos.x, newPos.y, -1);
    //}
}
