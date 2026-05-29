using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "ExplorationRule", menuName = "Neuro/Evaluation/Rules/Exploration")]
    public class ExplorationRule : EvaluationRule
    {
        public float cellSize = 1f;
        public float newCellReward = 5f;
        public float revisitPenaltyPerSecond = 0.1f;
        public override EvaluationRuleRuntime CreateRuntime() => new Runtime(this);
        private sealed class Runtime : EvaluationRuleRuntime
        {
            private readonly ExplorationRule explorationRule;
            public Runtime(ExplorationRule rule) : base(rule) { explorationRule = rule; }
            public override void Initialize(CreatureEvaluationContext context)
            {
                context.Exploration.Configure(explorationRule.cellSize);
                context.Exploration.Reset(context.CurrentPosition);
            }
            public override float Tick(CreatureEvaluationContext context)
            {
                if (explorationRule == null || !explorationRule.enabled) return 0f;
                context.Exploration.Configure(explorationRule.cellSize);
                bool isNewCell = context.Exploration.Sample(context.CurrentPosition);
                float score = isNewCell ? explorationRule.newCellReward : -explorationRule.revisitPenaltyPerSecond * context.DeltaTime;
                return score * explorationRule.weight * explorationRule.rewardMultiplier;
            }
        }
    }
}
