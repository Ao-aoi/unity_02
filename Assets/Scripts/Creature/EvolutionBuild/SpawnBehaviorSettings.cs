using System;
using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    [Serializable]
    public class MutationBiasSettings
    {
        [Min(0f)] public float weightMutationRateMultiplier = 1f;
        [Min(0f)] public float weightMutationAmountMultiplier = 1f;
        [Min(0f)] public float structuralMutationMultiplier = 1f;
        [Min(0f)] public float limbMutationMultiplier = 1f;
        [Min(0f)] public float sensorMutationMultiplier = 1f;
        [Min(0f)] public float brainMutationMultiplier = 1f;

        public CreatureAgent.MutationConfig CreateConfig(CreatureAgent.MutationConfig source)
        {
            CreatureAgent.MutationConfig defaults = source ?? CreatureAgent.DefaultMutationConfig;
            return new CreatureAgent.MutationConfig
            {
                structuralMutationProb = defaults.structuralMutationProb * structuralMutationMultiplier,
                armAddBase = defaults.armAddBase * limbMutationMultiplier,
                armRemoveBase = defaults.armRemoveBase * limbMutationMultiplier,
                jointAddBase = defaults.jointAddBase * limbMutationMultiplier,
                jointRemoveBase = defaults.jointRemoveBase * limbMutationMultiplier,
                nodeAddBase = defaults.nodeAddBase * brainMutationMultiplier,
                nodeRemoveBase = defaults.nodeRemoveBase * brainMutationMultiplier,
                sightRangeChangeBase = defaults.sightRangeChangeBase * sensorMutationMultiplier,
                fieldOfViewChangeBase = defaults.fieldOfViewChangeBase * sensorMutationMultiplier,
                poissonLambda = defaults.poissonLambda,
                minHiddenNodes = defaults.minHiddenNodes,
                maxHiddenNodes = defaults.maxHiddenNodes
            };
        }
    }

    [Serializable]
    public class SpawnBehaviorSettings
    {
        [Min(0)] public int desiredCreatureCount = 5;
        [Min(0f)] public float spawnRadius = 3f;
        [Min(1)] public int elitePoolSize = 5;
        [Min(0f)] public float mutationRate = 0.1f;
        [Min(0f)] public float mutationAmount = 0.2f;
        public MutationBiasSettings mutationBias = new MutationBiasSettings();
        public SavedCreatureData lineageSource;
        public LineageData fallbackLineage = new LineageData("Wild Lineage", Color.white);
        public Color bodyArmColor = Color.white;
        public Color faceColor = Color.white;
    }
}
