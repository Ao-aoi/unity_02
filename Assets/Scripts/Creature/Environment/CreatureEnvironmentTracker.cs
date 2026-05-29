using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Environment
{
    public class CreatureEnvironmentTracker : MonoBehaviour
    {
        private readonly Dictionary<string, int> activeTags = new Dictionary<string, int>();
        private float pendingReward;
        private float pendingPenalty;

        public bool HasTag(string tagId)
        {
            return !string.IsNullOrEmpty(tagId) && activeTags.ContainsKey(tagId) && activeTags[tagId] > 0;
        }

        public void EnterTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId))
                return;

            int count;
            activeTags.TryGetValue(tagId, out count);
            activeTags[tagId] = count + 1;
        }

        public void ExitTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId) || !activeTags.ContainsKey(tagId))
                return;

            activeTags[tagId]--;
            if (activeTags[tagId] <= 0)
                activeTags.Remove(tagId);
        }

        public void AddReward(float value)
        {
            if (value >= 0f)
                pendingReward += value;
            else
                pendingPenalty += -value;
        }

        public void AddPenalty(float value)
        {
            if (value >= 0f)
                pendingPenalty += value;
            else
                pendingReward += -value;
        }

        public float ConsumeScore()
        {
            float score = pendingReward - pendingPenalty;
            pendingReward = 0f;
            pendingPenalty = 0f;
            return score;
        }
    }
}
