using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    [System.Serializable]
    public sealed class MovementStatistics
    {
        public Vector3 PreviousPosition { get; private set; }
        public Vector3 CurrentPosition { get; private set; }
        public Vector3 DeltaPosition { get; private set; }
        public float DeltaDistance { get; private set; }
        public float TotalDistance { get; private set; }
        public float PreviousEnergyConsumed { get; private set; }
        public float DeltaEnergyConsumed { get; private set; }
        public float PreviousHpConsumed { get; private set; }
        public float DeltaHpConsumed { get; private set; }

        public void Initialize(Vector3 startPosition, CreatureAgent agent)
        {
            PreviousPosition = startPosition;
            CurrentPosition = startPosition;
            DeltaPosition = Vector3.zero;
            DeltaDistance = 0f;
            TotalDistance = 0f;
            PreviousEnergyConsumed = agent != null ? agent.TotalEnergyConsumed : 0f;
            DeltaEnergyConsumed = 0f;
            PreviousHpConsumed = agent != null ? agent.TotalHpConsumed : 0f;
            DeltaHpConsumed = 0f;
        }

        public void Sample(Vector3 position, CreatureAgent agent)
        {
            PreviousPosition = CurrentPosition;
            CurrentPosition = position;
            DeltaPosition = CurrentPosition - PreviousPosition;
            DeltaDistance = DeltaPosition.magnitude;
            TotalDistance += DeltaDistance;

            float energyConsumed = agent != null ? agent.TotalEnergyConsumed : 0f;
            DeltaEnergyConsumed = Mathf.Max(0f, energyConsumed - PreviousEnergyConsumed);
            PreviousEnergyConsumed = energyConsumed;

            float hpConsumed = agent != null ? agent.TotalHpConsumed : 0f;
            DeltaHpConsumed = Mathf.Max(0f, hpConsumed - PreviousHpConsumed);
            PreviousHpConsumed = hpConsumed;
        }
    }
}
