using UnityEngine;

namespace Neuro.Creature
{
    // ショップで購入し、スポーンポイントにセットできる「評価のルールブック」
    [System.Serializable]
    public class EvaluationCriteria
    {
        public string criteriaName = "エサ探索型"; // ルールの名前

        [Header("生存ボーナス")]
        public float survivalRewardPerSec = 1f; // 生きているだけで貰えるスコア

        [Header("エサ探索の評価")]
        public float approachFoodReward = 20f; // 近づいた時の倍率
        public float escapeFoodPenalty = 2f;   // 遠ざかった時のマイナス倍率

        [Header("移動・スピードの評価")]
        public float horizontalMoveReward = 0f; // 横方向（X軸）への移動ボーナス

        [Header("ジャンプ・高さの評価")]
        public float heightReward = 0f;         // 上方向（Y軸）への移動ボーナス
        public float airTimeReward = 0f;        // 空中にいる時間のボーナス
        
        [Header("滞在ペナルティ")]
        [Tooltip("動かないときに1秒あたり減点する値")]
        public float stationaryPenaltyPerSec = 0f;
        [Tooltip("この秒数以上動かなければペナルティ開始")]
        public float stationaryThresholdSeconds = 1f;
        [Tooltip("この移動距離未満を「動いていない」と見なす (単位: ユニティ距離)")]
        public float stationaryMovementEpsilon = 0.01f;
        // --- 以下はショップ用の設定 ---
        public bool isUnlocked = false;         // ショップでアンロックされたか
        public int unlockCost = 100;            // 解放に必要なポイント
    }
}