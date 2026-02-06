using UnityEngine;

public class TestingManager : MonoBehaviour
{
    [SerializeField] GameManager gameManager;

    [SerializeField] int gamesToRun = 200;

    MatchStats stats = new MatchStats();

    private int currentPlies;

    void Start()
    {
        gameManager.BitboardScript.GameEnded.AddListener(OnGameEnded);
    }

    public void BeginTest()
    {
        stats.Reset();
        currentPlies = 0;

        Debug.Log("Starting engine test");

        gameManager.RestartGame();
    }

    public void OnMovePlayed()
    {
        currentPlies++;
    }

    private void OnGameEnded(EndingState state, bool whiteWon)
    {
        stats.GamesPlayed++;
        stats.TotalPlies += currentPlies;

        if (state == EndingState.Checkmate)
        {
            if (whiteWon) stats.WhiteWins++;
            else stats.BlackWins++;
        }
        else
        {
            stats.Draws++;
        }

        //ply is a single move by one player
        Debug.Log($"Game {stats.GamesPlayed} finished | " + $"W:{stats.WhiteWins} B:{stats.BlackWins} D:{stats.Draws} | " + $"Avg plies: {stats.AveragePlies:F1}");

        currentPlies = 0;

        if (stats.GamesPlayed < gamesToRun)
            gameManager.RestartGame();
        else
            PrintFinalReport();
    }

    void PrintFinalReport()
    {
        Debug.Log($"FINAL RESULTS {stats.GamesPlayed} finished | " + $"W:{stats.WhiteWins} B:{stats.BlackWins} D:{stats.Draws} | " + $"Avg plies: {stats.AveragePlies:F1}");
    }
}
