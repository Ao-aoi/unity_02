using UnityEngine;

public class AutoFeedManager : MonoBehaviour
{
    [Header("エサのプレハブ")]
    public GameObject foodPrefab; 

    [Header("自動生成の設定")]
    [Tooltip("1秒間に何個のエサを自動生成するか（0にすると自動生成停止）")]
    public float spawnRatePerSecond = 2f;
    
    // エサ生成までのタイマー
    private float spawnTimer = 0f;

    void Update()
    {
        // 1. 手動タップでのエサ生成（以前の機能も残しておくといざという時便利です）
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;
            Instantiate(foodPrefab, worldPos, Quaternion.identity);
        }

        // 2. 自動でのエサ生成ループ
        if (spawnRatePerSecond > 0f)
        {
            spawnTimer += Time.deltaTime;
            
            // 設定したレート（例: 2なら0.5秒）ごとにエサを降らせる
            float spawnInterval = 1f / spawnRatePerSecond;

            if (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                SpawnFoodRandomly();
            }
        }
    }

    // 画面内のランダムな位置にエサを生成する関数
    void SpawnFoodRandomly()
    {
        if (foodPrefab == null || Camera.main == null) return;

        // Viewport座標（0.0〜1.0）を使って、画面の少し内側（10%〜90%の位置）をランダムに選ぶ
        // こうすることで、スマホの画面サイズやカメラのズームが変わっても絶対に画面内に収まります
        float randomViewportX = Random.Range(0.1f, 0.9f);
        float randomViewportY = Random.Range(0.1f, 0.9f);

        // Viewport座標を、ゲーム内のワールド座標に変換
        Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(randomViewportX, randomViewportY, 0));
        
        // 2D用にZ座標を0にリセット
        spawnPos.z = 0f;

        Instantiate(foodPrefab, spawnPos, Quaternion.identity);
    }
}