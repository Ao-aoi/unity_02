using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "MovementEfficiencyRule", menuName = "Neuro/Evaluation/Rules/Movement Efficiency")]
    public class MovementEfficiencyRule : EvaluationRule
    {
        public float minimumEnergy = 0.01f;
        public float rewardPerEfficientDistance = 1f;
        public override float Evaluate(CreatureEvaluationContext context)
        {
            float efficiency = context.Movement.DeltaDistance / Mathf.Max(minimumEnergy, context.Movement.DeltaEnergyConsumed);
            return efficiency * rewardPerEfficientDistance * context.DeltaTime;
        }
    }
}
