using UnityEngine;
using Neuro.Creature.Evaluation;

namespace Neuro.Creature.EvolutionBuild
{
    [CreateAssetMenu(fileName = "UnlockableEvaluationRule", menuName = "Neuro/Evolution Build/Unlockable Evaluation Rule")]
    public class UnlockableEvaluationRule : ScriptableObject
    {
        public string ruleId = System.Guid.NewGuid().ToString("N");
        public string displayName = "Evaluation Fragment";
        [TextArea] public string description;
        public Sprite icon;
        public EvaluationRule evaluationRule;
        public bool unlockedByDefault;
    }
}
