using System.Collections.Generic;

namespace Neuro.Creature.Evaluation
{
    public sealed class CompositeEvaluator
    {
        private readonly List<EvaluationRuleRuntime> ruleRuntimes = new List<EvaluationRuleRuntime>();
        private LegacyCriteriaRuntime legacyRuntime;

        public void Build(EvaluationProfile profile, EvaluationCriteria legacyCriteria, CreatureEvaluationContext context)
        {
            ruleRuntimes.Clear();
            legacyRuntime = null;

            if (profile != null && profile.rules != null && profile.rules.Count > 0)
            {
                for (int i = 0; i < profile.rules.Count; i++)
                {
                    EvaluationRule rule = profile.rules[i];
                    if (rule == null)
                        continue;

                    EvaluationRuleRuntime runtime = rule.CreateRuntime();
                    runtime.Initialize(context);
                    ruleRuntimes.Add(runtime);
                }
            }
            else if (legacyCriteria != null)
            {
                legacyRuntime = new LegacyCriteriaRuntime(legacyCriteria);
                legacyRuntime.Initialize(context);
            }
        }

        public float Tick(CreatureEvaluationContext context)
        {
            float score = 0f;
            if (ruleRuntimes.Count > 0)
            {
                for (int i = 0; i < ruleRuntimes.Count; i++)
                    score += ruleRuntimes[i].Tick(context);
            }
            else if (legacyRuntime != null)
            {
                score += legacyRuntime.Tick(context);
            }

            return score;
        }
    }
}
