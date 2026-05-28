using UnityEngine;
using Neuro.Creature;
using Neuro.UI; // UIを操作するために参照する

namespace Neuro.Interaction
{
    public class ShopController : MonoBehaviour
    {
        [Header("UIの参照")]
        [Tooltip("画面に配置した ShopUIManager を割り当ててください")]
        public ShopUIManager shopUI; 

        [Header("ポイント設定")]
        [SerializeField] private int playerPoints = 100; // 現在のお財布
        [SerializeField] private int armCost = 20;       // 手足を増やすコスト
        [SerializeField] private int jointCost = 30;     // 関節を増やすコスト  

        private CreatureAgent currentCreature;

        void OnEnable()
        {
            // 1. クリーチャーがタップされた時のイベントを購読
            CreatureSelector.OnCreatureSelected += HandleCreatureSelected;
            
            // 2. UIのボタンが押された時のイベントを購読
            if (shopUI != null)
            {
                shopUI.OnAddArmRequested += HandleAddArm;
                shopUI.OnRemoveArmRequested += HandleRemoveArm;
                shopUI.OnAddJointRequested += HandleAddJoint;
                shopUI.OnRemoveJointRequested += HandleRemoveJoint;
            }
        }

        void OnDisable()
        {
            CreatureSelector.OnCreatureSelected -= HandleCreatureSelected;
            
            if (shopUI != null)
            {
                shopUI.OnAddArmRequested -= HandleAddArm;
                shopUI.OnRemoveArmRequested -= HandleRemoveArm;
                shopUI.OnAddJointRequested -= HandleAddJoint;
                shopUI.OnRemoveJointRequested -= HandleRemoveJoint;
            }
        }

        // 👆 タップ選択された時の処理
        private void HandleCreatureSelected(CreatureAgent agent)
        {
            currentCreature = agent;
            
            // 何もないところをタップされたらUIを閉じる
            if (agent == null)
            {
                shopUI?.CloseShop();
            }
            else
            {
                UpdateShopUI();
            }
        }

        // 🎨 UIに最新データを渡して表示させる
        private void UpdateShopUI()
        {
            if (currentCreature != null && shopUI != null)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null)
                {
                    // UI側は表示するだけで、ロジックは一切知らない
                    shopUI.OpenShop(genome.generation, genome.armCount, genome.jointsPerArm, playerPoints);
                }
            }
        }

        // 🛠️ 「増やす」ボタンが押された時のロジック
        private void HandleAddArm()
        {
            if (currentCreature != null && playerPoints >= armCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.armCount < 8)
                {
                    playerPoints -= armCost; // ポイント消費
                    currentCreature.AddArm(); // クリーチャーの改造を実行！
                    
                    UpdateShopUI(); // 画面を更新
                }
            }
        }

        // 🛠️ 「減らす」ボタンが押された時のロジック
        private void HandleRemoveArm()
        {
            if (currentCreature != null)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.armCount > 0)
                {
                    // 今回は減らすのは無料っす
                    currentCreature.RemoveArm();
                    
                    UpdateShopUI(); // 画面を更新
                }
            }
        }

        // 🛠️ 「関節を増やす」ボタンが押された時のロジック
        private void HandleAddJoint()
        {
            if (currentCreature != null && playerPoints >= jointCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.jointsPerArm < 4)
                {
                    playerPoints -= jointCost; // ポイント消費
                    currentCreature.AddJointSegment(); // クリーチャーの改造を実行！

                    UpdateShopUI(); // 画面を更新
                }
            }
        }

        // 🛠️ 「関節を減らす」ボタンが押された時のロジック
        private void HandleRemoveJoint()
        {
            if (currentCreature != null)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.jointsPerArm > 1)
                {
                    // 今回は減らすのは無料っす
                    currentCreature.RemoveJointSegment();

                    UpdateShopUI(); // 画面を更新
                }
            }
        }
    }
}