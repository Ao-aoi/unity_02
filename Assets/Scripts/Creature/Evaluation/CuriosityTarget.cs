using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    public class CuriosityTarget : MonoBehaviour
    {
        [Tooltip("好奇心評価で近づいたときに加点される倍率")]
        public float curiosityValue = 1f;

        void OnEnable()
        {
            CuriosityTargetRegistry.Register(this);
        }

        void OnDisable()
        {
            CuriosityTargetRegistry.Unregister(this);
        }
    }

    public static class CuriosityTargetRegistry
    {
        private static readonly List<CuriosityTarget> targets = new List<CuriosityTarget>();
        public static IReadOnlyList<CuriosityTarget> Targets => targets;

        public static void Register(CuriosityTarget target)
        {
            if (target != null && !targets.Contains(target))
                targets.Add(target);
        }

        public static void Unregister(CuriosityTarget target)
        {
            if (target != null)
                targets.Remove(target);
        }
    }
}
