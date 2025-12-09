using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private BoardSettings boardSettings;
    [SerializeField] private GameObject squarePrefab;

    private void Start()
    {
        CreateBoard();
    }
    void CreateBoard()
    {
        Color whiteMaterial = boardSettings.whiteColor;
        Color blackMaterial = boardSettings.blackColor;
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
        Instantiate(squarePrefab, position, Quaternion.identity).GetComponent<SpriteRenderer>().color = colour;
    }
}
