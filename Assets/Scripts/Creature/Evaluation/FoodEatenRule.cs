using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "FoodEatenRule", menuName = "Neuro/Evaluation/Rules/Food Eaten")]
    public class FoodEatenRule : EvaluationRule
    {
        [Header("設定")]
        public float pointsPerSatiety = 1f; // 満腹値1あたりの得点

        public override EvaluationRuleRuntime CreateRuntime() => new Runtime(this);

        private sealed class Runtime : EvaluationRuleRuntime
        {
            private readonly FoodEatenRule foodRule;
            private float previousSatiety = 0f;

            public Runtime(FoodEatenRule rule) : base(rule) { this.foodRule = rule; }

            public override void Initialize(CreatureEvaluationContext context)
            {
                previousSatiety = context.Agent != null ? context.Agent.CurrentSatiety : 0f;
            }

            public override float Tick(CreatureEvaluationContext context)
            {
                if (foodRule == null || !foodRule.enabled) return 0f;
                if (context.Agent == null) return 0f;

                float current = context.Agent.CurrentSatiety;
                float delta = current - previousSatiety;
                previousSatiety = current;

                if (delta <= 0f) return 0f;

                float score = delta * foodRule.pointsPerSatiety;
                return score * foodRule.weight * foodRule.rewardMultiplier;
            }
        }
    }
}
