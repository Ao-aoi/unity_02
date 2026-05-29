using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "SurvivalRule", menuName = "Neuro/Evaluation/Rules/Survival")]
    public class SurvivalRule : EvaluationRule
    {
        public float rewardPerSecond = 1f;
        public override float Evaluate(CreatureEvaluationContext context) => rewardPerSecond * context.DeltaTime;
    }
}
