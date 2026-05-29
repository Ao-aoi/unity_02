using System;
using System.Collections.Generic;
using UnityEngine;
using Neuro.Creature.Evaluation;

namespace Neuro.Creature.EvolutionBuild
{
    [Serializable]
    public class SpawnRuleEntry
    {
        public EvaluationRule rule;
        [Min(0f)] public float weight = 1f;
        public bool enabled = true;
    }

    [Serializable]
    public class SpawnRuleSet
    {
        public EvaluationProfile baseProfile;
        public List<SpawnRuleEntry> additionalRules = new List<SpawnRuleEntry>();

        public EvaluationProfile CreateRuntimeProfile(string profileName)
        {
            EvaluationProfile profile = ScriptableObject.CreateInstance<EvaluationProfile>();
            profile.profileName = string.IsNullOrEmpty(profileName) ? "Runtime Spawn Profile" : profileName;
            profile.description = "Runtime profile built from SpawnRuleSet.";

            if (baseProfile != null && baseProfile.rules != null)
            {
                for (int i = 0; i < baseProfile.rules.Count; i++)
                    AddRuleCopy(profile, baseProfile.rules[i], 1f, true);
            }

            for (int i = 0; i < additionalRules.Count; i++)
            {
                SpawnRuleEntry entry = additionalRules[i];
                if (entry != null)
                    AddRuleCopy(profile, entry.rule, entry.weight, entry.enabled);
            }

            return profile;
        }

        private static void AddRuleCopy(EvaluationProfile profile, EvaluationRule source, float weightMultiplier, bool enabled)
        {
            if (profile == null || source == null || weightMultiplier <= 0f)
                return;

            EvaluationRule copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name + " (Spawn Runtime)";
            copy.enabled = enabled && source.enabled;
            copy.weight = source.weight * weightMultiplier;
            profile.rules.Add(copy);
        }
    }
}
