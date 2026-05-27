using UnityEngine;
using UnityEngine.UI;

public class CreatureAgent : MonoBehaviour
{
    [Header("ステータス")]
    public float maxHP = 100f;
    private float currentHP;
    public float hungerSpeed = 8f; 
    
    [Tooltip("1 度あたりに消費するHP量")] public float energyPerDegree = 0.02f;
    [Tooltip("ノイズ対策として無視する最小角度変化（度）")] public float movementThresholdDegrees = 0.1f;
    
    [Header("参照")]
    [SerializeField] private CreatureUIFollow uiFollow; 

    private HingeJoint2D[] joints;
    private float[] prevJointAngles;
    private EcosystemManager manager;
    private CreatureBrain brain;
    private CreatureSensor sensor;

    // 運動で消費したエネルギーを一時的に溜めておく変数
    private float pendingEnergyConsumption = 0f;

    // ★ 改善点1: コンポーネントの取得と配列の初期化は Awake にまとめる
    void Awake()
    {
        sensor = GetComponent<CreatureSensor>();
        joints = GetComponentsInChildren<HingeJoint2D>();
        
        if (joints != null && joints.Length > 0)
        {
            prevJointAngles = new float[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                prevJointAngles[i] = joints[i] != null ? joints[i].jointAngle : 0f;
            }
        }
    }

    public void SetupAgent(EcosystemManager ecosystemManager)
    {
        manager = ecosystemManager;
    }

    void Start()
    {
        currentHP = maxHP;

        // マネージャーからInitializeBrainが呼ばれなかった場合（テスト時など）の安全策
        if (brain == null)
        {
            InitializeBrain(null);
        }
    }

    public void InitializeBrain(CreatureGenome inheritedGenome = null)
    {
        int inputCount = 3;
        int outputCount = joints != null ? joints.Length : 0;
        
        brain = new CreatureBrain(inputCount, outputCount);

        if (inheritedGenome != null)
        {
            brain.LoadGenome(inheritedGenome);
        }
    }

    void FixedUpdate()
    {
        float totalMovementDegrees = 0f;

        if (brain != null && sensor != null && joints != null)
        {
            // 1. インプット（目）
            float[] inputs = new float[3];
            inputs[0] = sensor.dirToClosestFood.x;      
            inputs[1] = sensor.dirToClosestFood.y;      
            inputs[2] = sensor.distanceToClosestFood; 

            // 2. 計算（脳）
            float[] outputs = brain.Evaluate(inputs);

            // 3. アウトプット（筋肉）
            for (int i = 0; i < joints.Length; i++)
            {
                HingeJoint2D joint = joints[i];
                if (joint != null && joint.useMotor && i < outputs.Length)
                {
                    JointMotor2D motor = joint.motor;
                    motor.motorSpeed = outputs[i] * 300f;
                    motor.maxMotorTorque = 100f;
                    joint.motor = motor;

                    // 運動量の計算
                    float currentAngle = joint.jointAngle;
                    float prevAngle = prevJointAngles[i];
                    float delta = Mathf.Abs(Mathf.DeltaAngle(prevAngle, currentAngle));
                    
                    if (delta > movementThresholdDegrees)
                    {
                        totalMovementDegrees += delta;
                    }

                    prevJointAngles[i] = currentAngle;
                }
            }
        }

        // ★ 改善点2: FixedUpdate ではHPを直接減らさず、消費予定として溜めるだけにする
        if (totalMovementDegrees > 0f)
        {
            pendingEnergyConsumption += totalMovementDegrees * energyPerDegree;
        }
    }

    void Update()
    {
        // ★ 改善点2: Updateのタイミングで、時間経過の空腹と、運動で消費したカロリーをまとめて引き算する
        currentHP -= Time.deltaTime * hungerSpeed;
        currentHP -= pendingEnergyConsumption;
        pendingEnergyConsumption = 0f; // 引いたらリセット

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