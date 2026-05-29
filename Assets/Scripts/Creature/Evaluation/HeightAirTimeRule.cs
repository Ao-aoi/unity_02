using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "HeightAirTimeRule", menuName = "Neuro/Evaluation/Rules/Height And Air Time")]
    public class HeightAirTimeRule : EvaluationRule
    {
        public float heightReward = 1f;
        public float airTimeReward = 0f;
        public float airVelocityThreshold = 0.1f;
        public override float Evaluate(CreatureEvaluationContext context)
        {
            float score = Mathf.Max(0f, context.CurrentPosition.y - context.SpawnPosition.y) * heightReward * context.DeltaTime;
            if (context.Body != null && Mathf.Abs(context.Body.linearVelocity.y) > airVelocityThreshold)
                score += airTimeReward * context.DeltaTime;
            return score;
        }
    }
}
