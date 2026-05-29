using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Neuro.Creature.Evaluation;
using Neuro.Creature.EvolutionBuild;

namespace Neuro.Creature{
public class EcosystemManager : MonoBehaviour
{
    [Serializable]
    public class SpawnPointConfig
    {
        public Transform pointTransform; // スポーンする場所
        public EvaluationProfile pointEvaluationProfile; // 複数ルールを組み合わせる新評価プロファイル
        public CustomSpawnPoint customSpawnPoint; // フェーズ5: Runtime/Inspector編集可能なスポーンポイント
        public SpawnEvolutionProfile spawnEvolutionProfile; // Transformだけで運用する場合のSOプロファイル
        [Tooltip("このスポーンポイントで常に維持する個体数。0以下ならマネージャーの初期生成数を使う")] public int creatureCount = -1;
        
        [Header("見た目の設定")]
        public Color bodyArmColor = Color.white; // 胴体と腕の色
        public Color faceColor = Color.white;    // 顔の色
        public LineageData lineage = new LineageData("Spawn Lineage", Color.white);
        
        [Header("遺伝設定（-1でマネージャーのデフォルトを使用）")]
        [Tooltip("スポーンポイント固有の重み変異確率。-1でマネージャーの値を使用")] public float mutationRate = -1f;
        [Tooltip("スポーンポイント固有の重み変化量。-1でマネージャーの値を使用")] public float mutationAmount = -1f;
        [Tooltip("スポーンポイント固有のエリートプールサイズ。-1でマネージャーの値を使用")] public int genomePoolSize = -1;

        public SpawnBehaviorSettings ResolveBehavior()
        {
            if (customSpawnPoint != null) return customSpawnPoint.Behavior;
            if (spawnEvolutionProfile != null) return spawnEvolutionProfile.behavior;
            return null;
        }

        public EvaluationProfile BuildEvaluationProfile()
        {
            if (customSpawnPoint != null) return customSpawnPoint.BuildEvaluationProfile();
            if (spawnEvolutionProfile != null) return spawnEvolutionProfile.CreateRuntimeEvaluationProfile();
            return pointEvaluationProfile;
        }

        public LineageData ResolveLineage()
        {
            if (customSpawnPoint != null) return customSpawnPoint.ResolveLineage(bodyArmColor);
            if (spawnEvolutionProfile != null) return spawnEvolutionProfile.ResolveLineage();
            return lineage != null ? lineage.CloneForChild() : LineageData.CreateRuntime("Spawn Lineage", bodyArmColor);
        }
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

            for (int spawnIndex = 0; spawnIndex < spawnPoints.Length; spawnIndex++)
            {
                int spawnCount = GetSpawnPointCreatureCount(spawnIndex);
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnNewCreature(null, spawnIndex);
                }
            }
        }
    }

    public void SpawnNewCreature(CreatureGenome parentGenome)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        SpawnNewCreature(parentGenome, spawnIndex);
    }

    public void SpawnNewCreature(CreatureGenome parentGenome, int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
        {
            spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        }

        SpawnPointConfig chosenSpawn = spawnPoints[spawnIndex];

        Transform spawnTransform = chosenSpawn != null && chosenSpawn.customSpawnPoint != null ? chosenSpawn.customSpawnPoint.transform : (chosenSpawn != null ? chosenSpawn.pointTransform : null);
        Vector3 spawnOrigin = spawnTransform != null ? spawnTransform.position : Vector3.zero;
        SpawnBehaviorSettings behavior = chosenSpawn != null ? chosenSpawn.ResolveBehavior() : null;
        float spawnRadius = behavior != null ? behavior.spawnRadius : 3f;
        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius), 0);
        Vector3 spawnPos = spawnOrigin + randomOffset;

        GameObject newCreature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);

        CreatureAgent agent = newCreature.GetComponent<CreatureAgent>();
        if (agent != null)
        {
            agent.SetupAgent(this);
            
            // 💡 【新要素】選ばれたスポーンポイントの色をクリーチャーに渡す！
            if (chosenSpawn != null)
            {
                Color bodyColor = behavior != null ? behavior.bodyArmColor : chosenSpawn.bodyArmColor;
                Color faceColor = behavior != null ? behavior.faceColor : chosenSpawn.faceColor;
                agent.SetColors(bodyColor, faceColor);
                agent.SetLineage(chosenSpawn.ResolveLineage());
            }
            // origin を記録
            agent.originSpawnIndex = spawnIndex;
            
            // 親ゲノムが指定されていなければ、そのスポーンポイントのエリートプール/保存血統から選ぶ
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

                    if (chosenSpawn != null && chosenSpawn.customSpawnPoint != null)
                        parentGenome = chosenSpawn.customSpawnPoint.CreateDescendantGenome(parent, useMutRate, useMutAmount);
                    else if (chosenSpawn != null && chosenSpawn.spawnEvolutionProfile != null)
                        parentGenome = chosenSpawn.spawnEvolutionProfile.CreateAncestorGenome(useMutRate, useMutAmount, parent);
                    else
                        parentGenome = CreatureAgent.ApplyDetailedMutation(parent, useMutRate, useMutAmount, CreatureAgent.DefaultMutationConfig);
                }
            }

            if (parentGenome == null && chosenSpawn != null)
            {
                if (chosenSpawn.customSpawnPoint != null)
                    parentGenome = chosenSpawn.customSpawnPoint.CreateDescendantGenome(null, mutationRate, mutationAmount);
                else if (chosenSpawn.spawnEvolutionProfile != null)
                    parentGenome = chosenSpawn.spawnEvolutionProfile.CreateAncestorGenome(mutationRate, mutationAmount);
            }

            agent.InitializeBrain(parentGenome);

            CreatureEvaluator evaluator = newCreature.GetComponent<CreatureEvaluator>();
            if (evaluator != null && chosenSpawn != null)
            {
                EvaluationProfile profile = chosenSpawn.BuildEvaluationProfile();
                if (profile != null)
                    evaluator.SetEvaluationProfile(profile);
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
        int origin = -1;
        
        if (agent != null && evaluator != null)
        {
            CreatureGenome deadGenome = agent.GetGenome();
            float finalFitness = evaluator.totalFitness;

            CreatureDied?.Invoke(finalFitness);

            // Debug.Log($"個体が死亡。スコア: {finalFitness}");

            if (deadGenome != null)
            {
                origin = agent.originSpawnIndex;
                if (eliteGenomesPerSpawn != null && origin >= 0 && origin < eliteGenomesPerSpawn.Length)
                {
                    var pool = eliteGenomesPerSpawn[origin];
                    pool.Add(new GenomeRecord { fitness = finalFitness, genome = deadGenome.CopyExact() });

                    int poolSize = genomePoolSize;
                    if (spawnPoints != null && origin >= 0 && origin < spawnPoints.Length && spawnPoints[origin] != null)
                    {
                        SpawnBehaviorSettings behavior = spawnPoints[origin].ResolveBehavior();
                        if (behavior != null)
                            poolSize = behavior.elitePoolSize;
                        else if (spawnPoints[origin].genomePoolSize >= 0)
                            poolSize = spawnPoints[origin].genomePoolSize;
                    }

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
        if (origin >= 0 && spawnPoints != null && origin < spawnPoints.Length)
        {
            SpawnNewCreature(null, origin);
        }
        else
        {
            SpawnNewCreature(null);
        }
    }

    private int GetSpawnPointCreatureCount(int spawnIndex)
    {
        if (spawnPoints == null || spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
            return 0;

        SpawnPointConfig spawnPoint = spawnPoints[spawnIndex];
        if (spawnPoint == null)
            return 0;

        SpawnBehaviorSettings behavior = spawnPoint.ResolveBehavior();
        if (behavior != null)
            return Mathf.Max(0, behavior.desiredCreatureCount);

        if (spawnPoint.creatureCount < 0)
            return Mathf.Max(0, maxCreaturesCount);

        return spawnPoint.creatureCount;
    }

    private EvaluationProfile BuildProfileFromCriteria(EvaluationCriteria criteria)
    {
        var profile = ScriptableObject.CreateInstance<Neuro.Creature.Evaluation.EvaluationProfile>();
        profile.profileName = (criteria != null && !string.IsNullOrEmpty(criteria.criteriaName)) ? criteria.criteriaName + " (migrated)" : "Migrated Profile";
        profile.description = "Runtime-generated profile converted from legacy EvaluationCriteria.";

        if (criteria == null)
            return profile;

        // Survival
        if (criteria.survivalRewardPerSec != 0f)
        {
            var survival = ScriptableObject.CreateInstance<Neuro.Creature.Evaluation.SurvivalRule>();
            survival.enabled = true;
            survival.weight = 1f;
            survival.rewardMultiplier = 1f;
            survival.rewardPerSecond = criteria.survivalRewardPerSec;
            profile.rules.Add(survival);
        }

        // Food approach / escape
        if (criteria.approachFoodReward != 0f || criteria.escapeFoodPenalty != 0f)
        {
            var food = ScriptableObject.CreateInstance<Neuro.Creature.Evaluation.FoodApproachRule>();
            food.enabled = true;
            food.weight = 1f;
            food.rewardMultiplier = 1f;
            food.approachFoodReward = criteria.approachFoodReward;
            food.escapeFoodPenalty = criteria.escapeFoodPenalty;
            profile.rules.Add(food);
        }

        // Horizontal movement
        if (criteria.horizontalMoveReward != 0f)
        {
            var horiz = ScriptableObject.CreateInstance<Neuro.Creature.Evaluation.HorizontalMovementRule>();
            horiz.enabled = true;
            horiz.weight = 1f;
            horiz.rewardMultiplier = 1f;
            horiz.rewardPerDistanceSecond = criteria.horizontalMoveReward;
            profile.rules.Add(horiz);
        }

        // Height and air time
        if (criteria.heightReward != 0f || criteria.airTimeReward != 0f)
        {
            var height = ScriptableObject.CreateInstance<Neuro.Creature.Evaluation.HeightAirTimeRule>();
            height.enabled = true;
            height.weight = 1f;
            height.rewardMultiplier = 1f;
            height.heightReward = criteria.heightReward;
            height.airTimeReward = criteria.airTimeReward;
            profile.rules.Add(height);
        }

        // Stationary penalty
        if (criteria.stationaryPenaltyPerSec != 0f)
        {
            var stat = ScriptableObject.CreateInstance<Neuro.Creature.Evaluation.StationaryPenaltyRule>();
            stat.enabled = true;
            stat.weight = 1f;
            stat.rewardMultiplier = 1f;
            stat.penaltyPerSecond = criteria.stationaryPenaltyPerSec;
            stat.thresholdSeconds = criteria.stationaryThresholdSeconds;
            stat.movementEpsilon = criteria.stationaryMovementEpsilon;
            profile.rules.Add(stat);
        }

        return profile;
    }
}
}