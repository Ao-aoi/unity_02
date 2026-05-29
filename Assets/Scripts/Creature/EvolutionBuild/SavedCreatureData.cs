using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    [Serializable]
    public class EvaluationHistoryEntry
    {
        public string profileName;
        public float fitness;
        public float recordedTime;
        public int generation;
    }

    [CreateAssetMenu(fileName = "SavedCreatureData", menuName = "Neuro/Evolution Build/Saved Creature Data")]
    public class SavedCreatureData : ScriptableObject
    {
        public string creatureName = "Saved Creature";
        public CreatureGenome genome;
        public Color bodyArmColor = Color.white;
        public Color faceColor = Color.white;
        public LineageData lineage = new LineageData();
        public List<EvaluationHistoryEntry> evaluationHistory = new List<EvaluationHistoryEntry>();

        public CreatureGenome CreateGenomeCopy()
        {
            return genome != null ? genome.Clone() : null;
        }

        public void CaptureFromAgent(CreatureAgent agent, CreatureEvaluator evaluator = null, string displayName = null)
        {
            if (agent == null)
                return;

            creatureName = string.IsNullOrEmpty(displayName) ? agent.name : displayName;
            CreatureGenome sourceGenome = agent.GetGenome();
            genome = sourceGenome != null ? sourceGenome.CopyExact() : null;
            bodyArmColor = agent.currentBodyArmColor;
            faceColor = agent.currentFaceColor;
            lineage = agent.Lineage != null ? agent.Lineage.CloneForChild() : LineageData.CreateRuntime(creatureName, bodyArmColor);
            lineage.sourceCreatureName = creatureName;

            if (evaluator != null)
            {
                evaluationHistory.Add(new EvaluationHistoryEntry
                {
                    profileName = evaluator.evaluationProfile != null ? evaluator.evaluationProfile.profileName : "Legacy Criteria",
                    fitness = evaluator.totalFitness,
                    recordedTime = Time.time,
                    generation = genome != null ? genome.generation : 0
                });
            }
        }
    }
}
