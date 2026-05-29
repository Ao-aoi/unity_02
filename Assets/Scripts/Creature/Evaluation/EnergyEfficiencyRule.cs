using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "EnergyEfficiencyRule", menuName = "Neuro/Evaluation/Rules/Energy Efficiency")]
    public class EnergyEfficiencyRule : EvaluationRule
    {
        public float minimumHpConsumption = 0.01f;
        public float rewardPerHpEfficientDistance = 1f;
        public override float Evaluate(CreatureEvaluationContext context)
        {
            float efficiency = context.Movement.DeltaDistance / Mathf.Max(minimumHpConsumption, context.Movement.DeltaHpConsumed);
            return efficiency * rewardPerHpEfficientDistance * context.DeltaTime;
        }
    }
}
