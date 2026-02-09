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

    void GameEnded(EndingState state, bool isWhiteTurn)
    {
        DisableGameUI();
        EnableGameOverUI(state, isWhiteTurn);
    }
    
    private void DisableGameUI()
    {
        //_bitboardSwitcher.SetActive(false);
    }

    //called on game restart
    public void DisableGameOverUI()
    {
        _gameOverBackground.SetActive(false);
    }
    public void EnableGameUI()
    {
        //_bitboardSwitcher.SetActive(true);
    }


    private void EnableGameOverUI(EndingState state, bool isWhiteTurn)
    {
        string winner = isWhiteTurn ? "White" : "Black";
        _gameOverBackground.SetActive(true);

        switch (state)
        {
            case EndingState.Checkmate:
                _endingText.text = "Checkmate: " + winner + " Wins";
                break;
            case EndingState.Stalemate:
                _endingText.text = "Stalemate: Draw";
                break;
        }
    }
}
