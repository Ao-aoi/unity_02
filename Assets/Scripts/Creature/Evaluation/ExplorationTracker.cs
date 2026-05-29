using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    [System.Serializable]
    public sealed class ExplorationTracker
    {
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        private readonly HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
        private Vector2Int currentCell;

        public int VisitedCellCount => visitedCells.Count;
        public Vector2Int CurrentCell => currentCell;

        public void Configure(float newCellSize)
        {
            cellSize = Mathf.Max(0.1f, newCellSize);
        }

        public void Reset(Vector3 startPosition)
        {
            visitedCells.Clear();
            currentCell = WorldToCell(startPosition);
            visitedCells.Add(currentCell);
        }

        public bool Sample(Vector3 worldPosition)
        {
            currentCell = WorldToCell(worldPosition);
            return visitedCells.Add(currentCell);
        }

        private Vector2Int WorldToCell(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.y / cellSize));
        }
    }
}
