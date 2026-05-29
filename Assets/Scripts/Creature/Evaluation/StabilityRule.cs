using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "StabilityRule", menuName = "Neuro/Evaluation/Rules/Stability")]
    public class StabilityRule : EvaluationRule
    {
        public float uprightRewardPerSecond = 0.5f;
        public float angularVelocityPenalty = 0.02f;
        public float upsideDownPenaltyPerSecond = 2f;
        [Range(-1f, 1f)] public float upsideDownDotThreshold = 0f;
        public override float Evaluate(CreatureEvaluationContext context)
        {
            float score = 0f;
            float uprightDot = Vector3.Dot(context.Transform.up, Vector3.up);
            score += Mathf.Max(0f, uprightDot) * uprightRewardPerSecond * context.DeltaTime;
            if (context.Body != null) score -= Mathf.Abs(context.Body.angularVelocity) * angularVelocityPenalty * context.DeltaTime;
            if (uprightDot < upsideDownDotThreshold) score -= upsideDownPenaltyPerSecond * context.DeltaTime;
            return score;
        }
    }
}
