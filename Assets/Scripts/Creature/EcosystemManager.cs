using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Neuro.Creature{
public class EcosystemManager : MonoBehaviour
{
    [Serializable]
    public class SpawnPointConfig
    {
        public Transform pointTransform; // スポーンする場所
        public EvaluationCriteria pointCriteria; // この場所から生まれる個体に適用するルール
        
        [Header("見た目の設定")]
        public Color bodyArmColor = Color.white; // 胴体と腕の色
        public Color faceColor = Color.white;    // 顔の色
        
        [Header("遺伝設定（-1でマネージャーのデフォルトを使用）")]
        [Tooltip("スポーンポイント固有の重み変異確率。-1でマネージャーの値を使用")] public float mutationRate = -1f;
        [Tooltip("スポーンポイント固有の重み変化量。-1でマネージャーの値を使用")] public float mutationAmount = -1f;
        [Tooltip("スポーンポイント固有のエリートプールサイズ。-1でマネージャーの値を使用")] public int genomePoolSize = -1;
    }
    public static event Action<float> CreatureDied;

    [Header("プレハブ設定")]
    public GameObject creaturePrefab;
    [Header("UI設定")]
    public Canvas sliderCanvas;
    public int maxCreaturesCount = 5;
    public SpawnPointConfig[] spawnPoints;

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
    
    // 各スポーンポイントごとのエリート遺伝子プール
    private List<GenomeRecord>[] eliteGenomesPerSpawn;

    void Start()
    {
        // スポーンポイントごとのエリートプールを初期化
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            eliteGenomesPerSpawn = new List<GenomeRecord>[spawnPoints.Length];
            for (int i = 0; i < spawnPoints.Length; i++) eliteGenomesPerSpawn[i] = new List<GenomeRecord>();
        }
        for (int i = 0; i < maxCreaturesCount; i++)
        {
            SpawnNewCreature(null);
        }
    }

    public void SpawnNewCreature(CreatureGenome parentGenome)
    {
        // ランダムなスポーンポイントを選ぶ
        int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        SpawnPointConfig chosenSpawn = spawnPoints[spawnIndex];
        
        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);
        Vector3 spawnPos = (chosenSpawn.pointTransform != null ? chosenSpawn.pointTransform.position : Vector3.zero) + randomOffset;
        
        GameObject newCreature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);

        CreatureAgent agent = newCreature.GetComponent<CreatureAgent>();
        if (agent != null)
        {
            agent.SetupAgent(this);
            
            // 💡 【新要素】選ばれたスポーンポイントの色をクリーチャーに渡す！
            agent.SetColors(chosenSpawn.bodyArmColor, chosenSpawn.faceColor);
            // origin を記録
            agent.originSpawnIndex = spawnIndex;
            
            // 親ゲノムが指定されていなければ、そのスポーンポイントのエリートプールから選ぶ
            if (parentGenome == null && eliteGenomesPerSpawn != null && spawnIndex >= 0 && spawnIndex < eliteGenomesPerSpawn.Length)
            {
                var pool = eliteGenomesPerSpawn[spawnIndex];
                if (pool != null && pool.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                    if (UnityEngine.Random.value < 0.5f) randomIndex = 0;
                    CreatureGenome parent = pool[randomIndex].genome.Clone();

                    float useMutRate = (chosenSpawn != null && chosenSpawn.mutationRate >= 0f) ? chosenSpawn.mutationRate : mutationRate;
                    float useMutAmount = (chosenSpawn != null && chosenSpawn.mutationAmount >= 0f) ? chosenSpawn.mutationAmount : mutationAmount;

                    parentGenome = CreatureAgent.ApplyDetailedMutation(parent, useMutRate, useMutAmount, CreatureAgent.DefaultMutationConfig);
                }
            }

            agent.InitializeBrain(parentGenome);

            CreatureEvaluator evaluator = newCreature.GetComponent<CreatureEvaluator>();
            if (evaluator != null && chosenSpawn.pointCriteria != null)
            {
                evaluator.currentCriteria = chosenSpawn.pointCriteria;
            }

            Slider sliderComponent = agent.InitializeUIFollow(sliderCanvas);
            if (sliderComponent != null) creatureUiMap.Add(newCreature, sliderComponent);
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
                int origin = agent.originSpawnIndex;
                if (eliteGenomesPerSpawn != null && origin >= 0 && origin < eliteGenomesPerSpawn.Length)
                {
                    var pool = eliteGenomesPerSpawn[origin];
                    pool.Add(new GenomeRecord { fitness = finalFitness, genome = deadGenome });

                    int poolSize = genomePoolSize;
                    if (spawnPoints != null && origin >= 0 && origin < spawnPoints.Length && spawnPoints[origin] != null && spawnPoints[origin].genomePoolSize >= 0)
                        poolSize = spawnPoints[origin].genomePoolSize;

                    eliteGenomesPerSpawn[origin] = pool.OrderByDescending(g => g.fitness).Take(poolSize).ToList();
                }
            }
        }

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

        // 次の個体はスポーン時にそのスポーンポイントのプールから親を選ぶ
        SpawnNewCreature(null);
    }
}
}