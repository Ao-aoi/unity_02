using UnityEngine;
using System.Collections.Generic;

public class CreatureEvaluator : MonoBehaviour
{
    [Header("評価スコア")]
    public float totalFitness = 0f;

    [Header("グリッド探索の設定")]
    public float gridSize = 2.0f;
    private HashSet<Vector2Int> exploredGrid = new HashSet<Vector2Int>();

    private float previousDistanceToFood = Mathf.Infinity;
    private GameObject currentTrackedFood = null;

    void Start()
    {
        // 最初のマス目を開拓済みにする（これによって、生まれた瞬間の無条件+15を防ぐ）
        RecordGridPosition();
    }

    void Update()
    {
        // 1. 生きているだけでエラい！ボーナス（1秒間に +1 のスコア）
        totalFitness += Time.deltaTime;

        // 2. 距離による報酬（視界に入っていなくても、一番近いエサへの純粋な距離で測る）
        EvaluateDistanceReward();

        // 3. 空間の探索報酬
        EvaluateExplorationReward();
    }

    void EvaluateDistanceReward()
    {
        // 視界（センサー）関係なく、世界中のエサから一番近いものを探す（匂いのような感覚）
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
        GameObject closestFood = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject food in foods)
        {
            float dist = Vector3.Distance(food.transform.position, transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestFood = food;
            }
        }

        if (closestFood != null)
        {
            // ターゲットが変わっていなければ距離の差分を計算
            if (currentTrackedFood == closestFood && previousDistanceToFood != Mathf.Infinity)
            {
                float deltaDistance = previousDistanceToFood - closestDistance;

                if (deltaDistance > 0)
                {
                    // 近づいた！（+報酬を大きめに）
                    totalFitness += deltaDistance * 20f; 
                }
                else if (deltaDistance < 0)
                {
                    // 遠ざかった！（ペナルティは控えめにして、動くのを怖がらせないようにする）
                    totalFitness += deltaDistance * 2f; 
                }
            }
            currentTrackedFood = closestFood;
            previousDistanceToFood = closestDistance;
        }
    }

    void EvaluateExplorationReward()
    {
        RecordGridPosition();
    }

    void RecordGridPosition()
    {
        int gridX = Mathf.RoundToInt(transform.position.x / gridSize);
        int gridY = Mathf.RoundToInt(transform.position.y / gridSize);
        Vector2Int currentGridPos = new Vector2Int(gridX, gridY);

        if (!exploredGrid.Contains(currentGridPos))
        {
            exploredGrid.Add(currentGridPos);
            totalFitness += 15f; // 動いて新エリアに行った時だけもらえる
        }
    }
}