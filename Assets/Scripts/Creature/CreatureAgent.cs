using UnityEngine;
using UnityEngine.UI;

public class CreatureAgent : MonoBehaviour
{
    [Header("ステータス")]
    public float maxHP = 100f;
    private float currentHP;
    public float hungerSpeed = 8f; // 毎秒減るHP
    // 動いた量に応じた消費設定（角度あたりの消費量）
    [Tooltip("1 度あたりに消費するHP量")] public float energyPerDegree = 0.02f;
    [Tooltip("ノイズ対策として無視する最小角度変化（度）")] public float movementThresholdDegrees = 0.1f;
    [Header("参照")]
    [SerializeField] private CreatureUIFollow uiFollow; // 同一オブジェクトにあるUI追従クラスの参照

    private HingeJoint2D[] joints;
    private float[] prevJointAngles;
    private EcosystemManager manager;

    // マネージャーから誕生時に呼ばれてセットされる関数
    public void SetupAgent(EcosystemManager ecosystemManager)
    {
        manager = ecosystemManager;
    }

    void Start()
    {
        currentHP = maxHP;
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

    void FixedUpdate()
    {
        // ランダム運動（フェーズ1の処理）
        float totalMovementDegrees = 0f;
        if (joints != null)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                HingeJoint2D joint = joints[i];
                if (joint != null && joint.useMotor)
                {
                    JointMotor2D motor = joint.motor;
                    motor.motorSpeed = Random.Range(-300f, 300f);
                    motor.maxMotorTorque = 100f;
                    joint.motor = motor;
                }

                if (joint != null)
                {
                    float currentAngle = joint.jointAngle;
                    float prevAngle = (prevJointAngles != null && i < prevJointAngles.Length) ? prevJointAngles[i] : currentAngle;
                    float delta = Mathf.Abs(Mathf.DeltaAngle(prevAngle, currentAngle));
                    if (delta > movementThresholdDegrees) totalMovementDegrees += delta;
                    if (prevJointAngles != null && i < prevJointAngles.Length) prevJointAngles[i] = currentAngle;
                }
            }
        }

        if (totalMovementDegrees > 0f)
        {
            currentHP -= totalMovementDegrees * energyPerDegree;
        }
    }

    void Update()
    {
        // 1. お腹を空かせる計算
        currentHP -= Time.deltaTime * hungerSpeed;

        // 2. 【API】UIFollowを直接呼んで「HPバーの値を書き換えて！」と通知する
        if (uiFollow != null)
        {
            uiFollow.UpdateHPBar(currentHP);
        }

        // 3. 餓死したらマネージャーに通知して後片付けしてもらう
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

    // 【API】
    public void HealHP(float amount)
    {
        // 現在のHPに回復量を足し、最大値（maxHP）を超えないように制限する
        currentHP = Mathf.Min(currentHP + amount, maxHP);

        // 見た目担当のUIFollowに「HPが変わったよ！」と即座に通知する
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