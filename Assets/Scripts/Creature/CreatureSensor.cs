using UnityEngine;

public class CreatureSensor : MonoBehaviour
{
    [Header("視界のパラメーター")]
    public float sightRange = 5f;        // 視界の届く距離
    public float fieldOfViewAngle = 90f; // 視界の扇型の角度
    
    [Header("可視化用の設定（扇型視界）")]
    public LineRenderer sightLineRenderer; // (Use World Space は オフ)
    public int lineSegments = 20;        

    [Header("可視化用の設定（ロックオン線）")]
    public LineRenderer lockOnLineRenderer; // (Use World Space は オン)

    // AIに渡すためのセンサー出力データ
    [HideInInspector] public Vector2 dirToClosestFood = Vector2.zero; 
    [HideInInspector] public float distanceToClosestFood = 1f;       

    void Update()
    {
        // 1. 最も近いエサを探して、視界内ならロックオン線を描く
        FindAndTrackClosestFood();

        // 2. 視界の扇型を描画する
        DrawSightCone();
    }

    // 2Dエサセンサー ＋ 視界判定・ロックオン線描画
    void FindAndTrackClosestFood()
    {
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
        
        GameObject closestFood = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        // 全てのエサの中から一番近くて、かつ「視界に入っているエサ」を探す
        foreach (GameObject food in foods)
        {
            Vector3 targetPos = food.transform.position;
            float dist = Vector3.Distance(targetPos, currentPos);

            // 1. まず距離が視界の範囲内かチェック
            if (dist <= sightRange && dist < closestDistance)
            {
                // 2. 次に、自分の正面（transform.up）とエサへの方向の「角度」をチェック
                Vector3 dirToTarget = (targetPos - currentPos).normalized;
                float angleToTarget = Vector3.Angle(transform.up, dirToTarget);

                // 計算した角度が、視界の半分（左右の広がり）より小さければ「視界内」
                if (angleToTarget <= fieldOfViewAngle / 2f)
                {
                    closestDistance = dist;
                    closestFood = food;
                }
            }
        }

        // 視界内に一番近いエサが【見つかった】場合
        if (closestFood != null)
        {
            Vector3 directionWorld = (closestFood.transform.position - currentPos).normalized;
            
            // AI入力データの更新
            dirToClosestFood.x = Vector3.Dot(directionWorld, transform.right);
            dirToClosestFood.y = Vector3.Dot(directionWorld, transform.up);
            distanceToClosestFood = Mathf.Clamp01(closestDistance / sightRange);

            // 🎯 【ロックオン線の描画】
            if (lockOnLineRenderer != null)
            {
                lockOnLineRenderer.positionCount = 2;
                // 0番目の点は自分の位置（ワールド座標）
                lockOnLineRenderer.SetPosition(0, currentPos);
                // 1番目の点はエサの位置（ワールド座標）
                lockOnLineRenderer.SetPosition(1, closestFood.transform.position);
            }
        }
        // 🔴 エサがない、または視界から【外れた】場合
        else
        {
            dirToClosestFood = Vector2.up;
            distanceToClosestFood = 1f;

            // ❌ 【ロックオン線を消す】
            if (lockOnLineRenderer != null)
            {
                lockOnLineRenderer.positionCount = 0; // 点の数を0にすると線が消える
            }
        }
    }

    // 🟢 視界の可視化（前回と同じローカル空間の計算コード）
    void DrawSightCone()
    {
        if (sightLineRenderer == null) return;

        int pointCount = lineSegments + 2;
        sightLineRenderer.positionCount = pointCount;
        sightLineRenderer.SetPosition(0, Vector3.zero);

        float startAngle = -(fieldOfViewAngle / 2f);
        float endAngle = (fieldOfViewAngle / 2f);

        for (int i = 0; i <= lineSegments; i++)
        {
            float progress = (float)i / lineSegments;
            float angle = (Mathf.Lerp(startAngle, endAngle, progress) + 90f) * Mathf.Deg2Rad;
            Vector3 vertexPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * sightRange;
            sightLineRenderer.SetPosition(i + 1, vertexPos);
        }
    }
}