using System;
using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    public class EvaluationRuleUnlock : MonoBehaviour
    {
        public UnlockableEvaluationRule unlockableRule;
        public EvaluationInventory targetInventory;
        public GameObject collectEffectPrefab;
        public bool destroyOnCollect = true;

        public static event Action<UnlockableEvaluationRule> RuleUnlocked;

        public static void NotifyRuleUnlocked(UnlockableEvaluationRule unlockableRule)
        {
            RuleUnlocked?.Invoke(unlockableRule);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();
            if (agent == null)
                return;

            EvaluationInventory inventory = targetInventory != null ? targetInventory : EvaluationInventoryProvider.ActiveInventory;
            if (inventory != null)
                inventory.Unlock(unlockableRule);

            NotifyRuleUnlocked(unlockableRule);

            if (collectEffectPrefab != null)
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);

            if (destroyOnCollect)
                Destroy(gameObject);
        }
    }
}
