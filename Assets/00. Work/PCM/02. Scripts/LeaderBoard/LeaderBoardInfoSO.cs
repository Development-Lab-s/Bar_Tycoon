using UnityEngine;

[CreateAssetMenu(fileName = "LeaderBoardInfo", menuName = "Scriptable Objects/LeaderBoardInfo")]
public class LeaderBoardInfoSO : ScriptableObject
{
    public string playerName;
    public double highScore;
    public int myRank;
    public void UpdateData(string name, double score, int rank)
    {
        playerName = name;
        highScore = score;
        myRank = rank;
    }
}
