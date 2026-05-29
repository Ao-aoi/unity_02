using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    [CreateAssetMenu(fileName = "CreatureArchive", menuName = "Neuro/Evolution Build/Creature Archive")]
    public class CreatureArchive : ScriptableObject
    {
        public List<SavedCreatureData> savedCreatures = new List<SavedCreatureData>();

        public SavedCreatureData SaveRuntimeCreature(CreatureAgent agent, CreatureEvaluator evaluator = null, string displayName = null)
        {
            if (agent == null)
                return null;

            SavedCreatureData data = CreateInstance<SavedCreatureData>();
            data.CaptureFromAgent(agent, evaluator, displayName);
            savedCreatures.Add(data);
            return data;
        }

        public bool ContainsLineage(string lineageId)
        {
            if (string.IsNullOrEmpty(lineageId))
                return false;

            for (int i = 0; i < savedCreatures.Count; i++)
            {
                SavedCreatureData data = savedCreatures[i];
                if (data != null && data.lineage != null && data.lineage.LineageId == lineageId)
                    return true;
            }

            return false;
        }
    }
}
