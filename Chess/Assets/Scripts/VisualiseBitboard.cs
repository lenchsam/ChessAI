using System.Collections.Generic;
using UnityEngine;

public class VisualiseBitboard : MonoBehaviour
{
    [SerializeField] private GameObject _highlightPrefab;
    private GameObject[] HighlightGameObjects = new GameObject[64];
    private List<GameObject> _activeHighlights = new List<GameObject>();

    [SerializeField] GameManager _gameManager;
    private Bitboards _bitboard;

    private void Start()
    {
        InstantiateSquares();
        _gameManager.BitboardScript.MovingPieceEvent.AddListener(ShowBitboardOverlay);
    }
    public void SetHighlightColour(Color highlightColour)
    {
        _highlightPrefab.GetComponent<SpriteRenderer>().color = highlightColour;
    }
    private void InstantiateSquares()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject highlight = Instantiate(
                    _highlightPrefab,
                    new Vector3(x, y, -0.5f),
                    Quaternion.identity,
                    transform
                );
                highlight.SetActive(false);

                HighlightGameObjects[(y * 8) + x] = highlight;
            }
        }
    }
    public void DisplayPieceBitboard(int piece)
    {
        ulong bitboard = _gameManager.BitboardScript.GetBitboard((Piece)piece);
        ShowBitboardOverlay(bitboard);
    }
    private void ClearHighlights()
    {
        foreach (GameObject square in _activeHighlights)
            square.SetActive(false);

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
