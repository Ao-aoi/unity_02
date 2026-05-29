using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    public class EvaluationInventoryProvider : MonoBehaviour
    {
        public EvaluationInventory inventory;
        public static EvaluationInventory ActiveInventory { get; private set; }

        void OnEnable()
        {
            if (inventory != null)
                ActiveInventory = inventory;
        }

        void OnDisable()
        {
            if (ActiveInventory == inventory)
                ActiveInventory = null;
        }
    }
}
