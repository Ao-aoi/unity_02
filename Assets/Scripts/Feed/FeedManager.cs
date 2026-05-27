using UnityEngine;

public class FeedManager : MonoBehaviour
{
    // インスペクターからエサのプレハブ（Sprite、CircleCollider2Dがついたもの）を割り当てる
    public GameObject foodPrefab; 

    void Update()
    {
        // パソコンの左クリック、またはスマホの画面タップを検知
        if (Input.GetMouseButtonDown(0))
        {
            // タップされた画面の座標（ピクセル）を取得
            Vector3 mousePos = Input.mousePosition;

            // 画面座標を、ゲーム内の2Dワールド座標（メートル）に変換する
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            // Z軸はカメラの手前になってしまうので、2Dゲーム用に 0 にリセット
            worldPos.z = 0f;

            // エサを画面にポコッと生成！
            Instantiate(foodPrefab, worldPos, Quaternion.identity);
        }
    }
}