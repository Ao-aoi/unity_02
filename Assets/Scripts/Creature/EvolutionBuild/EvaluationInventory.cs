using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    [CreateAssetMenu(fileName = "EvaluationInventory", menuName = "Neuro/Evolution Build/Evaluation Inventory")]
    public class EvaluationInventory : ScriptableObject
    {
        public List<UnlockableEvaluationRule> unlockedRules = new List<UnlockableEvaluationRule>();
        public event Action<UnlockableEvaluationRule> RuleUnlocked;

        public bool IsUnlocked(UnlockableEvaluationRule unlockable)
        {
            return unlockable != null && unlockedRules.Contains(unlockable);
        }

        public bool Unlock(UnlockableEvaluationRule unlockable)
        {
            if (unlockable == null || unlockedRules.Contains(unlockable))
                return false;

            unlockedRules.Add(unlockable);
            RuleUnlocked?.Invoke(unlockable);
            return true;
        }

        public void AddDefaults(IEnumerable<UnlockableEvaluationRule> defaults)
        {
            if (defaults == null)
                return;

            foreach (UnlockableEvaluationRule rule in defaults)
            {
                if (rule != null && rule.unlockedByDefault && !unlockedRules.Contains(rule))
                    unlockedRules.Add(rule);
            }
        }
    }
}
