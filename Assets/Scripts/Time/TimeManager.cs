using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("時間の速さを変更するスライダーを割り当ててください")]
    public Slider timeSlider;
    
    [Tooltip("現在の倍率を表示するテキスト（任意）")]
    public TextMeshProUGUI timeDisplayText; 

    void Start()
    {
        if (timeSlider != null)
        {
            // スライダーの設定を初期化
            timeSlider.minValue = 0f;    // 0 = 一時停止
            timeSlider.maxValue = 5f;    // 5 = 5倍速（スマホの限界に合わせて調整してください）
            timeSlider.value = 1f;       // 最初は1倍速（標準）

            // スライダーが動かされた時に呼ばれる関数を登録
            timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            
            // 初回のテキスト表示更新
            UpdateTimeDisplay(timeSlider.value);
        }
    }

    // スライダーの値が変わるたびに呼ばれる関数
    public void OnTimeSliderChanged(float newTimeScale)
    {
        // 🕰️ 【魔法の1行】ゲーム全体の時間の進み方を変更する
        Time.timeScale = newTimeScale;

        UpdateTimeDisplay(newTimeScale);
    }

    void UpdateTimeDisplay(float speed)
    {
        if (timeDisplayText != null)
        {
            // "1.5x" のように小数点1桁まで表示する
            timeDisplayText.text = speed.ToString("F1") + "x";
        }
    }

    void OnDestroy()
    {
        // ゲーム終了時やシーン破棄時に、必ず標準の1倍速に戻しておく（バグ防止）
        Time.timeScale = 1f;
    }
}