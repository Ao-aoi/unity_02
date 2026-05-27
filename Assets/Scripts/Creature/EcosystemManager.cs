using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // ★リストをスコア順に並び替えるために必要

public class EcosystemManager : MonoBehaviour
{
    [Header("プレハブ設定")]
    public GameObject creaturePrefab;
    [Header("UI設定")]
    public Canvas sliderCanvas;
    public int maxCreaturesCount = 5;
    public Transform spawnPoint;

    [Header("遺伝（進化）設定")]
    [Tooltip("突然変異の確率 (0.0 ~ 1.0)")] public float mutationRate = 0.1f;
    [Tooltip("突然変異による変化量")] public float mutationAmount = 0.2f;
    [Tooltip("記憶しておく歴代の優秀な遺伝子の数")] public int genomePoolSize = 5;

    private List<GameObject> aliveCreatures = new List<GameObject>();
    private Dictionary<GameObject, Slider> creatureUiMap = new Dictionary<GameObject, Slider>();

    // 🧬 【新要素】死んだクリーチャーの「評価スコア」と「遺伝子」をセットで保存するクラス
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

    // 🧬 引数に「親の遺伝子データ」を受け取れるように変更
    public void SpawnNewCreature(CreatureGenome parentGenome)
    {
        Vector3 randomOffset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
        Vector3 spawnPos = (spawnPoint != null ? spawnPoint.position : Vector3.zero) + randomOffset;
        GameObject newCreature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);

        CreatureAgent agent = newCreature.GetComponent<CreatureAgent>();
        if (agent != null)
        {
            agent.SetupAgent(this);

            // ★ ここで、受け取った親の遺伝子を赤ちゃんの脳にセットする！
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
        // 💀 1. 死んだ個体から「評価スコア」と「遺伝子」を回収する
        CreatureAgent agent = deadCreature.GetComponent<CreatureAgent>();
        CreatureEvaluator evaluator = deadCreature.GetComponent<CreatureEvaluator>();
        
        if (agent != null && evaluator != null)
        {
            CreatureGenome deadGenome = agent.GetGenome();
            float finalFitness = evaluator.totalFitness;

            if (deadGenome != null)
            {
                // エリートプールに追加し、スコアが高い順に並び替え、定員（5個）を溢れたら最下位を捨てる
                eliteGenomes.Add(new GenomeRecord { fitness = finalFitness, genome = deadGenome });
                eliteGenomes = eliteGenomes.OrderByDescending(g => g.fitness).Take(genomePoolSize).ToList();
                
                Debug.Log($"個体が死亡。スコア: {finalFitness} / 現在の歴代最高スコア: {eliteGenomes[0].fitness}");
            }
        }

        // 2. リストとUIから削除
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

        // 🐣 3. 次の親を選んで、リアルタイム補充！
        CreatureGenome nextGenome = null;

        // エリートプールに遺伝子が1つでもストックされていれば
        if (eliteGenomes.Count > 0)
        {
            // 歴代トップ（一番スコアが高かったやつ）の遺伝子をクローン（コピー）する
            nextGenome = eliteGenomes[0].genome.Clone();
            
            // ★ 進化のスパイス：コピーした遺伝子を少しだけ「突然変異」させる！
            nextGenome.Mutate(mutationRate, mutationAmount);
        }

        // 新しい遺伝子を持って生まれ変わる
        SpawnNewCreature(nextGenome);
    }
}