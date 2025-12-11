using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private BoardSettings _boardSettings;
    [SerializeField] private GameObject _squarePrefab;

    Dictionary<char, GameObject> _piecePrefabDictionary;

    private char[,] board = new char[8, 8];
    private GameObject[,] _pieceObjects = new GameObject[8, 8];

    [Header("Piece Prefabs")]
    [SerializeField] GameObject _whitePawnPrefab;
    [SerializeField] GameObject _whiteRookPrefab;
    [SerializeField] GameObject _whiteKnightPrefab;
    [SerializeField] GameObject _whiteBishopPrefab;
    [SerializeField] GameObject _whiteKingPrefab;
    [SerializeField] GameObject _whiteQueenPrefab;

    [Space(5)]

    [SerializeField] GameObject _blackPawnPrefab;
    [SerializeField] GameObject _blackRookPrefab;
    [SerializeField] GameObject _blackKnightPrefab;
    [SerializeField] GameObject _blackBishopPrefab;
    [SerializeField] GameObject _blackKingPrefab;
    [SerializeField] GameObject _blackQueenPrefab;

    private GameObject _boardParent;
    private GameObject _piecesParent;
    private void Start()
    {
        _boardParent = new GameObject();
        _boardParent.name = "Board Squares";

        _piecesParent = new GameObject();
        _piecesParent.name = "Pieces";

        CreateBoard();

        //initialize piece dictionary
        _piecePrefabDictionary = new Dictionary<char, GameObject>()
        {
            {'P', _whitePawnPrefab},
            {'R', _whiteRookPrefab},
            {'N', _whiteKnightPrefab},
            {'B', _whiteBishopPrefab},
            {'Q', _whiteQueenPrefab},
            {'K', _whiteKingPrefab},

            {'p', _blackPawnPrefab},
            {'r', _blackRookPrefab},
            {'n', _blackKnightPrefab},
            {'b', _blackBishopPrefab},
            {'q', _blackQueenPrefab},
            {'k', _blackKingPrefab},
        };

        //uppercase = white lowercase = black
        FENtoPiecePositions("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");
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

    void DrawSquare(Color colour, Vector2 position)
    {
        Instantiate(_squarePrefab, position, Quaternion.identity, _boardParent.transform).GetComponent<SpriteRenderer>().color = colour;
    }

    void FENtoPiecePositions(string FEN)
    {
        int row = 7;
        int col = 0;

        foreach (char c in FEN)
        {
            if (char.IsDigit(c))
            {
                int emptySquares = int.Parse(c.ToString());
                col += emptySquares;
            }
            else if (c == '/')
            {
                row--;
                col = 0;
            }
            else
            {
                Vector2 position = new Vector2(col, row);

                board[col, row] = c;

                GameObject piece = Instantiate(CharToPiece(c), position, Quaternion.identity, _piecesParent.transform);

                _pieceObjects[col, row] = piece;

                piece.transform.position = new Vector3(col, row, -1);

                col++;
            }
        }
    }
    GameObject CharToPiece(char c)
    {
        return _piecePrefabDictionary[c];
    }

    public char GetPieceAt(int x, int y)
    {
        if (board[x, y] == '\0')
        {
            return '\0';
        }
        return board[x, y];
    }

    public GameObject GetPieceFromPosition(Vector2Int pos)
    {
        return _pieceObjects[pos.x, pos.y];
    }

    public void MovePiece(Vector2Int oldPos, Vector2Int newPos, GameObject pieceObj)
    {
        //remove any captured piece
        if (board[newPos.x, newPos.y] != '\0')
        {
            //if same colour then cannot capture
            //TODO: change this to check move is valid not just colour
            if (_pieceObjects[oldPos.x, oldPos.y].tag == _pieceObjects[newPos.x, newPos.y].tag)
            {
                pieceObj.transform.position = new Vector3 (oldPos.x, oldPos.y, -1);
                Debug.Log("Cannot capture your own piece!");
                return;
            }
            else
            {
                GameObject capturedPiece = _pieceObjects[newPos.x, newPos.y];
                Destroy(capturedPiece);
            }
        }

        //add piece to new position in board array
        board[newPos.x, newPos.y] = board[oldPos.x, oldPos.y];
        _pieceObjects[newPos.x, newPos.y] = pieceObj;

        //remove piece from old position in board array
        board[oldPos.x, oldPos.y] = '\0';
        _pieceObjects[oldPos.x, oldPos.y] = null;

        //snap piece to new position grid
        pieceObj.transform.position = new Vector3(newPos.x, newPos.y, -1);
    }
}
