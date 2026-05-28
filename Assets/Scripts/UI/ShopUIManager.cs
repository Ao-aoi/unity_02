using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Neuro.UI
{
    public class ShopUIManager : MonoBehaviour
    {
        [Header("UIパネル")]
        public GameObject shopPanel; 
        
        [Header("テキスト表示")]
        public TextMeshProUGUI statusText;  
        public TextMeshProUGUI pointsText;  

        [Header("パーツ改造ボタン")]
        public Button addArmButton;    
        public Button removeArmButton; 
        public Button addJointButton;    
        public Button removeJointButton; 

        [Header("感覚アップグレード用のスライダー")]
        public Slider sightRangeSlider;   
        public Slider fieldOfViewSlider;  

        [Header("感覚アップグレード用のボタン")]
        public Button upgradeSightRangeButton; 
        public Button upgradeFieldOfViewButton; 

        [Header("★脳アップグレード用のUIパーツ")]
        public Slider brainNodeSlider;     // 脳のシワの量を表すバー
        public Button upgradeBrainButton;  // 脳を育てる！ボタン
        
        // 📢 コントローラーに通知するイベント
        public event Action OnAddArmRequested;
        public event Action OnRemoveArmRequested;
        public event Action OnAddJointRequested;
        public event Action OnRemoveJointRequested;
        public event Action OnUpgradeSightRangeRequested;
        public event Action OnUpgradeFieldOfViewRequested;

        // ★新要素のイベント
        public event Action OnUpgradeBrainRequested;

        void Start()
        {
            shopPanel.SetActive(false);
            
            if (addArmButton != null) addArmButton.onClick.AddListener(OnAddArmButtonClicked);
            if (removeArmButton != null) removeArmButton.onClick.AddListener(() => OnRemoveArmRequested?.Invoke());
            if (addJointButton != null) addJointButton.onClick.AddListener(() => OnAddJointRequested?.Invoke());
            if (removeJointButton != null) removeJointButton.onClick.AddListener(() => OnRemoveJointRequested?.Invoke());
            if (upgradeSightRangeButton != null) upgradeSightRangeButton.onClick.AddListener(() => OnUpgradeSightRangeRequested?.Invoke());
            if (upgradeFieldOfViewButton != null) upgradeFieldOfViewButton.onClick.AddListener(() => OnUpgradeFieldOfViewRequested?.Invoke());

            // ★新要素：脳強化ボタンのクリック検知
            if (upgradeBrainButton != null) upgradeBrainButton.onClick.AddListener(OnUpgradeBrainButtonClicked);

            if (sightRangeSlider != null) { sightRangeSlider.minValue = 2f; sightRangeSlider.maxValue = 15f; }
            if (fieldOfViewSlider != null) { fieldOfViewSlider.minValue = 30f; fieldOfViewSlider.maxValue = 180f; }
            
            // 💡 脳スライダーのメモリ（最小2〜最大24）をセット
            if (brainNodeSlider != null) { brainNodeSlider.minValue = 2f; brainNodeSlider.maxValue = 24f; }
        }

        // 🎨 引数に「hiddenNodes（脳のシワの数）」を追加して、データをもらう
        public void OpenShop(int generation, int armCount, int jointsPerArm, float sightRange, float fieldOfView, int hiddenNodes, int currentPoints)
        {
            shopPanel.SetActive(true);
            RefreshUI(generation, armCount, jointsPerArm, sightRange, fieldOfView, hiddenNodes, currentPoints);
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
        }

        public void RefreshUI(int generation, int armCount, int jointsPerArm, float sightRange, float fieldOfView, int hiddenNodes, int currentPoints)
        {
            // 💡 テキスト表示に「現在の脳のシワ（ニューロン数）」を表示させる
            if (statusText != null)
            {
                statusText.text = $"第 {generation} 世代\n手足の数: {armCount} 本\n関節の数: {jointsPerArm} 個\n脳の細胞: {hiddenNodes} 個";
            }
            if (pointsText != null)
            {
                pointsText.text = $"EP: {currentPoints}";
            }

            if (sightRangeSlider != null) sightRangeSlider.value = sightRange;
            if (fieldOfViewSlider != null) fieldOfViewSlider.value = fieldOfView;
            
            // 💡 現在の個体の脳のノード数を、スライダーメーターにセット！
            if (brainNodeSlider != null) brainNodeSlider.value = hiddenNodes;
        }

        // Debug wrappers so we can see which button was actually clicked in the Console
        private void OnAddArmButtonClicked()
        {
            Debug.Log("[ShopUI] AddArmButton clicked");
            OnAddArmRequested?.Invoke();
        }

        private void OnUpgradeBrainButtonClicked()
        {
            Debug.Log("[ShopUI] UpgradeBrainButton clicked");
            OnUpgradeBrainRequested?.Invoke();
        }
    }
}