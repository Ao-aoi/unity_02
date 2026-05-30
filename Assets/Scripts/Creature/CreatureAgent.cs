using UnityEngine;
using UnityEngine.UI;
using Neuro.Creature.Evaluation;
using Neuro.Creature.EvolutionBuild;
using Neuro.Creature.Environment;
using System.Collections.Generic;

namespace Neuro.Creature{   
public class CreatureAgent : MonoBehaviour
{
    public event System.Action<CreatureAgent, CreatureGenome, float> Died;

    [Header("ステータス")]
    public float maxHP = 100f;
    private float currentHP;
    public float hungerSpeed = 8f; 
    [Tooltip("1 度あたりに消費するHP量")] public float energyPerDegree = 0.02f;
    [Tooltip("ノイズ対策として無視する最小角度変化（度）")] public float movementThresholdDegrees = 0.1f;

    [Header("食事バフ")]
    public float maxSatiety = 100f;
    public float satietyDecayRate = 10f;
    [Range(0f, 1f)] public float maxConsumptionReduction = 0.8f;
    private float currentSatiety = 0f;

    [Header("動的パーツ生成の設定")]
    public GameObject armPrefab;
    public float spawnRadius = 1.0f;

    [Header("見た目の設定（インスペクターで割り当て）")]
    public List<SpriteRenderer> bodySpriteRenderers = new List<SpriteRenderer>();
    public List<SpriteRenderer> faceSpriteRenderers = new List<SpriteRenderer>();

    [Header("参照")]
    [SerializeField] private CreatureUIFollow uiFollow; 

    private HingeJoint2D[] joints;
    private float[] prevJointAngles;
    private bool isDying;
    private CustomSpawnPoint originSpawnPoint;
    private CreatureBrain brain;
    private CreatureSensor sensor;

    private float pendingEnergyConsumption = 0f;
    private float totalEnergyConsumed = 0f;
    private float totalHpConsumed = 0f;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float CurrentSatiety => currentSatiety;
    public float TotalEnergyConsumed => totalEnergyConsumed;
    public float TotalHpConsumed => totalHpConsumed;
    [Header("突然変異設定")]
    [Tooltip("重み（ウェイト）変異の確率（0..1） - 高めで良い（例: 0.1）")]
    public float weightMutationRate = 0.1f;
    [Tooltip("重み変化量の最大値（ランダムに±する）")]
    public float weightMutationAmount = 0.2f;

    [System.Serializable]
    public class MutationConfig
    {
        [Header("構造変異の基本確率（個体ごと）")]
        public float structuralMutationProb = 0.03f; // 個体単位で構造変異が発生する確率

        [Header("腕の変化確率（ベース）")]
        public float armAddBase = 0.005f;
        public float armRemoveBase = 0.002f;

        [Header("関節の変化確率（ベース）")]
        public float jointAddBase = 0.01f;
        public float jointRemoveBase = 0.005f;

        [Header("中間ノード（隠れノード）の変化確率（ベース）")]
        public float nodeAddBase = 0.02f;
        public float nodeRemoveBase = 0.01f;

        [Header("感覚の変化確率（ベース）")]
        public float sightRangeChangeBase = 0.02f;
        public float fieldOfViewChangeBase = 0.02f;

        [Header("ポアソン分布のλ（1回の個体変異で変える期待個数）")]
        public float poissonLambda = 0.2f;

        [Header("隠れノードの範囲（下限はコード上でも保証）")]
        public int minHiddenNodes = 2;
        public int maxHiddenNodes = 24;
    }

    // デフォルト設定（外部から参照可）
    public static readonly MutationConfig DefaultMutationConfig = new MutationConfig();
    
    // 現在の血統の色を記憶しておく変数
    [HideInInspector] public Color currentBodyArmColor = Color.white;
    [HideInInspector] public Color currentFaceColor = Color.white;
    [Header("系統情報")]
    [SerializeField] private LineageData lineage = new LineageData("Wild Lineage", Color.white);
    public string lineageId;
    public string parentLineageId;
    public string lineageName;
    public Color lineageColor = Color.white;
    public LineageData Lineage => lineage;

    void Awake()
    {
        sensor = GetComponent<CreatureSensor>();
        if (GetComponent<CreatureEnvironmentTracker>() == null)
            gameObject.AddComponent<CreatureEnvironmentTracker>();
    }

    void OnEnable()
    {
        CreatureRegistry.Register(this);
    }

    void OnDisable()
    {
        CreatureRegistry.Unregister(this);
    }

    public void SetOriginSpawnPoint(CustomSpawnPoint spawnPoint)
    {
        originSpawnPoint = spawnPoint;
    }

    // 🎨 【新要素】マネージャーから色を受け取って適用する関数
    public void SetLineage(LineageData newLineage)
    {
        if (newLineage == null)
            return;

        lineage = newLineage.CloneForChild();
        lineageId = lineage.LineageId;
        parentLineageId = lineage.parentLineageId;
        lineageName = lineage.lineageName;
        lineageColor = lineage.lineageColor;
        if (lineageColor != Color.clear)
            SetColors(lineageColor, currentFaceColor);

        CreatureGenome genome = GetGenome();
        if (genome != null)
            ApplyLineageToGenome(genome);
    }

    private void ApplyLineageToGenome(CreatureGenome genome)
    {
        if (genome == null)
            return;

        genome.lineageId = lineageId;
        genome.parentLineageId = parentLineageId;
        genome.lineageName = lineageName;
    }

    public void ApplyDamage(float amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);
        if (uiFollow != null) uiFollow.UpdateHPBar(currentHP);
    }

    public SavedCreatureData SaveToArchive(CreatureArchive archive, string displayName = null)
    {
        CreatureEvaluator evaluator = GetComponent<CreatureEvaluator>();
        if (archive != null)
            return archive.SaveRuntimeCreature(this, evaluator, displayName);

        SavedCreatureData data = ScriptableObject.CreateInstance<SavedCreatureData>();
        data.CaptureFromAgent(this, evaluator, displayName);
        return data;
    }

    public void SetColors(Color bodyColor, Color faceColor)
    {
        currentBodyArmColor = bodyColor;
        currentFaceColor = faceColor;
        ApplyColor(bodySpriteRenderers, currentBodyArmColor);
        ApplyColor(faceSpriteRenderers, currentFaceColor);
    }

    private void ApplyColor(List<SpriteRenderer> renderers, Color color)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer != null)
                renderer.color = color;
        }
    }

    void Start()
    {
        currentHP = maxHP;
        if (brain == null) InitializeBrain(null);
    }

    void FixedUpdate()
    {
        float totalMovementDegrees = 0f;

        if (brain != null && sensor != null && joints != null)
        {
            int sectorCount = (sensor != null) ? Mathf.Max(1, sensor.sectorCount) : 8;
            int totalInputs = 3 + sectorCount + 2; // food(x,y,d) + sectors + forward(x,y)
            float[] inputs = new float[totalInputs];
            int idx = 0;
            inputs[idx++] = sensor.dirToClosestFood.x;
            inputs[idx++] = sensor.dirToClosestFood.y;
            inputs[idx++] = sensor.distanceToClosestFood;

            // セクター距離を順に埋める（見つからない = 1f）
            if (sensor.sectorEnvironmentDistances != null)
            {
                for (int s = 0; s < sectorCount && s < sensor.sectorEnvironmentDistances.Length; s++)
                    inputs[idx++] = sensor.sectorEnvironmentDistances[s];
            }
            else
            {
                for (int s = 0; s < sectorCount; s++) inputs[idx++] = 1f;
            }

            inputs[idx++] = transform.up.x;
            inputs[idx++] = transform.up.y;

            float[] outputs = brain.Evaluate(inputs);

            for (int i = 0; i < joints.Length; i++)
            {
                HingeJoint2D joint = joints[i];
                if (joint != null && joint.useMotor && i < outputs.Length)
                {
                    JointMotor2D motor = joint.motor;
                    motor.motorSpeed = outputs[i] * 300f; 
                    motor.maxMotorTorque = 100f;
                    joint.motor = motor;

                    float currentAngle = joint.jointAngle;
                    float prevAngle = (prevJointAngles != null && i < prevJointAngles.Length) ? prevJointAngles[i] : currentAngle;
                    float delta = Mathf.Abs(Mathf.DeltaAngle(prevAngle, currentAngle));
                    
                    if (delta > movementThresholdDegrees)
                        totalMovementDegrees += delta;

                    if (prevJointAngles != null && i < prevJointAngles.Length)
                        prevJointAngles[i] = currentAngle;
                }
            }
        }

        if (totalMovementDegrees > 0f)
            pendingEnergyConsumption += totalMovementDegrees * energyPerDegree;
    }

    void Update()
    {
        float satietyRatio = (maxSatiety > 0f) ? Mathf.Clamp01(currentSatiety / maxSatiety) : 0f;
        float consumptionMultiplier = 1f - satietyRatio * maxConsumptionReduction;

        float hungerConsumption = Time.deltaTime * hungerSpeed * consumptionMultiplier;
        float movementConsumption = pendingEnergyConsumption * consumptionMultiplier;
        currentHP -= hungerConsumption;
        currentHP -= movementConsumption;
        totalHpConsumed += hungerConsumption + movementConsumption;
        totalEnergyConsumed += movementConsumption;
        pendingEnergyConsumption = 0f; 

        if (currentSatiety > 0f)
            currentSatiety = Mathf.Max(0f, currentSatiety - satietyDecayRate * Time.deltaTime);

        if (uiFollow != null) uiFollow.UpdateHPBar(currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDying)
            return;

        isDying = true;

        if (uiFollow != null)
            uiFollow.DestroyHPBar();

        CreatureGenome deadGenome = GetGenome();
        float finalFitness = 0f;
        CreatureEvaluator evaluator = GetComponent<CreatureEvaluator>();
        if (evaluator != null)
            finalFitness = evaluator.totalFitness;

        EcosystemManager.ReportCreatureDied(finalFitness);

        if (originSpawnPoint != null)
            Died?.Invoke(this, deadGenome, finalFitness);

        Destroy(gameObject);
    }

    public void InitializeBrain(CreatureGenome inheritedGenome = null)
    {
        Rigidbody2D bodyRb = GetComponent<Rigidbody2D>();
        if (bodyRb == null) bodyRb = GetComponentInChildren<Rigidbody2D>();
        if (sensor == null) sensor = GetComponent<CreatureSensor>();

        CreatureGenome myGenome = inheritedGenome;
        if (myGenome == null)
        {
            int sensorSectors = (sensor != null) ? Mathf.Max(1, sensor.sectorCount) : 8;
            int initialInputCount = 3 + sensorSectors + 2; // food(x,y,d) + sector distances + forward(x,y)
            int initialExpectedWeights = (initialInputCount * 12) + (12 * 4);
            myGenome = new CreatureGenome(initialExpectedWeights);
        }

        if (sensor != null)
        {
            sensor.sightRange = myGenome.sightRange;
            sensor.fieldOfViewAngle = myGenome.fieldOfViewAngle;
        }

        List<HingeJoint2D> spawnedJoints = new List<HingeJoint2D>();

        if (armPrefab != null && bodyRb != null)
        {
            int armsToSpawn = myGenome.armCount;
            int segmentsPerArm = myGenome.jointsPerArm; 
            float angleStep = 360f / armsToSpawn;

            for (int i = 0; i < armsToSpawn; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 anchorDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 armDirection = new Vector3(anchorDirection.x, anchorDirection.y, 0f);
                Quaternion armRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);

                Rigidbody2D previousConnectedBody = bodyRb;
                Vector3 lastSpawnPos = bodyRb.transform.position;
                Transform currentParent = bodyRb.transform;

                for (int j = 0; j < segmentsPerArm; j++)
                {
                    Vector3 spawnPos = lastSpawnPos + armDirection * spawnRadius;
                    GameObject armObj = Instantiate(armPrefab, spawnPos, armRotation, currentParent);
                    
                    // 🎨 【新要素】動的に生成した腕の色を、血統の色に染める！
                    SpriteRenderer armRenderer = armObj.GetComponent<SpriteRenderer>();
                    if (armRenderer != null) armRenderer.color = currentBodyArmColor;

                    HingeJoint2D joint = armObj.GetComponent<HingeJoint2D>();
                    Rigidbody2D armRb = armObj.GetComponent<Rigidbody2D>();

                    if (joint != null && previousConnectedBody != null)
                    {
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedBody = previousConnectedBody;
                        if (j == 0) joint.connectedAnchor = anchorDirection * spawnRadius;

                        spawnedJoints.Add(joint);     
                    }

                    previousConnectedBody = armRb;
                    lastSpawnPos = spawnPos;
                    if (armRb != null) currentParent = armRb.transform;
                }
            }
        }

        joints = spawnedJoints.ToArray();

        if (joints != null && joints.Length > 0)
        {
            prevJointAngles = new float[joints.Length];
            for (int i = 0; i < joints.Length; i++)
                prevJointAngles[i] = joints[i] != null ? joints[i].jointAngle : 0f;
        }

        int inputCount = 3 + ((sensor != null) ? Mathf.Max(1, sensor.sectorCount) : 8) + 2;            
        int outputCount = joints.Length; 
        brain = new CreatureBrain(inputCount, outputCount, myGenome.hiddenNodeCount);
        brain.LoadGenome(myGenome);
        ApplyLineageToGenome(brain.GetGenome());
    }

    public CreatureGenome GetGenome() => brain?.GetGenome();

    public void HealHP(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (uiFollow != null) uiFollow.UpdateHPBar(currentHP);
    }

    public void ApplyFood(float amount)
    {
        HealHP(amount);
        currentSatiety = Mathf.Min(maxSatiety, currentSatiety + amount);
    }

    public Slider InitializeUIFollow(Canvas sliderCanvas)
    {
        if (uiFollow != null) return uiFollow.InitializeSlider(maxHP, sliderCanvas);
        return null;
    }

    public void AddJointSegment()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.jointsPerArm >= CreatureLimits.MaxJointsPerArm) return;
        currentGenome.jointsPerArm++;
        RebuildBody(currentGenome);
    }

    public void RemoveJointSegment()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.jointsPerArm <= CreatureLimits.MinJointsPerArm) return;
        currentGenome.jointsPerArm--;
        RebuildBody(currentGenome);
    }

    public void AddArm()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.armCount >= CreatureLimits.MaxArms) return;
        currentGenome.armCount++;
        RebuildBody(currentGenome);
    }

    public void RemoveArm()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.armCount <= CreatureLimits.MinArms) return;
        currentGenome.armCount--;
        RebuildBody(currentGenome);
    }

    public void UpgradeSightRange(float amount)
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || sensor == null) return;
        currentGenome.sightRange = Mathf.Clamp(currentGenome.sightRange + amount, 2f, 15f);
        sensor.sightRange = currentGenome.sightRange;
    }

    public void UpgradeFieldOfView(float amount)
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || sensor == null) return;
        currentGenome.fieldOfViewAngle = Mathf.Clamp(currentGenome.fieldOfViewAngle + amount, 30f, 180f);
        sensor.fieldOfViewAngle = currentGenome.fieldOfViewAngle;
    }

    private void RebuildBody(CreatureGenome newGenome)
    {
        if (joints != null)
        {
            foreach (HingeJoint2D joint in joints)
                if (joint != null) Destroy(joint.gameObject);
        }
        InitializeBrain(newGenome);
    }
    
    public void UpgradeBrainNodes()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null) return;

        if (currentGenome.hiddenNodeCount >= 24) return;

        currentGenome.hiddenNodeCount += 2; 

        int inputCount = 3 + ((sensor != null) ? Mathf.Max(1, sensor.sectorCount) : 8) + 2;
        int outputCount = (joints != null) ? joints.Length : 0;

        brain = new CreatureBrain(inputCount, outputCount, currentGenome.hiddenNodeCount);
        brain.LoadGenome(currentGenome);
    }

    // --- 詳細な突然変異ロジック（重み + 構造変異） ---
    public static CreatureGenome ApplyDetailedMutation(CreatureGenome parentGenome, float weightMutRate, float weightMutAmount, MutationConfig config = null)
    {
        if (parentGenome == null) return null;
        if (config == null) config = DefaultMutationConfig;

        CreatureGenome child = parentGenome.Clone();

        // 1) 重みの変異
        if (child.weights != null && child.weights.Length > 0)
        {
            for (int i = 0; i < child.weights.Length; i++)
            {
                if (Random.value < weightMutRate)
                {
                    child.weights[i] += Random.Range(-weightMutAmount, weightMutAmount);
                    child.weights[i] = Mathf.Clamp(child.weights[i], -1f, 1f);
                }
            }
        }

        // 2) 構造変異（個体単位でまず発生判定）
        if (Random.value < config.structuralMutationProb)
        {
            // 腕の増減
            int armChanges = SamplePoisson(config.poissonLambda);
            for (int k = 0; k < Mathf.Max(1, armChanges); k++)
            {
                float addP = config.armAddBase * (1f - (child.armCount / (float)CreatureLimits.MaxArms));
                if (Random.value < addP)
                    child.armCount = Mathf.Clamp(child.armCount + 1, CreatureLimits.MinArms, CreatureLimits.MaxArms);

                float remP = config.armRemoveBase * (child.armCount / (float)CreatureLimits.MaxArms);
                if (Random.value < remP)
                    child.armCount = Mathf.Clamp(child.armCount - 1, CreatureLimits.MinArms, CreatureLimits.MaxArms);
            }

            // 関節の増減（各腕の関節数）
            int jointChanges = SamplePoisson(config.poissonLambda);
            for (int k = 0; k < Mathf.Max(1, jointChanges); k++)
            {
                float addP = config.jointAddBase * (1f - (child.jointsPerArm / (float)CreatureLimits.MaxJointsPerArm));
                if (Random.value < addP)
                    child.jointsPerArm = Mathf.Clamp(child.jointsPerArm + 1, CreatureLimits.MinJointsPerArm, CreatureLimits.MaxJointsPerArm);

                float remP = config.jointRemoveBase * (child.jointsPerArm / (float)CreatureLimits.MaxJointsPerArm);
                if (Random.value < remP)
                    child.jointsPerArm = Mathf.Clamp(child.jointsPerArm - 1, CreatureLimits.MinJointsPerArm, CreatureLimits.MaxJointsPerArm);
            }

            // 中間ノード（隠れノード）の増減
            int nodeChanges = SamplePoisson(config.poissonLambda);
            for (int k = 0; k < Mathf.Max(1, nodeChanges); k++)
            {
                float addP = config.nodeAddBase * (1f - (child.hiddenNodeCount / (float)config.maxHiddenNodes));
                if (Random.value < addP)
                    child.hiddenNodeCount = Mathf.Clamp(child.hiddenNodeCount + 1, config.minHiddenNodes, config.maxHiddenNodes);

                float remP = config.nodeRemoveBase * (child.hiddenNodeCount / (float)config.maxHiddenNodes);
                if (Random.value < remP)
                    child.hiddenNodeCount = Mathf.Clamp(child.hiddenNodeCount - 1, config.minHiddenNodes, config.maxHiddenNodes);
            }
        }

        if (Random.value < config.sightRangeChangeBase)
            child.sightRange = Mathf.Clamp(child.sightRange + Random.Range(-0.5f, 0.5f), 2f, 15f);

        if (Random.value < config.fieldOfViewChangeBase)
            child.fieldOfViewAngle = Mathf.Clamp(child.fieldOfViewAngle + Random.Range(-10f, 10f), 30f, 180f);

        return child;
    }

    // λを受け取りポアソン乱数を返す（小さいλ向け）
    private static int SamplePoisson(float lambda)
    {
        if (lambda <= 0f) return 0;
        float L = Mathf.Exp(-lambda);
        int k = 0;
        float p = 1f;
        do
        {
            k++;
            p *= Random.value;
        } while (p > L && k < 100);
        return k - 1;
    }
}
}