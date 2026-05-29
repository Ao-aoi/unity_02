using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    internal sealed class LegacyCriteriaRuntime
    {
        private readonly EvaluationCriteria criteria;
        private readonly FoodApproachRule foodRule;
        private readonly EvaluationRuleRuntime foodRuntime;
        private float stationaryTimer;

        public LegacyCriteriaRuntime(EvaluationCriteria criteria)
        {
            this.criteria = criteria;
            foodRule = ScriptableObject.CreateInstance<FoodApproachRule>();
            foodRule.enabled = true;
            foodRule.weight = 1f;
            foodRule.rewardMultiplier = 1f;
            foodRule.approachFoodReward = criteria != null ? criteria.approachFoodReward : 0f;
            foodRule.escapeFoodPenalty = criteria != null ? criteria.escapeFoodPenalty : 0f;
            foodRuntime = foodRule.CreateRuntime();
        }

        public void Initialize(CreatureEvaluationContext context)
        {
            stationaryTimer = 0f;
            foodRuntime.Initialize(context);
        }

        public float Tick(CreatureEvaluationContext context)
        {
            if (criteria == null)
                return 0f;

            float score = 0f;
            score += criteria.survivalRewardPerSec * context.DeltaTime;

            if (criteria.approachFoodReward > 0f || criteria.escapeFoodPenalty > 0f)
                score += foodRuntime.Tick(context);

            if (criteria.horizontalMoveReward > 0f)
            {
                float distanceX = Mathf.Abs(context.CurrentPosition.x - context.SpawnPosition.x);
                score += distanceX * criteria.horizontalMoveReward * context.DeltaTime;
            }

            if (criteria.heightReward > 0f)
            {
                float heightY = Mathf.Max(0f, context.CurrentPosition.y - context.SpawnPosition.y);
                score += heightY * criteria.heightReward * context.DeltaTime;
            }

            if (criteria.airTimeReward > 0f && context.Body != null && Mathf.Abs(context.Body.linearVelocity.y) > 0.1f)
                score += criteria.airTimeReward * context.DeltaTime;

            if (criteria.stationaryPenaltyPerSec > 0f)
            {
                if (context.Movement.DeltaDistance > criteria.stationaryMovementEpsilon)
                {
                    stationaryTimer = 0f;
                }
                else
                {
                    stationaryTimer += context.DeltaTime;
                    if (stationaryTimer > criteria.stationaryThresholdSeconds)
                        score -= criteria.stationaryPenaltyPerSec * context.DeltaTime;
                }
            }

            return score;
        }
    }
}
