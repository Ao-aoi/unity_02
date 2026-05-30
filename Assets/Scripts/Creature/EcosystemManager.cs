using System;
using UnityEngine;
using UnityEngine.UI;
using Neuro.Creature.Evaluation;
using Neuro.Creature.EvolutionBuild;

namespace Neuro.Creature{
public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager Instance { get; private set; }
    public static event Action<float> CreatureDied;

    [Header("プレハブ設定")]
    public GameObject creaturePrefab;

    [Header("UI設定")]
    public Canvas sliderCanvas;

    [Header("遺伝（進化）設定")]
    [Tooltip("突然変異の確率 (0.0 ~ 1.0)")] public float mutationRate = 0.1f;
    [Tooltip("突然変異による変化量")] public float mutationAmount = 0.2f;
    [Tooltip("記憶しておく歴代の優秀な遺伝子の数")] public int genomePoolSize = 5;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ReportCreatureDied(float finalFitness)
    {
        CreatureDied?.Invoke(finalFitness);
    }

    public CreatureAgent SpawnCreature(
        CreatureGenome genome,
        EvaluationProfile evaluationProfile,
        Color bodyColor,
        Color faceColor,
        Vector3 position,
        LineageData lineage,
        CustomSpawnPoint originSpawnPoint)
    {
        if (creaturePrefab == null)
            return null;

        GameObject newCreature = Instantiate(creaturePrefab, position, Quaternion.identity);
        CreatureAgent agent = newCreature.GetComponent<CreatureAgent>();
        if (agent == null)
            return null;

        agent.SetColors(bodyColor, faceColor);
        agent.SetLineage(lineage);
        agent.SetOriginSpawnPoint(originSpawnPoint);
        agent.InitializeBrain(genome);
        agent.InitializeUIFollow(sliderCanvas);

        CreatureEvaluator evaluator = newCreature.GetComponent<CreatureEvaluator>();
        if (evaluator != null && evaluationProfile != null)
            evaluator.SetEvaluationProfile(evaluationProfile);

        return agent;
    }

}
}