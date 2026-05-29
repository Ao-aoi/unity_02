using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "FlockingRule", menuName = "Neuro/Evaluation/Rules/Flocking")]
    public class FlockingRule : EvaluationRule
    {
        public enum FlockingMode { Cohesion, Separation, PreferredDistanceBand }
        public FlockingMode mode = FlockingMode.Cohesion;
        public float radius = 5f;
        public float preferredDistance = 2f;
        public float tolerance = 0.5f;
        public float rewardPerSecond = 1f;
        public float penaltyPerSecond = 1f;
        public override float Evaluate(CreatureEvaluationContext context)
        {
            float nearestDistance = Mathf.Infinity;
            var creatures = context.OtherCreatures;
            for (int i = 0; i < creatures.Count; i++)
            {
                CreatureAgent other = creatures[i];
                if (other == null || other == context.Agent) continue;
                float distance = Vector3.Distance(other.transform.position, context.CurrentPosition);
                if (distance < nearestDistance) nearestDistance = distance;
            }
            if (nearestDistance == Mathf.Infinity) return 0f;
            switch (mode)
            {
                case FlockingMode.Separation:
                    return nearestDistance >= radius ? rewardPerSecond * context.DeltaTime : -penaltyPerSecond * (1f - nearestDistance / Mathf.Max(0.01f, radius)) * context.DeltaTime;
                case FlockingMode.PreferredDistanceBand:
                    return Mathf.Abs(nearestDistance - preferredDistance) <= tolerance ? rewardPerSecond * context.DeltaTime : -penaltyPerSecond * context.DeltaTime;
                default:
                    return nearestDistance <= radius ? rewardPerSecond * (1f - nearestDistance / Mathf.Max(0.01f, radius)) * context.DeltaTime : -penaltyPerSecond * context.DeltaTime;
            }
        }
    }
}
