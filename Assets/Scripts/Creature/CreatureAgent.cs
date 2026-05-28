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

    [Header("動的パーツ生成の設定")]
    [Tooltip("生成する手足（パーツ）のプレハブを割り当ててください")]
    public GameObject armPrefab;
    [Tooltip("胴体からどれくらい離れた位置に手足を生成するか")]
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

        if (brain == null)
        {
            InitializeBrain(null);
        }
    }

    void FixedUpdate()
    {
        float totalMovementDegrees = 0f;

        if (brain != null && sensor != null && joints != null)
        {
            float[] inputs = new float[3];
            inputs[0] = sensor.dirToClosestFood.x;      
            inputs[1] = sensor.dirToClosestFood.y;      
            inputs[2] = sensor.distanceToClosestFood; 

            float[] outputs = brain.Evaluate(inputs);

            // 🧠 すべての関節モーターを脳の出力で駆動
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
                    {
                        totalMovementDegrees += delta;
                    }

                    if (prevJointAngles != null && i < prevJointAngles.Length)
                    {
                        prevJointAngles[i] = currentAngle;
                    }
                }
            }
        }

        if (totalMovementDegrees > 0f)
        {
            pendingEnergyConsumption += totalMovementDegrees * energyPerDegree;
        }
    }

    void Update()
    {
        currentHP -= Time.deltaTime * hungerSpeed;
        currentHP -= pendingEnergyConsumption;
        pendingEnergyConsumption = 0f; 

        if (uiFollow != null)
        {
            uiFollow.UpdateHPBar(currentHP);
        }

        if (currentHP <= 0)
        {
            if (manager != null)
            {
                manager.OnCreatureDied(this.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    // 🐣 【神の多関節パーツ生成】
    public void InitializeBrain(CreatureGenome inheritedGenome = null)
    {
        Rigidbody2D bodyRb = GetComponent<Rigidbody2D>();
        if (bodyRb == null)
        {
            bodyRb = GetComponentInChildren<Rigidbody2D>();
        }

        CreatureGenome myGenome = inheritedGenome;
        if (myGenome == null)
        {
            // 多関節化に伴い配線数が多くなるため、初期配列サイズを余裕を持って200にしておきます
            myGenome = new CreatureGenome(200);
        }

        List<HingeJoint2D> spawnedJoints = new List<HingeJoint2D>();

        // 🧬 遺伝子（armCount と jointsPerArm）を読み込んで多関節の足を生やす！
        if (armPrefab != null && bodyRb != null)
        {
            int armsToSpawn = myGenome.armCount;
            int segmentsPerArm = myGenome.jointsPerArm; // 1本の足の中のパーツ数

            for (int i = 0; i < armsToSpawn; i++)
            {
                // 1. 胴体から生やす根本の角度と方向を計算
                float angle = i * (360f / armsToSpawn) * Mathf.Deg2Rad;
                Vector3 armDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

                // 繋ぎ先を最初は「胴体（bodyRb）」にする
                Rigidbody2D previousConnectedBody = bodyRb;
                Vector3 lastSpawnPos = bodyRb.transform.position;
                Transform currentParent = bodyRb.transform;

                // 💡 【直列ループ】設定された関節の数だけ、外側に向かって数珠繋ぎに生やすっす！
                for (int j = 0; j < segmentsPerArm; j++)
                {
                    // 根本からj番目のパーツの生成位置を計算（どんどん外側に伸びる）
                    Vector3 spawnPos = lastSpawnPos + armDirection * spawnRadius;

                    GameObject armObj = Instantiate(armPrefab, spawnPos, Quaternion.identity, currentParent);
                    
                    HingeJoint2D joint = armObj.GetComponent<HingeJoint2D>();
                    Rigidbody2D armRb = armObj.GetComponent<Rigidbody2D>();

                    if (joint != null && previousConnectedBody != null)
                    {
                        // 🔗 1つ前のパーツのRigidbodyと自分をガッチャンコ！
                        joint.connectedBody = previousConnectedBody;
                        spawnedJoints.Add(joint);     
                    }

                    // 次のループのために「1つ前のパーツ」の情報を自分に更新する
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
            {
                prevJointAngles[i] = joints[i] != null ? joints[i].jointAngle : 0f;
            }
        }

        // 🧠 生えた【全関節の総数】に合わせて脳みそをピッタリのサイズで自動再構築！
        int inputCount = 3;            
        int outputCount = joints.Length; 

        brain = new CreatureBrain(inputCount, outputCount);
        brain.LoadGenome(myGenome);
    }

    public CreatureGenome GetGenome()
    {
        return brain?.GetGenome();
    }

    public void HealHP(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (uiFollow != null)
        {
            uiFollow.UpdateHPBar(currentHP);
        }
    }

    public Slider InitializeUIFollow(Canvas sliderCanvas)
    {
        if (uiFollow != null)
        {
            return uiFollow.InitializeSlider(maxHP, sliderCanvas);
        }
        return null;
    }

    // 🛠️ 【ショップ用API】関節（足の長さ）を1段階増やす
    public void AddJointSegment()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.jointsPerArm >= 4) return;

        currentGenome.jointsPerArm++;
        RebuildBody(currentGenome);
    }

    // 🛠️ 【ショップ用API】関節（足の長さ）を1段階減らす
    public void RemoveJointSegment()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.jointsPerArm <= 1) return;

        currentGenome.jointsPerArm--;
        RebuildBody(currentGenome);
    }

    // 🛠️ 既存のAddArm / RemoveArmも多関節を維持したまま動くように、内部でRebuildBodyを呼ぶだけでOK
    public void AddArm()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.armCount >= 8) return;
        currentGenome.armCount++;
        RebuildBody(currentGenome);
    }

    public void RemoveArm()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.armCount <= 0) return;
        currentGenome.armCount--;
        RebuildBody(currentGenome);
    }

    private void RebuildBody(CreatureGenome newGenome)
    {
        if (joints != null)
        {
            foreach (HingeJoint2D joint in joints)
            {
                if (joint != null) Destroy(joint.gameObject);
            }
        }
        InitializeBrain(newGenome);
    }
}
}