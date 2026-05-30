using UnityEngine;
using Neuro.Creature.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Neuro.Creature.EvolutionBuild
{
    public class CustomSpawnPoint : MonoBehaviour
    {
        public SpawnEvolutionProfile evolutionProfile;
        public bool useRuntimeRuleSetOverride;
        public SpawnRuleSet runtimeRuleSetOverride;
        public bool useRuntimeBehaviorOverride;
        public SpawnBehaviorSettings runtimeBehaviorOverride;

        private class GenomeRecord
        {
            public float fitness;
            public CreatureGenome genome;
        }

        public EcosystemManager ecosystemManager;
        private readonly List<GenomeRecord> eliteGenomes = new List<GenomeRecord>();
        private readonly HashSet<CreatureAgent> aliveChildren = new HashSet<CreatureAgent>();

        public SpawnBehaviorSettings Behavior
        {
            get
            {
                if (useRuntimeBehaviorOverride && runtimeBehaviorOverride != null)
                    return runtimeBehaviorOverride;
                return evolutionProfile != null ? evolutionProfile.behavior : null;
            }
        }

        void Start()
        {
            if (ecosystemManager == null)
                ecosystemManager = FindFirstObjectByType<EcosystemManager>();
            int initialCount = ResolveDesiredCreatureCount();
            for (int i = 0; i < initialCount; i++)
            {
                SpawnOneChild();
            }
        }

        public void HandleChildDeath(CreatureAgent child, CreatureGenome deadGenome, float finalFitness)
        {
            if (child != null)
            {
                child.Died -= HandleChildDeath;
                aliveChildren.Remove(child);
            }

            if (deadGenome != null)
            {
                eliteGenomes.Add(new GenomeRecord
                {
                    fitness = finalFitness,
                    genome = deadGenome.CopyExact()
                });

                int maxPoolSize = ResolveElitePoolSize();
                if (eliteGenomes.Count > maxPoolSize)
                {
                    List<GenomeRecord> sorted = eliteGenomes
                        .OrderByDescending(g => g.fitness)
                        .Take(maxPoolSize)
                        .ToList();
                    eliteGenomes.Clear();
                    eliteGenomes.AddRange(sorted);
                }
            }

            SpawnOneChild();
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

        public CreatureGenome SelectParentGenome()
        {
            if (eliteGenomes.Count <= 0)
                return null;

            int randomIndex = Random.Range(0, eliteGenomes.Count);
            if (Random.value < 0.5f)
                randomIndex = 0;

            GenomeRecord record = eliteGenomes[randomIndex];
            return record != null && record.genome != null ? record.genome.Clone() : null;
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

        private void SpawnOneChild()
        {
            if (ecosystemManager == null)
                return;

            SpawnBehaviorSettings behavior = Behavior;
            float spawnRadius = behavior != null ? behavior.spawnRadius : 3f;

            Vector3 origin = transform.position;
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                Random.Range(-spawnRadius, spawnRadius),
                0f);
            Vector3 spawnPosition = origin + randomOffset;

            CreatureGenome parentGenome = SelectParentGenome();
            CreatureGenome genomeForChild = CreateDescendantGenome(
                parentGenome,
                ecosystemManager.mutationRate,
                ecosystemManager.mutationAmount);

            Color bodyColor = behavior != null ? behavior.bodyArmColor : Color.white;
            Color faceColor = behavior != null ? behavior.faceColor : Color.white;
            EvaluationProfile evaluationProfile = BuildEvaluationProfile();
            LineageData lineage = ResolveLineage(bodyColor);

            CreatureAgent child = ecosystemManager.SpawnCreature(
                genomeForChild,
                evaluationProfile,
                bodyColor,
                faceColor,
                spawnPosition,
                lineage,
                this);

            if (child != null)
            {
                child.Died += HandleChildDeath;
                aliveChildren.Add(child);
            }
        }

        private int ResolveDesiredCreatureCount()
        {
            SpawnBehaviorSettings behavior = Behavior;
            if (behavior != null)
                return Mathf.Max(0, behavior.desiredCreatureCount);

            return 0;
        }

        private int ResolveElitePoolSize()
        {
            SpawnBehaviorSettings behavior = Behavior;
            if (behavior != null)
                return Mathf.Max(1, behavior.elitePoolSize);

            if (ecosystemManager != null)
                return Mathf.Max(1, ecosystemManager.genomePoolSize);

            return 1;
        }
    }
}
