using UnityEngine;
using System.Collections.Generic;

namespace Neuro.Creature
{
    public class CreatureEvaluator : MonoBehaviour
    {
        [Header("評価スコア")]
        public float totalFitness = 0f;

        // 💡 マネージャーから渡される「この個体の評価ルール」
        public EvaluationCriteria currentCriteria;

        [Header("状態トラッキング")]
        private Vector3 spawnPosition;
        private float previousDistanceToFood = Mathf.Infinity;
        private GameObject currentTrackedFood = null;
        private Rigidbody2D bodyRb;

        void Start()
        {
            spawnPosition = transform.position;
            bodyRb = GetComponent<Rigidbody2D>();
            if (bodyRb == null) bodyRb = GetComponentInChildren<Rigidbody2D>();
            
            // ルールが渡されていなければ、デフォルトのルールを作成
            if (currentCriteria == null) currentCriteria = new EvaluationCriteria();
        }

        void Update()
        {
            if (currentCriteria == null) return;

            // 1. 生存ボーナス
            totalFitness += currentCriteria.survivalRewardPerSec * Time.deltaTime;

            // 2. エサ探索の評価
            if (currentCriteria.approachFoodReward > 0 || currentCriteria.escapeFoodPenalty > 0)
            {
                EvaluateFoodDistance();
            }

            // 3. スピード（横移動）の評価
            if (currentCriteria.horizontalMoveReward > 0)
            {
                // 生まれた場所からのX軸の距離をスコアにする（遠くに行くほど高い）
                float distanceX = Mathf.Abs(transform.position.x - spawnPosition.x);
                totalFitness += distanceX * currentCriteria.horizontalMoveReward * Time.deltaTime;
            }

            // 4. ジャンプ（高さと滞空）の評価
            if (currentCriteria.heightReward > 0)
            {
                // 生まれた場所より上にいる場合、その高さをスコアにする
                float heightY = Mathf.Max(0, transform.position.y - spawnPosition.y);
                totalFitness += heightY * currentCriteria.heightReward * Time.deltaTime;
            }
            
            if (currentCriteria.airTimeReward > 0 && bodyRb != null)
            {
                // Y軸の速度がプラス（上昇中）か、下に落ちている最中なら滞空とみなす
                if (Mathf.Abs(bodyRb.linearVelocity.y) > 0.1f) 
                {
                    totalFitness += currentCriteria.airTimeReward * Time.deltaTime;
                }
            }
        }

        void EvaluateFoodDistance()
        {
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
                if (currentTrackedFood == closestFood && previousDistanceToFood != Mathf.Infinity)
                {
                    float deltaDistance = previousDistanceToFood - closestDistance;

                    if (deltaDistance > 0)
                    {
                        totalFitness += deltaDistance * currentCriteria.approachFoodReward; 
                    }
                    else if (deltaDistance < 0)
                    {
                        totalFitness += deltaDistance * currentCriteria.escapeFoodPenalty; 
                    }
                }
                currentTrackedFood = closestFood;
                previousDistanceToFood = closestDistance;
            }
        }
    }
}