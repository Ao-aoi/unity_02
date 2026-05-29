using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    public abstract class EvaluationRule : ScriptableObject
    {
        [Header("共通設定")]
        public bool enabled = true;
        public float weight = 1f;
        public float rewardMultiplier = 1f;

        public virtual EvaluationRuleRuntime CreateRuntime()
        {
            return new EvaluationRuleRuntime(this);
        }

        public virtual float Evaluate(CreatureEvaluationContext context)
        {
            return 0f;
        }
    }

    public class EvaluationRuleRuntime
    {
        protected readonly EvaluationRule rule;

        public EvaluationRuleRuntime(EvaluationRule rule)
        {
            this.rule = rule;
        }

        public virtual void Initialize(CreatureEvaluationContext context) { }

        public virtual float Tick(CreatureEvaluationContext context)
        {
            if (rule == null || !rule.enabled)
                return 0f;

            return rule.Evaluate(context) * rule.weight * rule.rewardMultiplier;
        }
    }
}
