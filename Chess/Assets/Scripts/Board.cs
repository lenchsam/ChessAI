using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private BoardSettings _boardSettings;
    [SerializeField] private GameObject _squarePrefab;

    Dictionary<char, GameObject> _piecePrefabDictionary;

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
    private void Start()
    {
        _boardParent = new GameObject();
        _boardParent.name = "Board Squares";
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
        FENtoPiecePositions("RNBQKBNR/PPPPPPPP/8/8/8/8/pppppppp/rnbqkbnr");

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
        int row = 0;
        int col = 0;
        int numEmpty = 0;
        foreach (char c in FEN)
        {
            if (char.IsDigit(c))
            {
                //empty squares
                int emptySquares = int.Parse(c.ToString());
                numEmpty += emptySquares;
                col += emptySquares;
            }
            else if (c == '/')
            {
                //new rank
                row++;
                numEmpty = 0;
                col = 0;
            }
            else
            {
                //place pieces
                if (numEmpty > 0)
                {
                    for (int i = 0; i < numEmpty; i++)
                    {
                        // Place empty square at (i + numEmpty, row)
                    }
                    numEmpty = 0;
                }
                else
                {
                    col++;
                    Vector3 position = new Vector3(col - 1, row);
                    Debug.Log($"Placing piece {c} at position {position}");
                   
                    Instantiate(CharToPiece(c), position, Quaternion.identity);

                }
            }
        }
    }
    GameObject CharToPiece(char c)
    {
        return _piecePrefabDictionary[c];
    }
}
