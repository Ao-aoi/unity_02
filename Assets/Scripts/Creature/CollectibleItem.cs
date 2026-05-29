using UnityEngine;
using System;

namespace Neuro.Creature
{
    public class CollectibleItem : MonoBehaviour
    {
        // 💡 アイテムの種類の定義（必要に応じて増やせます）
        public enum ItemType { Generic, SpeedFragment, JumpFragment, VisionFragment }

        [Header("アイテム設定")]
        public ItemType itemType = ItemType.Generic;
        public int itemValue = 1; // 拾った時に加算される数

        [Header("アンロック設定")]
        [Tooltip("このアイテムを拾った時にプレイヤーが獲得（アンロック）する新しい評価ルールを割り当てます")]
        public ScriptableObject unlockableRule; // EvaluationRule などがセットされます

        [Header("演出")]
        public GameObject collectEffectPrefab;

        // 📢 プレイヤー（マネージャー）に「新しいルールがアンロックされたぞ！」と知らせるグローバルイベント
        public static event Action<ScriptableObject> OnRuleUnlocked;

        void OnTriggerEnter2D(Collider2D other)
        {
            // 触れた相手の親から CreatureAgent を見つける
            CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();

            if (agent != null)
            {
                // 2. もしアンロック対象のルールが設定されていれば、プレイヤーのインベントリに通知を飛ばす！
                if (unlockableRule != null)
                {
                    OnRuleUnlocked?.Invoke(unlockableRule);
                }

                // 3. エフェクトを出して消滅
                if (collectEffectPrefab != null)
                {
                    Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
            }
        }
    }
}