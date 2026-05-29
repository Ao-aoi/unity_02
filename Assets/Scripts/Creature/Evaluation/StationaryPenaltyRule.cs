using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "StationaryPenaltyRule", menuName = "Neuro/Evaluation/Rules/Stationary Penalty")]
    public class StationaryPenaltyRule : EvaluationRule
    {
        public float penaltyPerSecond = 1f;
        public float thresholdSeconds = 1f;
        public float movementEpsilon = 0.01f;
        public override EvaluationRuleRuntime CreateRuntime() => new Runtime(this);
        private sealed class Runtime : EvaluationRuleRuntime
        {
            private readonly StationaryPenaltyRule stationaryRule;
            private float stationaryTimer;
            public Runtime(StationaryPenaltyRule rule) : base(rule) { stationaryRule = rule; }
            public override void Initialize(CreatureEvaluationContext context) { stationaryTimer = 0f; }
            public override float Tick(CreatureEvaluationContext context)
            {
                if (stationaryRule == null || !stationaryRule.enabled) return 0f;
                if (context.Movement.DeltaDistance > stationaryRule.movementEpsilon) { stationaryTimer = 0f; return 0f; }
                stationaryTimer += context.DeltaTime;
                if (stationaryTimer > stationaryRule.thresholdSeconds)
                    return -stationaryRule.penaltyPerSecond * context.DeltaTime * stationaryRule.weight * stationaryRule.rewardMultiplier;
                return 0f;
            }
        }
    }
}
