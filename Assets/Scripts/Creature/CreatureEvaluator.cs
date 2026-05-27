using UnityEngine;
using System.Collections.Generic;

public class CreatureEvaluator : MonoBehaviour
{
    [Header("評価スコア（これが高いほど次世代に遺伝しやすい）")]
    public float totalFitness = 0f;

    [Header("グリッド探索の設定")]
    public float gridSize = 2.0f;

    [Header("参照")]
    [SerializeField] private CreatureSensor sensor; // 参照する目のコンポーネント
    private float previousDistanceToFood = Mathf.Infinity;
    private GameObject currentTrackedFood = null;

    // 踏破したマス目を記録するハッシュセット
    private HashSet<Vector2Int> exploredGrid = new HashSet<Vector2Int>();

    void Update()
    {
        // 1. センサーのデータを使って、エサへの接近報酬を計算
        EvaluateDistanceReward();

        // 2. 自分の位置を使って、空間の探索報酬を計算
        EvaluateExplorationReward();
    }

    // 🔴 報酬①＆②：近づくとプラス、遠ざかるとマイナス
    void EvaluateDistanceReward()
    {
        if (sensor == null) return;

        // センサーが今まさにロックオンしているエサと、その距離を取得
        GameObject targetFood = sensor.ClosestFood;
        // 実際の距離（メートル単位）をセンサーのデータから逆算（正規化を戻す）
        float currentDistance = sensor.DistanceToClosestFood * sensor.sightRange;

        if (targetFood != null)
        {
            if (currentTrackedFood == targetFood && previousDistanceToFood != Mathf.Infinity)
            {
                float deltaDistance = previousDistanceToFood - currentDistance;

                if (deltaDistance > 0)
                {
                    totalFitness += deltaDistance * 10f; // 近づいた！
                }
                else if (deltaDistance < 0)
                {
                    totalFitness += deltaDistance * 5f;  // 遠ざかった（ペナルティ）
                }
            }

            currentTrackedFood = targetFood;
            previousDistanceToFood = currentDistance;
        }
        else
        {
            // エサを見失ったら履歴をリセット
            previousDistanceToFood = Mathf.Infinity;
            currentTrackedFood = null;
        }
    }

    // 🟢 報酬③：広い範囲を歩き回ったら報酬
    void EvaluateExplorationReward()
    {
        int gridX = Mathf.RoundToInt(transform.position.x / gridSize);
        int gridY = Mathf.RoundToInt(transform.position.y / gridSize);
        Vector2Int currentGridPos = new Vector2Int(gridX, gridY);

        if (!exploredGrid.Contains(currentGridPos))
        {
            exploredGrid.Add(currentGridPos);
            totalFitness += 15f; // 新エリア開拓ボーナス！
            // Debug.Log($"新エリア開拓！ 総スコア: {totalFitness}");
        }
    }
}