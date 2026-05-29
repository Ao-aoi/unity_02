using UnityEngine;
using Neuro.Creature.Evaluation;

namespace Neuro.Creature.EvolutionBuild
{
    public class CustomSpawnPoint : MonoBehaviour
    {
        public SpawnEvolutionProfile evolutionProfile;
        public bool useRuntimeRuleSetOverride;
        public SpawnRuleSet runtimeRuleSetOverride;
        public bool useRuntimeBehaviorOverride;
        public SpawnBehaviorSettings runtimeBehaviorOverride;

        public SpawnBehaviorSettings Behavior
        {
            get
            {
                if (useRuntimeBehaviorOverride && runtimeBehaviorOverride != null)
                    return runtimeBehaviorOverride;
                return evolutionProfile != null ? evolutionProfile.behavior : null;
            }
        }

        public EvaluationProfile BuildEvaluationProfile()
        {
            if (useRuntimeRuleSetOverride && runtimeRuleSetOverride != null)
                return runtimeRuleSetOverride.CreateRuntimeProfile(name + " Runtime Rules");
            return evolutionProfile != null ? evolutionProfile.CreateRuntimeEvaluationProfile() : null;
        }

        public CreatureGenome CreateDescendantGenome(CreatureGenome fallbackParent, float defaultMutationRate, float defaultMutationAmount)
        {
            SpawnBehaviorSettings behavior = Behavior;
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

        public LineageData ResolveLineage(Color fallbackColor)
        {
            SpawnBehaviorSettings behavior = Behavior;
            if (behavior != null && behavior.lineageSource != null && behavior.lineageSource.lineage != null)
                return behavior.lineageSource.lineage.CloneForChild();

            if (behavior != null && behavior.fallbackLineage != null)
                return behavior.fallbackLineage.CloneForChild();

            string lineageName = evolutionProfile != null ? evolutionProfile.profileName : name;
            return LineageData.CreateRuntime(lineageName, fallbackColor);
        }
    }
}
