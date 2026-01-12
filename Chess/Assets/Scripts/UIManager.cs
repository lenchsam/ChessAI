using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;

    [Header("UI")]
    [SerializeField] private GameObject _bitboardSwitcher;
    [SerializeField] private GameObject _gameOverBackground;
    [SerializeField] private TextMeshProUGUI _endingText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _gameManager.BitboardScript.GameEnded.AddListener(GameEnded);
    }

    void GameEnded(GameState state, bool isWhiteTurn)
    {
        DisableGameUI();
        EnableGameOverUI(state, isWhiteTurn);
    }

    private void DisableGameUI()
    {
        _bitboardSwitcher.SetActive(false);
    }

    public void DisableGameOverUI()
    {
        _gameOverBackground.SetActive(false);
    }

    private void EnableGameOverUI(GameState state, bool isWhiteTurn)
    {
        string winner = isWhiteTurn ? "White" : "Black";
        _gameOverBackground.SetActive(true);

        switch (state)
        {
            case GameState.Checkmate:
                _endingText.text = "Checkmate: " + winner + " Wins";
                break;
            case GameState.Stalemate:
                _endingText.text = "Stalemate: Draw";
                break;
        }
    }
}
