using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // リストを使うために追加

namespace Neuro.Creature{   
public class CreatureAgent : MonoBehaviour
{
    [Header("ステータス")]
    public float maxHP = 100f;
    private float currentHP;
    public float hungerSpeed = 8f; // 毎秒減るHP
    // 動いた量に応じた消費設定（角度あたりの消費量）
    [Tooltip("1 度あたりに消費するHP量")] public float energyPerDegree = 0.02f;
    [Tooltip("ノイズ対策として無視する最小角度変化（度）")] public float movementThresholdDegrees = 0.1f;

    [Header("動的パーツ生成の設定")]
    [Tooltip("生成する手足（パーツ）のプレハブを割り当ててください")]
    public GameObject armPrefab;
    [Tooltip("胴体からどれくらい離れた位置に手足を生成するか")]
    public float spawnRadius = 1.0f;

    [Header("参照")]
    [SerializeField] private CreatureUIFollow uiFollow; // 同一オブジェクトにあるUI追従クラスの参照

    private HingeJoint2D[] joints;
    private float[] prevJointAngles;
    private EcosystemManager manager;
    private CreatureBrain brain;
    private CreatureSensor sensor;

    // 運動で消費したエネルギーを一時的に溜めておく変数
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

        // マネージャーを経由せず、直接エディタ上に置いてテスト再生したときの安全策
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
            // 1. 目のセンサーの【実際のパブリック変数】から「インプット（状態）」を直接もらうっす！
            float[] inputs = new float[3];
            inputs[0] = sensor.dirToClosestFood.x;      // エサのローカル方向X
            inputs[1] = sensor.dirToClosestFood.y;      // エサのローカル方向Y
            inputs[2] = sensor.distanceToClosestFood; // エサへの距離 (0〜1)

            // 2. 計算（脳）
            float[] outputs = brain.Evaluate(inputs);

            // 3. アウトプット（筋肉）と、復活させた角度消費エネルギー計算！
            for (int i = 0; i < joints.Length; i++)
            {
                HingeJoint2D joint = joints[i];
                if (joint != null && joint.useMotor && i < outputs.Length)
                {
                    JointMotor2D motor = joint.motor;
                    motor.motorSpeed = outputs[i] * 300f; // 脳の出力（-1〜+1）を速度にマッピング
                    motor.maxMotorTorque = 100f;
                    joint.motor = motor;

                    // 🔥 大復活！関節が実際に動いた角度（エネルギー消費）の計算
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

        // 運動量に応じたHP消費予定を溜める
        if (totalMovementDegrees > 0f)
        {
            pendingEnergyConsumption += totalMovementDegrees * energyPerDegree;
        }
    }

    void Update()
    {
        // Updateのタイミングで、時間経過の空腹と、運動で消費したカロリーをまとめて引き算
        currentHP -= Time.deltaTime * hungerSpeed;
        currentHP -= pendingEnergyConsumption;
        pendingEnergyConsumption = 0f; // 引いたらリreset

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

    // 🐣 【神の動的パーツ生成】誕生時にマネージャーから呼ばれる身体と脳の初期化
    public void InitializeBrain(CreatureGenome inheritedGenome = null)
    {
        // まず、このオブジェクト自身に Rigidbody2D があるか探す（なければ子オブジェクト内を探索）
        Rigidbody2D bodyRb = GetComponent<Rigidbody2D>();
        if (bodyRb == null)
        {
            bodyRb = GetComponentInChildren<Rigidbody2D>();
        }

        // デバッグ用: armPrefab または Rigidbody2D が未設定だと手足は生成されないので警告を出す
        if (armPrefab == null)
        {
            Debug.LogWarning($"CreatureAgent on '{gameObject.name}': armPrefab is not assigned. Arms will not be generated.");
        }
        if (bodyRb == null)
        {
            Debug.LogWarning($"CreatureAgent on '{gameObject.name}': Rigidbody2D is missing on body or its children. Arms require a Rigidbody2D to connect to.");
        }

        // 1. 遺伝子データの確定（親がいない場合は完全ランダムに新規作成）
        CreatureGenome myGenome = inheritedGenome;
        if (myGenome == null)
        {
            // 関節の数に合わせて脳のサイズが可変になるため、
            // 遺伝子配列（重み）の長さは、今回は一旦大きめに100個分作っておく
            myGenome = new CreatureGenome(100);
        }

        // 動的に生やしたジョイントを一時的に集めるためのリスト
        List<HingeJoint2D> spawnedJoints = new List<HingeJoint2D>();

        // 2. 🧬 遺伝子の armCount を見て、手足を円状に動くように生やす
        if (armPrefab != null && bodyRb != null)
        {
            int armsToSpawn = myGenome.armCount;

            for (int i = 0; i < armsToSpawn; i++)
            {
                // 胴体の周りに等間隔（円状）に配置するための角度を計算
                float angle = i * (360f / armsToSpawn) * Mathf.Deg2Rad;
                Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnRadius;
                // もし子オブジェクトに Rigidbody2D があれば、そこの座標を基準に生成し
                // 生成先の親もその子オブジェクトにする（要求どおり）
                Vector3 basePos = (bodyRb != null) ? (Vector3)bodyRb.transform.position : transform.position;
                Transform parentTransform = (bodyRb != null) ? bodyRb.transform : transform;
                Vector3 spawnPos = basePos + spawnOffset;

                // 指定された子オブジェクトを親にして手足を生成
                GameObject armObj = Instantiate(armPrefab, spawnPos, Quaternion.identity, parentTransform);
                
                // 🔗 物理ジョイント（HingeJoint2D）で胴体と手足をガッチャンコ！
                HingeJoint2D joint = armObj.GetComponent<HingeJoint2D>();
                if (joint != null)
                {
                    joint.connectedBody = bodyRb; // 親のRigidbodyと繋ぐ
                    spawnedJoints.Add(joint);     // リストに登録
                }
            }
        }

        // 3. 実際に生えた手足の数（spawnedJoints）を、配列に確定させるっす！
        joints = spawnedJoints.ToArray();

        // 角度を記憶する配列も、生えた手足のジャストサイズで初期化するっす
        if (joints != null && joints.Length > 0)
        {
            prevJointAngles = new float[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                prevJointAngles[i] = joints[i] != null ? joints[i].jointAngle : 0f;
            }
        }

        // 4. 生えた手足の数に合わせて脳みそをピッタリのサイズで組み立てる
        int inputCount = 3;            // 入力: エサ方向X, Y, 距離
        int outputCount = joints.Length; // 出力: 実際に生えた手足の数（モーター速度）

        brain = new CreatureBrain(inputCount, outputCount);
        
        // 遺伝子を脳にロード
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
}
}