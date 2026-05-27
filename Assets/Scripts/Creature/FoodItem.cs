using UnityEngine;

namespace Neuro.Creature{
public class FoodItem : MonoBehaviour
{
    [Header("エサの設定")]
    public float healAmount = 40f;
    public GameObject eatEffectPrefab; // 食べた時のパーティクル演出（あれば）

    // Triggerコライダーに何かが侵入した瞬間に実行されるUnityの基本関数
    void OnTriggerEnter2D(Collider2D other)
    {
        // 触れた相手、またはその親オブジェクトに「CreatureAgent」がついているか確認
        // （手足に触れた場合でも、親をたどって本体のAgentを見つけられます）
        CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();

        if (agent != null)
        {
            // 1. クリーチャーの回復APIを直接呼び出す（通知）
            agent.HealHP(healAmount);

            // 2. 食べた演出（パーティクル）をその場に生成
            if (eatEffectPrefab != null)
            {
                Instantiate(eatEffectPrefab, transform.position, Quaternion.identity);
            }

            // 3. エサ自身を消去する
            Destroy(gameObject);
        }
    }
}
}