using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Neuro.Creature{   
public class CreatureAgent : MonoBehaviour
{
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

    [Header("参照")]
    [SerializeField] private CreatureUIFollow uiFollow; 

    private HingeJoint2D[] joints;
    private float[] prevJointAngles;
    private EcosystemManager manager;
    private CreatureBrain brain;
    private CreatureSensor sensor;

    private float pendingEnergyConsumption = 0f;

    void Awake()
    {
        sensor = GetComponent<CreatureSensor>();
    }

    public void SetupAgent(EcosystemManager ecosystemManager)
    {
        manager = ecosystemManager;
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
            // 💡 【脳への入力アーキテクチャの拡張】：計5つの電気信号を脳に送る！
            float[] inputs = new float[5];
            inputs[0] = sensor.dirToClosestFood.x;      // 1. エサの方向X
            inputs[1] = sensor.dirToClosestFood.y;      // 2. エサの方向Y
            inputs[2] = sensor.distanceToClosestFood;   // 3. エサへの距離
            
            // 🧠 【新・三半規管ジャイロ】自分の正面（transform.up）のワールド向きベクトルを渡す！
            // これにより、脳は「今自分がどっちにどれくらい傾いているか」を100%自覚できるっす！
            inputs[3] = transform.up.x;                 // 4. 自分の傾きX
            inputs[4] = transform.up.y;                 // 5. 自分の傾きY

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

        currentHP -= Time.deltaTime * hungerSpeed * consumptionMultiplier;
        currentHP -= pendingEnergyConsumption * consumptionMultiplier;
        pendingEnergyConsumption = 0f; 

        if (currentSatiety > 0f)
            currentSatiety = Mathf.Max(0f, currentSatiety - satietyDecayRate * Time.deltaTime);

        if (uiFollow != null) uiFollow.UpdateHPBar(currentHP);

        if (currentHP <= 0)
        {
            if (manager != null) manager.OnCreatureDied(this.gameObject);
            else Destroy(gameObject);
        }
    }

    public void InitializeBrain(CreatureGenome inheritedGenome = null)
    {
        Rigidbody2D bodyRb = GetComponent<Rigidbody2D>();
        if (bodyRb == null) bodyRb = GetComponentInChildren<Rigidbody2D>();
        if (sensor == null) sensor = GetComponent<CreatureSensor>();

        CreatureGenome myGenome = inheritedGenome;
        if (myGenome == null)
        {
            // 💡 多層化に伴い必要となる、大きめの初期サイズでゲノムを自動生成
            int initialExpectedWeights = (5 * 12) + (12 * 4); // 入力5, 隠れ12, 初期想定関節4本想定
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

        int inputCount = 5;            
        int outputCount = joints.Length; 
        brain = new CreatureBrain(inputCount, outputCount, myGenome.hiddenNodeCount);
        brain.LoadGenome(myGenome);
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

        // 最大24ノードまで強化可能にする
        if (currentGenome.hiddenNodeCount >= 24) return;

        currentGenome.hiddenNodeCount += 2; // 1回につき2ノードずつ脳細胞を増やす

        // 脳のアップグレードは身体パーツを触らずに実行する。
        // 現在の関節数（出力数）を維持したまま、新しい隠れノード数で脳のみ再生成する。
        int inputCount = 5;
        int outputCount = (joints != null) ? joints.Length : 0;

        brain = new CreatureBrain(inputCount, outputCount, currentGenome.hiddenNodeCount);
        brain.LoadGenome(currentGenome);
    }
}
}