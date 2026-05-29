using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Evaluation
{
    [CreateAssetMenu(fileName = "EvaluationProfile", menuName = "Neuro/Evaluation/Profile")]
    public class EvaluationProfile : ScriptableObject
    {
        public string profileName = "評価プロファイル";
        [TextArea] public string description;
        public List<EvaluationRule> rules = new List<EvaluationRule>();
    }
}
