using UnityEngine;

public class AreaFeedManager : MonoBehaviour
{
    [Header("エサのプレハブ")]
    public GameObject foodPrefab;

    [Header("Gizmo")]
    public bool showSpawnAreaGizmo = true;

    [Header("自動生成の設定")]
    [Tooltip("1秒間に何個のエサを自動生成するか（0にすると自動生成停止）")]
    public float spawnRatePerSecond = 2f;

    // エサ生成までのタイマー
    private float spawnTimer = 0f;

    void Update()
    {
        if (spawnRatePerSecond <= 0f)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        // 設定したレートに応じて、一定間隔でエサを生成する
        float spawnInterval = 1f / spawnRatePerSecond;

        while (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            SpawnFoodInArea();
        }
    }

    // 指定した範囲内のランダムな位置にエサを生成する
    void SpawnFoodInArea()
    {
        if (foodPrefab == null)
        {
            return;
        }

        Vector3 areaPosition = transform.position;
        Vector3 areaScale = transform.lossyScale;

        float halfWidth = Mathf.Abs(areaScale.x) * 0.5f;
        float halfHeight = Mathf.Abs(areaScale.y) * 0.5f;

        float randomX = Random.Range(areaPosition.x - halfWidth, areaPosition.x + halfWidth);
        float randomY = Random.Range(areaPosition.y - halfHeight, areaPosition.y + halfHeight);

        Vector3 spawnPos = new Vector3(randomX, randomY, 0f);
        Instantiate(foodPrefab, spawnPos, Quaternion.identity);
    }

    void OnDrawGizmos()
    {
        if (!showSpawnAreaGizmo)
        {
            return;
        }

        Vector3 areaPosition = transform.position;
        Vector3 areaScale = transform.lossyScale;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(areaPosition, new Vector3(Mathf.Abs(areaScale.x), Mathf.Abs(areaScale.y), 0f));
    }
}
