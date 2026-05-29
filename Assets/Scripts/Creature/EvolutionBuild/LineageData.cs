using System;
using UnityEngine;

namespace Neuro.Creature.EvolutionBuild
{
    [Serializable]
    public class LineageData
    {
        [SerializeField] private string lineageId = Guid.NewGuid().ToString("N");
        public string lineageName = "New Lineage";
        public Color lineageColor = Color.white;
        public string parentLineageId;
        public string sourceCreatureName;

        public string LineageId => lineageId;

        public LineageData() { }

        public LineageData(string name, Color color, string parentId = null)
        {
            lineageId = Guid.NewGuid().ToString("N");
            lineageName = string.IsNullOrEmpty(name) ? "New Lineage" : name;
            lineageColor = color;
            parentLineageId = parentId;
        }

        public LineageData CloneForChild(string childName = null)
        {
            return new LineageData
            {
                lineageId = lineageId,
                lineageName = string.IsNullOrEmpty(childName) ? lineageName : childName,
                lineageColor = lineageColor,
                parentLineageId = parentLineageId,
                sourceCreatureName = sourceCreatureName
            };
        }

        public static LineageData CreateRuntime(string name, Color color, string parentId = null)
        {
            return new LineageData(name, color, parentId);
        }
    }
}
