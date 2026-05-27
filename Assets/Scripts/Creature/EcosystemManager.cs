using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // ★リストをスコア順に並び替えるために必要
namespace Neuro.Creature{
public class EcosystemManager : MonoBehaviour
{
    public static event Action<float> CreatureDied;

    [Header("プレハブ設定")]
    public GameObject creaturePrefab;
    [Header("UI設定")]
    public Canvas sliderCanvas;
    public int maxCreaturesCount = 5;
    public Transform[] spawnPoints;

    [Header("遺伝（進化）設定")]
    [Tooltip("突然変異の確率 (0.0 ~ 1.0)")] public float mutationRate = 0.1f;
    [Tooltip("突然変異による変化量")] public float mutationAmount = 0.2f;
    [Tooltip("記憶しておく歴代の優秀な遺伝子の数")] public int genomePoolSize = 5;

    private List<GameObject> aliveCreatures = new List<GameObject>();
    private Dictionary<GameObject, Slider> creatureUiMap = new Dictionary<GameObject, Slider>();
    private class GenomeRecord
    {
        public float fitness;
        public CreatureGenome genome;
    }
    
    // 🏆 歴代の優秀な遺伝子をストックしておく「エリートプール」
    private List<GenomeRecord> eliteGenomes = new List<GenomeRecord>();

    void Start()
    {
        // 最初の世代は親がいないので、完全ランダム（nullを渡す）で定員分スポーン
        for (int i = 0; i < maxCreaturesCount; i++)
        {
            SpawnNewCreature(null);
        }
    }

    public void SpawnNewCreature(CreatureGenome parentGenome)
    {
        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);
        Vector3 spawnPos = (spawnPoints.Length > 0 ? spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position : Vector3.zero) + randomOffset;
        GameObject newCreature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);

        CreatureAgent agent = newCreature.GetComponent<CreatureAgent>();
        if (agent != null)
        {
            agent.SetupAgent(this);

            // ここで、受け取った親の遺伝子を赤ちゃんの脳にセットする！
            agent.InitializeBrain(parentGenome);

            Slider sliderComponent = agent.InitializeUIFollow(sliderCanvas);

            if (sliderComponent != null)
            {
                creatureUiMap.Add(newCreature, sliderComponent);
            }
        }

        aliveCreatures.Add(newCreature);
    }

    public void OnCreatureDied(GameObject deadCreature)
    {
        CreatureAgent agent = deadCreature.GetComponent<CreatureAgent>();
        CreatureEvaluator evaluator = deadCreature.GetComponent<CreatureEvaluator>();
        
        if (agent != null && evaluator != null)
        {
            CreatureGenome deadGenome = agent.GetGenome();
            float finalFitness = evaluator.totalFitness;

            CreatureDied?.Invoke(finalFitness);

            Debug.Log($"個体が死亡。スコア: {finalFitness}");

            if (deadGenome != null)
            {
                eliteGenomes.Add(new GenomeRecord { fitness = finalFitness, genome = deadGenome });
                eliteGenomes = eliteGenomes.OrderByDescending(g => g.fitness).Take(genomePoolSize).ToList();
            }
        }

        // 1. リストとUIから削除
        if (aliveCreatures.Contains(deadCreature))
        {
            aliveCreatures.Remove(deadCreature);
        }

        if (creatureUiMap.ContainsKey(deadCreature))
        {
            Slider associatedSlider = creatureUiMap[deadCreature];
            if (associatedSlider != null)
            {
                Destroy(associatedSlider.gameObject);
            }
            creatureUiMap.Remove(deadCreature);
        }

        Destroy(deadCreature);

        CreatureGenome nextGenome = null;

        if (eliteGenomes.Count > 0)
        {
            // 💡 改善策：常に1位だけを選ぶのではなく、プール（上位5人）の中からランダムに選ぶ！
            // これにより、優秀な遺伝子が1回の失敗で絶滅するのを防ぎ、複数の子孫を残せるようになります。
            int randomIndex = UnityEngine.Random.Range(0, eliteGenomes.Count);
            
            // さらに、超・エリート（歴代1位）が選ばれやすくなるように少しひいきする（ルーレット選択の簡易版）
            // 50%の確率で強制的に1位、残りの50%で2位〜5位からランダム
            if (UnityEngine.Random.value < 0.5f)
            {
                randomIndex = 0; // 歴代トップ
            }

            nextGenome = eliteGenomes[randomIndex].genome.Clone();
            
            // 🧬 突然変異の微調整
            // 天才の形が崩れすぎないよう、変異量（mutationAmount）を少しマイルド（0.1fなど）にするのもおすすめです
            nextGenome.Mutate(mutationRate, mutationAmount);
        }

        // 新しい遺伝子を持って生まれ変わる
        SpawnNewCreature(nextGenome);
    }
}
}