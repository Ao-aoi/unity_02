using UnityEngine;
using UnityEngine.UI;
namespace Neuro.Creature{   
public class CreatureUIFollow : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 1.2f, 0); // 頭上の位置調整用
    private CanvasGroup hpSliderCanvasGroup;
    private Rigidbody2D[] childRigidbodies;

    void Awake()
    {
        childRigidbodies = GetComponentsInChildren<Rigidbody2D>();
    }

    private Vector3 GetFollowWorldPosition()
    {
        if (childRigidbodies != null && childRigidbodies.Length > 0)
        {
            Vector2 weightedCenter = Vector2.zero;
            float totalMass = 0f;

            for (int i = 0; i < childRigidbodies.Length; i++)
            {
                Rigidbody2D rb = childRigidbodies[i];
                if (rb == null)
                {
                    continue;
                }

                float mass = Mathf.Max(rb.mass, 0.0001f);
                weightedCenter += rb.worldCenterOfMass * mass;
                totalMass += mass;
            }

            if (totalMass > 0f)
            {
                Vector2 center = weightedCenter / totalMass;
                return new Vector3(center.x, center.y, transform.position.z) + uiOffset;
            }
        }

        return transform.position + uiOffset;
    }


    void LateUpdate()
    {
        // フレーム終端で追従させることで、急な移動時の見た目の遅れを減らす
        if (hpSlider != null)
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            Vector3 worldPosition = GetFollowWorldPosition();
            Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);
            hpSlider.transform.position = screenPosition;

            RectTransform sliderRect = hpSlider.transform as RectTransform;
            if (sliderRect == null)
            {
                return;
            }

            Vector2 scaledSize = Vector2.Scale(sliderRect.rect.size, sliderRect.lossyScale);
            float halfWidth = scaledSize.x * 0.5f;
            float halfHeight = scaledSize.y * 0.5f;

            bool isBehindCamera = screenPosition.z < 0f;
            bool isFullyOutsideScreen =
                (screenPosition.x + halfWidth < 0f) ||
                (screenPosition.x - halfWidth > Screen.width) ||
                (screenPosition.y + halfHeight < 0f) ||
                (screenPosition.y - halfHeight > Screen.height);

            bool shouldShow = !isBehindCamera && !isFullyOutsideScreen;
            if (hpSliderCanvasGroup == null)
            {
                hpSliderCanvasGroup = hpSlider.GetComponent<CanvasGroup>();
            }

            if (hpSliderCanvasGroup != null)
            {
                float nextAlpha = shouldShow ? 1f : 0f;
                if (!Mathf.Approximately(hpSliderCanvasGroup.alpha, nextAlpha))
                {
                    hpSliderCanvasGroup.alpha = nextAlpha;
                }
            }
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
            CanvasGroup canvasGroup = newSlider.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = newSlider.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            hpSliderCanvasGroup = canvasGroup;
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

    public void DestroyHPBar()
    {
        if (hpSlider != null)
        {
            Destroy(hpSlider.gameObject);
            hpSlider = null;
            hpSliderCanvasGroup = null;
        }
    }
}
}