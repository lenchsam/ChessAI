using UnityEngine;

public class Evaluation
{
    private const int _pawnValue = 100;
    private const int _knightValue = 300;
    private const int _bishopValue = 320;
    private const int _rookValue = 500;
    private const int _queenValue = 900;

    public struct EvaluationData
    {
        public int MaterialScore;
        //other data needed for an evaluation
    }
    public float Evaluate(Bitboards bitboard)
    {
        EvaluationData whiteEvalData = new EvaluationData();
        EvaluationData blackEvalData = new EvaluationData();

        //get evaluation of piece amounts
        Material whiteMaterial = bitboard.GetMaterialForColour(true);
        Material blackMaterial = bitboard.GetMaterialForColour(false);
        whiteEvalData.MaterialScore = whiteMaterial.SumOfMaterialNumbers(_pawnValue, _knightValue, _queenValue, _bishopValue, _rookValue);
        blackEvalData.MaterialScore = blackMaterial.SumOfMaterialNumbers(_pawnValue, _knightValue, _queenValue, _bishopValue, _rookValue);

        int mobility = bitboard.GetTurn() ? 1 : -1;
        int evaluation = whiteEvalData.MaterialScore - blackEvalData.MaterialScore;
        Debug.Log(evaluation * mobility);
        return evaluation * mobility;
    }
}
