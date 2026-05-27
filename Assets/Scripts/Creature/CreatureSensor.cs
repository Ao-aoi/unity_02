using UnityEngine;

public class CreatureSensor : MonoBehaviour
{
    [Header("視界のパラメーター")]
    public float sightRange = 5f;        // 視界の届く距離（個体差遺伝用）
    public float fieldOfViewAngle = 90f; // 視界の扇型の角度（個体差遺伝用）
    
    [Header("可視化用の設定")]
    public LineRenderer sightLineRenderer;
    public int lineSegments = 20;        // 扇型の線のなめらかさ（多いほど綺麗な丸になる）

    // 【AI（フェーズ3後半）に渡すためのセンサー出力データ】
    [HideInInspector] public Vector2 dirToClosestFood = Vector2.zero; // 最も近いエサへの方向
    [HideInInspector] public float distanceToClosestFood = 1f;       // 最も近いエサへの距離（0〜1に正規化）

    void Update()
    {
        // 1. 最も近いエサを探して、方向と距離を計算する（エサセンサー）
        FindClosestFood();

        // 2. 視界の扇型を画面に描画する（可視化）
        DrawSightCone();
    }

    // 🔴 2Dエサセンサー：一番近いエサを見つけるロジック
    void FindClosestFood()
    {
        // 画面内にある、Tagが "Food" のオブジェクトをすべて集める
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
        
        GameObject closestFood = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject food in foods)
        {
            float dist = Vector3.Distance(food.transform.position, currentPos);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestFood = food;
            }
        }

        // 一番近いエサが見つかった場合、AIの入力用データを更新する
        if (closestFood != null)
        {
            Vector3 directionWorld = (closestFood.transform.position - currentPos).normalized;
            
            // クリーチャーの向いている正面（transform.up）を基準とした、ローカルな2Dベクトルに変換
            // （これによって、自分から見て「前か後ろか、右か左か」がAIに伝わるようになります）
            dirToClosestFood.x = Vector3.Dot(directionWorld, transform.right);
            dirToClosestFood.y = Vector3.Dot(directionWorld, transform.up);

            // 距離を0〜1の間に収めてAIが学習しやすくする（視界の最大距離で割る）
            distanceToClosestFood = Mathf.Clamp01(closestDistance / sightRange);
        }
        else
        {
            // エサがないときはデフォルト値（正面を向いている、遠くにある）にする
            dirToClosestFood = Vector2.up;
            distanceToClosestFood = 1f;
        }
    }

    // 視界の可視化：LineRendererで綺麗な扇型を描くロジック
    void DrawSightCone()
    {
        if (sightLineRenderer == null) return;

        int pointCount = lineSegments + 2;
        sightLineRenderer.positionCount = pointCount;

        // 【ローカル空間になったので、自分の中心点は常に(0, 0, 0)になります】
        sightLineRenderer.SetPosition(0, Vector3.zero);

        // 正面（Unity2Dでは真上が基準なら90度、右が基準なら0度）を中心に扇型を開く
        // ここでは、純粋に「自分の正面」を0度として左右に広げます
        float startAngle = -(fieldOfViewAngle / 2f);
        float endAngle = (fieldOfViewAngle / 2f);

        for (int i = 0; i <= lineSegments; i++)
        {
            float progress = (float)i / lineSegments;
            // 2Dの標準的な向き（右）ではなく、クリーチャーの正面（真上）を基準にするため 90度（Mathf.PI / 2）をオフセットとして足します
            float angle = (Mathf.Lerp(startAngle, endAngle, progress) + 90f) * Mathf.Deg2Rad;

            // ローカル座標としての位置を計算
            Vector3 vertexPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * sightRange;
            
            // 【自分の現在位置（transform.position）を足さずに、そのままセットします】
            sightLineRenderer.SetPosition(i + 1, vertexPos);
        }
    }
}