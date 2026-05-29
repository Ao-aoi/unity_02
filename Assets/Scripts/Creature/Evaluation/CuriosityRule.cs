using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "CuriosityRule", menuName = "Neuro/Evaluation/Rules/Curiosity")]
    public class CuriosityRule : EvaluationRule
    {
        public float approachReward = 5f;
        public float escapePenalty = 0f;
        public float maxDistance = 10f;
        public override EvaluationRuleRuntime CreateRuntime() => new Runtime(this);
        private sealed class Runtime : EvaluationRuleRuntime
        {
            private readonly CuriosityRule curiosityRule;
            private CuriosityTarget currentTarget;
            private float previousDistance = Mathf.Infinity;
            public Runtime(CuriosityRule rule) : base(rule) { curiosityRule = rule; }
            public override void Initialize(CreatureEvaluationContext context) { currentTarget = null; previousDistance = Mathf.Infinity; }
            public override float Tick(CreatureEvaluationContext context)
            {
                if (curiosityRule == null || !curiosityRule.enabled) return 0f;
                CuriosityTarget closest = null;
                float closestDistance = Mathf.Infinity;
                var targets = context.CuriosityTargets;
                for (int i = 0; i < targets.Count; i++)
                {
                    CuriosityTarget target = targets[i];
                    if (target == null) continue;
                    float distance = Vector3.Distance(target.transform.position, context.CurrentPosition);
                    if (distance < closestDistance && distance <= curiosityRule.maxDistance) { closestDistance = distance; closest = target; }
                }
                float score = 0f;
                if (closest != null)
                {
                    if (currentTarget == closest && previousDistance != Mathf.Infinity)
                    {
                        float delta = previousDistance - closestDistance;
                        float value = Mathf.Max(0f, closest.curiosityValue);
                        if (delta > 0f) score += delta * curiosityRule.approachReward * value;
                        else if (delta < 0f) score += delta * curiosityRule.escapePenalty * value;
                    }
                    currentTarget = closest;
                    previousDistance = closestDistance;
                }
                return score * curiosityRule.weight * curiosityRule.rewardMultiplier;
            }
        }
    }
}
