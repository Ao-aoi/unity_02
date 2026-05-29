using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "HorizontalMovementRule", menuName = "Neuro/Evaluation/Rules/Horizontal Movement")]
    public class HorizontalMovementRule : EvaluationRule
    {
        public float rewardPerDistanceSecond = 1f;
        public override float Evaluate(CreatureEvaluationContext context)
        {
            float distanceX = Mathf.Abs(context.CurrentPosition.x - context.SpawnPosition.x);
            return distanceX * rewardPerDistanceSecond * context.DeltaTime;
        }
    }
}
