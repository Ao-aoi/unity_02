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
    [Tooltip("食べ物で蓄積される満腹値の最大値（調整用）")]
    public float maxSatiety = 100f;
    [Tooltip("満腹値の減少速度（1秒あたり）")]
    public float satietyDecayRate = 10f;
    [Tooltip("満腹による最大消費削減割合（0〜1）。例: 0.8 -> 最大で消費が20%になる）")]
    [Range(0f, 1f)] public float maxConsumptionReduction = 0.8f;

    private float currentSatiety = 0f;

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
        // 食後の満腹効果で消費が軽減される（満腹値に応じて割合を下げる）
        float satietyRatio = (maxSatiety > 0f) ? Mathf.Clamp01(currentSatiety / maxSatiety) : 0f;
        float consumptionMultiplier = 1f - satietyRatio * maxConsumptionReduction;

        currentHP -= Time.deltaTime * hungerSpeed * consumptionMultiplier;
        currentHP -= pendingEnergyConsumption * consumptionMultiplier;
        pendingEnergyConsumption = 0f; 

        // 満腹値を時間経過で減少させる
        if (currentSatiety > 0f)
        {
            currentSatiety = Mathf.Max(0f, currentSatiety - satietyDecayRate * Time.deltaTime);
        }

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

    // 🐣 【神の多関節パーツ生成・完全版】
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
            myGenome = new CreatureGenome(200);
        }

        List<HingeJoint2D> spawnedJoints = new List<HingeJoint2D>();

        // 🧬 遺伝子を読み込んで多関節の足を生やす
        if (armPrefab != null && bodyRb != null)
        {
            int armsToSpawn = myGenome.armCount;
            int segmentsPerArm = myGenome.jointsPerArm; 
            float angleStep = 360f / armsToSpawn;

            for (int i = 0; i < armsToSpawn; i++)
            {
                // 1. 胴体から生やす根本の角度と方向（ベクトル）を計算
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 anchorDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 armDirection = new Vector3(anchorDirection.x, anchorDirection.y, 0f);
                Quaternion armRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);

                // 💡 足ごとに毎回、接続先を「胴体」の位置と情報に初期化リセット！
                Rigidbody2D previousConnectedBody = bodyRb;
                Vector3 lastSpawnPos = bodyRb.transform.position;
                Transform currentParent = bodyRb.transform;

                // 💡 1本の足の中で関節の数だけ外側に向かって直列に生やすループ
                for (int j = 0; j < segmentsPerArm; j++)
                {
                    // 根本からj番目のパーツの生成位置を計算（外側にどんどんズラす）
                    // 1つ目の腕の長さが0.2の場合、spawnRadiusの数値を調整するか、ここで直接0.2fなどをかけても綺麗に繋がります
                    Vector3 spawnPos = lastSpawnPos + armDirection * spawnRadius;

                    GameObject armObj = Instantiate(armPrefab, spawnPos, armRotation, currentParent);
                    
                    HingeJoint2D joint = armObj.GetComponent<HingeJoint2D>();
                    Rigidbody2D armRb = armObj.GetComponent<Rigidbody2D>();

                    if (joint != null && previousConnectedBody != null)
                    {
                        joint.autoConfigureConnectedAnchor = false;

                        joint.connectedBody = previousConnectedBody;
                        if (j == 0)
                        {
                            joint.connectedAnchor = anchorDirection * spawnRadius;
                        }

                        spawnedJoints.Add(joint);     
                    }

                    // 次のループのために「1つ前のパーツ」の情報に自分をセット
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

        // 🧠 脳みその自動再構築
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

    // 食べ物を摂取した際のAPI: HP回復に加えて満腹値を蓄積する
    public void ApplyFood(float amount)
    {
        HealHP(amount);
        currentSatiety = Mathf.Min(maxSatiety, currentSatiety + amount);
    }

    public Slider InitializeUIFollow(Canvas sliderCanvas)
    {
        if (uiFollow != null)
        {
            return uiFollow.InitializeSlider(maxHP, sliderCanvas);
        }
        return null;
    }

    public void AddJointSegment()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.jointsPerArm >= 4) return;

        currentGenome.jointsPerArm++;
        RebuildBody(currentGenome);
    }

    public void RemoveJointSegment()
    {
        CreatureGenome currentGenome = GetGenome();
        if (currentGenome == null || currentGenome.jointsPerArm <= 1) return;

        currentGenome.jointsPerArm--;
        RebuildBody(currentGenome);
    }

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