using UnityEngine;

public class CreatureMotorTest : MonoBehaviour
{
    // 子ノードにあるすべての関節を自動で入れるための配列
    private HingeJoint2D[] joints;

    // モーターの動く強さ（スマホの電池消費や難易度に合わせて調整）
    public float motorPower = 300f; 

    void Start()
    {
        // 自分（胴体）の子ノードにある手足のHingeJoint2Dをすべて自動で回収する
        joints = GetComponentsInChildren<HingeJoint2D>();
    }

    void FixedUpdate()
    {
        // 物理演算のタイミング（FixedUpdate）で、すべての関節をランダムに動かす
        foreach (HingeJoint2D joint in joints)
        {
            if (joint != null && joint.useMotor)
            {
                // 現在のモーター設定を取り出す
                JointMotor2D motor = joint.motor;

                // -300 〜 +300 の間でランダムな目標速度を決める
                motor.motorSpeed = Random.Range(-motorPower, motorPower);

                // モーターが発揮できる最大の力
                motor.maxMotorTorque = 100f; 

                // 変更したモーター設定を関節に上書きして適用する
                joint.motor = motor;
            }
        }
    }
}