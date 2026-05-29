using UnityEngine;
using Neuro.Creature.Evaluation;

namespace Neuro.Creature.EvolutionBuild
{
    [CreateAssetMenu(fileName = "SpawnEvolutionProfile", menuName = "Neuro/Evolution Build/Spawn Evolution Profile")]
    public class SpawnEvolutionProfile : ScriptableObject
    {
        public string profileName = "Spawn Evolution Profile";
        [TextArea] public string description;
        public SpawnRuleSet ruleSet = new SpawnRuleSet();
        public SpawnBehaviorSettings behavior = new SpawnBehaviorSettings();

        public EvaluationProfile CreateRuntimeEvaluationProfile()
        {
            return ruleSet != null ? ruleSet.CreateRuntimeProfile(profileName) : null;
        }

        public CreatureGenome CreateAncestorGenome(float defaultMutationRate, float defaultMutationAmount, CreatureGenome fallbackParent = null)
        {
            CreatureGenome parent = fallbackParent;
            if (parent == null && behavior != null && behavior.lineageSource != null)
                parent = behavior.lineageSource.CreateGenomeCopy();

            if (parent == null)
                return null;

            float rate = behavior != null ? behavior.mutationRate : defaultMutationRate;
            float amount = behavior != null ? behavior.mutationAmount : defaultMutationAmount;
            MutationBiasSettings bias = behavior != null ? behavior.mutationBias : null;
            if (bias != null)
            {
                rate *= bias.weightMutationRateMultiplier;
                amount *= bias.weightMutationAmountMultiplier;
            }

            CreatureAgent.MutationConfig config = bias != null ? bias.CreateConfig(CreatureAgent.DefaultMutationConfig) : CreatureAgent.DefaultMutationConfig;
            return CreatureAgent.ApplyDetailedMutation(parent, rate, amount, config);
        }

        public LineageData ResolveLineage()
        {
            if (behavior != null && behavior.lineageSource != null && behavior.lineageSource.lineage != null)
                return behavior.lineageSource.lineage.CloneForChild();

            if (behavior != null && behavior.fallbackLineage != null)
                return behavior.fallbackLineage.CloneForChild();

            return LineageData.CreateRuntime(profileName, Color.white);
        }
    }
}
