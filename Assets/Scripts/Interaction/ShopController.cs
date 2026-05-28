using UnityEngine;
using Neuro.Creature;
using Neuro.UI; 

namespace Neuro.Interaction
{
    public class ShopController : MonoBehaviour
    {
        [Header("UIの参照")]
        public ShopUIManager shopUI; 

        [Header("ポイント設定（コスト）")]
        public int playerPoints = 300; // 初期お財布を少し大盛りの300にしておきます
        public int armCost = 20;       
        public int jointCost = 30;     
        public int sightRangeUpgradeCost = 15;
        public int fieldOfViewUpgradeCost = 15;
        
        // ★新要素：脳みそを賢くするアップグレードコスト！
        public int brainUpgradeCost = 40; 

        private CreatureAgent currentCreature;

        void OnEnable()
        {
            CreatureSelector.OnCreatureSelected += HandleCreatureSelected;
            
            if (shopUI != null)
            {
                shopUI.OnAddArmRequested += HandleAddArm;
                shopUI.OnRemoveArmRequested += HandleRemoveArm;
                shopUI.OnAddJointRequested += HandleAddJoint;
                shopUI.OnRemoveJointRequested += HandleRemoveJoint;
                shopUI.OnUpgradeSightRangeRequested += HandleUpgradeSightRange;
                shopUI.OnUpgradeFieldOfViewRequested += HandleUpgradeFieldOfView;

                // ★新要素：脳強化ボタンのイベントを購読
                shopUI.OnUpgradeBrainRequested += HandleUpgradeBrain;
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
                shopUI.OnUpgradeSightRangeRequested -= HandleUpgradeSightRange;
                shopUI.OnUpgradeFieldOfViewRequested -= HandleUpgradeFieldOfView;

                // ★新要素：イベント購読の解除
                shopUI.OnUpgradeBrainRequested -= HandleUpgradeBrain;
            }
        }

        private void HandleCreatureSelected(CreatureAgent agent)
        {
            currentCreature = agent;
            
            if (agent == null)
            {
                shopUI?.CloseShop();
            }
            else
            {
                UpdateShopUI();
            }
        }

        private void UpdateShopUI()
        {
            if (currentCreature != null && shopUI != null)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null)
                {
                    // ★脳のノード数（hiddenNodeCount）もUIに渡すように拡張！
                    shopUI.OpenShop(genome.generation, genome.armCount, genome.jointsPerArm, genome.sightRange, genome.fieldOfViewAngle, genome.hiddenNodeCount, playerPoints);
                }
            }
        }

        private void HandleAddArm()
        {
            if (currentCreature != null && playerPoints >= armCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.armCount < CreatureLimits.MaxArms)
                {
                    playerPoints -= armCost; 
                    currentCreature.AddArm(); 
                    UpdateShopUI(); 
                }
            }
        }

        private void HandleRemoveArm()
        {
            if (currentCreature != null)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.armCount > CreatureLimits.MinArms)
                {
                    currentCreature.RemoveArm();
                    UpdateShopUI(); 
                }
            }
        }

        private void HandleAddJoint()
        {
            if (currentCreature != null && playerPoints >= jointCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.jointsPerArm < CreatureLimits.MaxJointsPerArm) 
                {
                    playerPoints -= jointCost; 
                    currentCreature.AddJointSegment(); 
                    UpdateShopUI(); 
                }
            }
        }

        private void HandleRemoveJoint()
        {
            if (currentCreature != null)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.jointsPerArm > CreatureLimits.MinJointsPerArm) 
                {
                    currentCreature.RemoveJointSegment();
                    UpdateShopUI(); 
                }
            }
        }

        private void HandleUpgradeSightRange()
        {
            if (currentCreature != null && playerPoints >= sightRangeUpgradeCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.sightRange < 15f)
                {
                    playerPoints -= sightRangeUpgradeCost; 
                    currentCreature.UpgradeSightRange(1.0f); 
                    UpdateShopUI(); 
                }
            }
        }

        private void HandleUpgradeFieldOfView()
        {
            if (currentCreature != null && playerPoints >= fieldOfViewUpgradeCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                if (genome != null && genome.fieldOfViewAngle < 180f)
                {
                    playerPoints -= fieldOfViewUpgradeCost; 
                    currentCreature.UpgradeFieldOfView(15f); 
                    UpdateShopUI(); 
                }
            }
        }

        // 🛠️ ★新要素：「脳をアップグレード」するロジック
        private void HandleUpgradeBrain()
        {
            if (currentCreature != null && playerPoints >= brainUpgradeCost)
            {
                CreatureGenome genome = currentCreature.GetGenome();
                // 最大24ノード未満なら実行
                if (genome != null && genome.hiddenNodeCount < 24)
                {
                    playerPoints -= brainUpgradeCost; // ポイント消費
                    currentCreature.UpgradeBrainNodes(); // 脳細胞を増やす！
                    UpdateShopUI(); // 表示更新
                }
            }
        }
    }
}