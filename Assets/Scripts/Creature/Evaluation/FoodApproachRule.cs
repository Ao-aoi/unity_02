using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "FoodApproachRule", menuName = "Neuro/Evaluation/Rules/Food Approach")]
    public class FoodApproachRule : EvaluationRule
    {
        public float approachFoodReward = 20f;
        public float escapeFoodPenalty = 2f;
        public override EvaluationRuleRuntime CreateRuntime() => new Runtime(this);
        private sealed class Runtime : EvaluationRuleRuntime
        {
            private readonly FoodApproachRule foodRule;
            private FoodItem currentTrackedFood;
            private float previousDistanceToFood = Mathf.Infinity;
            public Runtime(FoodApproachRule rule) : base(rule) { foodRule = rule; }
            public override void Initialize(CreatureEvaluationContext context) { currentTrackedFood = null; previousDistanceToFood = Mathf.Infinity; }
            public override float Tick(CreatureEvaluationContext context)
            {
                if (foodRule == null || !foodRule.enabled) return 0f;
                FoodItem closestFood = null;
                float closestDistance = Mathf.Infinity;
                var foods = context.Foods;
                for (int i = 0; i < foods.Count; i++)
                {
                    FoodItem food = foods[i];
                    if (food == null) continue;
                    float distance = Vector3.Distance(food.transform.position, context.CurrentPosition);
                    if (distance < closestDistance) { closestDistance = distance; closestFood = food; }
                }
                float score = 0f;
                if (closestFood != null)
                {
                    if (currentTrackedFood == closestFood && previousDistanceToFood != Mathf.Infinity)
                    {
                        float deltaDistance = previousDistanceToFood - closestDistance;
                        if (deltaDistance > 0f) score += deltaDistance * foodRule.approachFoodReward;
                        else if (deltaDistance < 0f) score += deltaDistance * foodRule.escapeFoodPenalty;
                    }
                    currentTrackedFood = closestFood;
                    previousDistanceToFood = closestDistance;
                }
                return score * foodRule.weight * foodRule.rewardMultiplier;
            }
        }
    }
}
