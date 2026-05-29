using System.Collections.Generic;
using UnityEngine;
namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "TerrainProgressRule", menuName = "Neuro/Evaluation/Rules/Terrain Progress")]
    public class TerrainProgressRule : EvaluationRule
    {
        public enum ProgressAxis { PositiveX, NegativeX, PositiveY, NegativeY }
        public ProgressAxis axis = ProgressAxis.PositiveX;
        public float milestoneInterval = 5f;
        public float milestoneReward = 10f;
        public bool useSceneMarkers = true;
        public override EvaluationRuleRuntime CreateRuntime() => new Runtime(this);
        private sealed class Runtime : EvaluationRuleRuntime
        {
            private readonly TerrainProgressRule terrainRule;
            private readonly HashSet<TerrainProgressMarker> rewardedMarkers = new HashSet<TerrainProgressMarker>();
            private float bestProgress;
            private int rewardedMilestones;
            public Runtime(TerrainProgressRule rule) : base(rule) { terrainRule = rule; }
            public override void Initialize(CreatureEvaluationContext context)
            {
                bestProgress = GetProgress(context.CurrentPosition, context.SpawnPosition);
                rewardedMilestones = 0;
                rewardedMarkers.Clear();
            }
            public override float Tick(CreatureEvaluationContext context)
            {
                if (terrainRule == null || !terrainRule.enabled) return 0f;
                bestProgress = Mathf.Max(bestProgress, GetProgress(context.CurrentPosition, context.SpawnPosition));
                float score = 0f;
                if (terrainRule.milestoneInterval > 0f)
                {
                    int milestoneCount = Mathf.FloorToInt(bestProgress / terrainRule.milestoneInterval);
                    int newlyCleared = Mathf.Max(0, milestoneCount - rewardedMilestones);
                    rewardedMilestones = Mathf.Max(rewardedMilestones, milestoneCount);
                    score += newlyCleared * terrainRule.milestoneReward;
                }
                if (terrainRule.useSceneMarkers)
                {
                    var markers = TerrainProgressMarkerRegistry.Markers;
                    for (int i = 0; i < markers.Count; i++)
                    {
                        TerrainProgressMarker marker = markers[i];
                        if (marker == null || rewardedMarkers.Contains(marker)) continue;
                        float markerProgress = marker.progressX - context.SpawnPosition.x;
                        if (terrainRule.axis == ProgressAxis.NegativeX) markerProgress = context.SpawnPosition.x - marker.progressX;
                        if ((terrainRule.axis == ProgressAxis.PositiveX || terrainRule.axis == ProgressAxis.NegativeX) && bestProgress >= markerProgress)
                        {
                            score += marker.reward;
                            rewardedMarkers.Add(marker);
                        }
                    }
                }
                return score * terrainRule.weight * terrainRule.rewardMultiplier;
            }
            private float GetProgress(Vector3 position, Vector3 spawnPosition)
            {
                Vector3 delta = position - spawnPosition;
                switch (terrainRule.axis)
                {
                    case ProgressAxis.NegativeX: return -delta.x;
                    case ProgressAxis.PositiveY: return delta.y;
                    case ProgressAxis.NegativeY: return -delta.y;
                    default: return delta.x;
                }
            }
        }
    }
}
