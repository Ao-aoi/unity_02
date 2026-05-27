using UnityEngine;
using UnityEngine.UI;

public class CreatureUIFollow : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 1.2f, 0); // 頭上の位置調整用


    void Update()
    {
        // スライダーの頭上へのピタッとした追従処理だけに専念する
        if (hpSlider != null)
        {
            Vector3 worldPosition = transform.position + uiOffset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            hpSlider.transform.position = screenPosition;
        }
    }

    // 【API】エージェントが生まれたときに、スライダーをInstanciateする
    public Slider InitializeSlider(float maxHP, Canvas sliderCanvas)
    {
        if (hpSlider != null && sliderCanvas != null)
        {
            Slider newSlider = Instantiate(hpSlider, sliderCanvas.transform);
            newSlider.maxValue = maxHP;
            newSlider.value = maxHP;
            hpSlider = newSlider; // 生成したスライダーをこのクラスの参照にセット
            return newSlider;
        }   

        return null;
    }

    // 【API】エージェントがお腹空いた時に、直接これを呼んでHPの見た目を変える
    public void UpdateHPBar(float currentHP)
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }
    }
}