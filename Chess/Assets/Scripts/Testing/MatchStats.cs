using UnityEngine;

public class MatchStats : MonoBehaviour
{
    public int GamesPlayed;

    public int WhiteWins;
    public int BlackWins;
    public int Draws;

    public int TotalPlies;

    public long TotalNodesSearched;

    //a ply is a single move by one player
    public float AveragePlies =>
        GamesPlayed == 0 ? 0 : (float)TotalPlies / GamesPlayed;

    public float AverageNodesPerGame =>
        GamesPlayed == 0 ? 0 : (float)TotalNodesSearched / GamesPlayed;

    public void Reset()
    {
        GamesPlayed = 0;
        WhiteWins = 0;
        BlackWins = 0;
        Draws = 0;
        TotalPlies = 0;
        TotalNodesSearched = 0;
    }
}
