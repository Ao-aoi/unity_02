using UnityEngine;
using Neuro.Creature.Environment;

namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "EnvironmentInteractionRule", menuName = "Neuro/Evaluation/Environment Interaction Rule")]
    public class EnvironmentInteractionRule : EvaluationRule
    {
        [Tooltip("空なら全環境スコアを評価します。指定すると該当タグに滞在中だけ継続報酬を追加します。")]
        public string requiredEnvironmentTag;
        public float activeTagRewardPerSecond;

        public override float Evaluate(CreatureEvaluationContext context)
        {
            if (context == null || context.Agent == null)
                return 0f;

            CreatureEnvironmentTracker tracker = context.Agent.GetComponent<CreatureEnvironmentTracker>();
            if (tracker == null)
                return 0f;

            float score = tracker.ConsumeScore();
            if (!string.IsNullOrEmpty(requiredEnvironmentTag) && tracker.HasTag(requiredEnvironmentTag))
                score += activeTagRewardPerSecond * context.DeltaTime;

            return score;
        }
    }
}
