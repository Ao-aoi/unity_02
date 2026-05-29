using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    public class TerrainProgressMarker : MonoBehaviour
    {
        [Tooltip("この地点を越えたときの基本加点")]
        public float reward = 10f;
        [Tooltip("X軸で突破判定する場合のしきい値。未設定ならこのTransformのX座標")]
        public float progressX;

        void Reset()
        {
            progressX = transform.position.x;
        }

        void OnEnable()
        {
            TerrainProgressMarkerRegistry.Register(this);
        }

        void OnDisable()
        {
            TerrainProgressMarkerRegistry.Unregister(this);
        }
    }

    public static class TerrainProgressMarkerRegistry
    {
        private static readonly List<TerrainProgressMarker> markers = new List<TerrainProgressMarker>();
        public static IReadOnlyList<TerrainProgressMarker> Markers => markers;

        public static void Register(TerrainProgressMarker marker)
        {
            if (marker != null && !markers.Contains(marker))
                markers.Add(marker);
        }

        public static void Unregister(TerrainProgressMarker marker)
        {
            if (marker != null)
                markers.Remove(marker);
        }
    }
}
