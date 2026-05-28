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
        
        // 📢 コントローラー（外部）にボタンが押されたことを知らせるイベント
        public event Action OnAddArmRequested;
        public event Action OnRemoveArmRequested;

        void Start()
        {
            shopPanel.SetActive(false);
            
            // ボタンが押されたら、イベントを発行するだけ！
            if (addArmButton != null) addArmButton.onClick.AddListener(() => OnAddArmRequested?.Invoke());
            if (removeArmButton != null) removeArmButton.onClick.AddListener(() => OnRemoveArmRequested?.Invoke());
        }

        // 🎨 外部から「このデータでUIを開いて！」と呼ばれるAPI
        public void OpenShop(int generation, int armCount, int currentPoints)
        {
            shopPanel.SetActive(true);
            RefreshUI(generation, armCount, currentPoints);
        }

        // 🎨 外部から「UIを閉じて！」と呼ばれるAPI
        public void CloseShop()
        {
            shopPanel.SetActive(false);
        }

        // 🎨 テキストの表示だけを更新するAPI
        public void RefreshUI(int generation, int armCount, int currentPoints)
        {
            if (statusText != null)
            {
                statusText.text = $"第 {generation} 世代\n手足の数: {armCount} 本";
            }
            if (pointsText != null)
            {
                pointsText.text = $"EP: {currentPoints}";
            }
        }
    }
}