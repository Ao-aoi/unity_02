using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    public sealed class CreatureEvaluationContext
    {
        public CreatureEvaluator Evaluator { get; }
        public CreatureAgent Agent { get; }
        public Transform Transform { get; }
        public Rigidbody2D Body { get; }
        public MovementStatistics Movement { get; }
        public ExplorationTracker Exploration { get; }
        public float DeltaTime { get; internal set; }
        public float ElapsedTime { get; internal set; }
        public Vector3 SpawnPosition { get; }
        public Vector3 CurrentPosition => Transform != null ? Transform.position : Vector3.zero;
        public IReadOnlyList<CreatureAgent> OtherCreatures => CreatureRegistry.Agents;
        public IReadOnlyList<FoodItem> Foods => FoodRegistry.Foods;
        public IReadOnlyList<CuriosityTarget> CuriosityTargets => CuriosityTargetRegistry.Targets;

        public CreatureEvaluationContext(CreatureEvaluator evaluator, CreatureAgent agent, Transform transform, Rigidbody2D body, MovementStatistics movement, ExplorationTracker exploration, Vector3 spawnPosition)
        {
            Evaluator = evaluator;
            Agent = agent;
            Transform = transform;
            Body = body;
            Movement = movement;
            Exploration = exploration;
            SpawnPosition = spawnPosition;
        }
    }
}
