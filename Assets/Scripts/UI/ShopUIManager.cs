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

        [Header("ボタン")]
        public Button addArmButton;    
        public Button removeArmButton; 
        
        // ★新要素：関節（セグメント）を増減するボタン
        public Button addJointButton;    
        public Button removeJointButton; 
        
        // 📢 コントローラー（外部）にボタンが押されたことを知らせるイベント
        public event Action OnAddArmRequested;
        public event Action OnRemoveArmRequested;
        
        // ★新要素：関節ボタンのイベント
        public event Action OnAddJointRequested;
        public event Action OnRemoveJointRequested;

        void Start()
        {
            shopPanel.SetActive(false);
            
            if (addArmButton != null) addArmButton.onClick.AddListener(() => OnAddArmRequested?.Invoke());
            if (removeArmButton != null) removeArmButton.onClick.AddListener(() => OnRemoveArmRequested?.Invoke());
            
            // ★新要素：関節ボタンのクリック検知
            if (addJointButton != null) addJointButton.onClick.AddListener(() => OnAddJointRequested?.Invoke());
            if (removeJointButton != null) removeJointButton.onClick.AddListener(() => OnRemoveJointRequested?.Invoke());
        }
        public void OpenShop(int generation, int armCount, int jointsPerArm, int currentPoints)
        {
            shopPanel.SetActive(true);
            RefreshUI(generation, armCount, jointsPerArm, currentPoints);
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
        }

        // 🎨 ステータスの表示に関節の数も追加
        public void RefreshUI(int generation, int armCount, int jointsPerArm, int currentPoints)
        {
            if (statusText != null)
            {
                statusText.text = $"第 {generation} 世代\n手足の数: {armCount} 本\n関節の数: {jointsPerArm} 個";
            }
            if (pointsText != null)
            {
                pointsText.text = $"EP: {currentPoints}";
            }
        }
    }
}