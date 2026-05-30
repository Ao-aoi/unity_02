using UnityEngine;
using Neuro.Creature;
using Neuro.Creature.Evaluation;

public class CreatureSensor : MonoBehaviour
{
    private const float MinSensorUpdateInterval = 0.01f;

    [Header("更新間隔")]
    [Tooltip("エサ探索を実行する間隔（秒）")]
    [Min(MinSensorUpdateInterval)] public float sensorUpdateInterval = 0.2f;

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
    public GameObject ClosestFood { get; private set; }
    public float DistanceToClosestFood { get; private set; } = 1f;
    public Vector2 DirToClosestFood { get; private set; } = Vector2.up;
    // 環境ギミック（ゾーン）検知用データ
    [HideInInspector] public Vector2 dirToClosestEnvironment = Vector2.zero;
    [HideInInspector] public float distanceToClosestEnvironment = 1f;
    public GameObject ClosestEnvironmentZone { get; private set; }
    public float DistanceToClosestEnvironment { get; private set; } = 1f;
    public Vector2 DirToClosestEnvironment { get; private set; } = Vector2.up;
    [Header("セクター式環境センサー")]
    [Tooltip("視界を分割するセクター数（例:8）")]
    public int sectorCount = 8;
    // 各セクターごとの正規化距離（0=近い, 1=視界外/未検出）
    [HideInInspector] public float[] sectorEnvironmentDistances;
    private Rigidbody2D[] childRigidbodies;
    private float sensorUpdateTimer;
    private float effectiveSensorUpdateInterval;

    void Awake()
    {
        childRigidbodies = GetComponentsInChildren<Rigidbody2D>();
        RefreshUpdateInterval();
    }

    void Start()
    {
        FindAndTrackClosestFood();
        EnsureSectorArray();
    }

    void OnValidate()
    {
        RefreshUpdateInterval();
        EnsureSectorArray();
    }

    private void EnsureSectorArray()
    {
        int count = Mathf.Max(1, sectorCount);
        if (sectorEnvironmentDistances == null || sectorEnvironmentDistances.Length != count)
            sectorEnvironmentDistances = new float[count];
        for (int i = 0; i < sectorEnvironmentDistances.Length; i++) sectorEnvironmentDistances[i] = 1f;
    }

    private Vector3 GetBodyCenterWorldPosition()
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
                return new Vector3(center.x, center.y, transform.position.z);
            }
        }

        return transform.position;
    }

    void Update()
    {
        sensorUpdateTimer += Time.deltaTime;
        if (sensorUpdateTimer >= effectiveSensorUpdateInterval)
        {
            // 1. 最も近いエサを探して、視界内ならロックオン線を描く
            FindAndTrackClosestFood();
            // 追加: 視界内の最も近い環境ゾーンを探す（後方互換）
            FindAndTrackClosestEnvironmentZone();
            // 追加: セクターごとの環境検出を行う
            SampleEnvironmentSectors();
            sensorUpdateTimer %= effectiveSensorUpdateInterval;
        }

        // 2. 視界の扇型を描画する
        DrawSightCone();
    }

    // 2Dエサセンサー ＋ 視界判定・ロックオン線描画
    private void RefreshUpdateInterval()
    {
        effectiveSensorUpdateInterval = Mathf.Max(MinSensorUpdateInterval, sensorUpdateInterval);
    }

    void FindAndTrackClosestFood()
    {
        var foods = FoodRegistry.Foods;
        
        GameObject closestFood = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPos = GetBodyCenterWorldPosition();

        // 登録済みのエサの中から一番近くて、かつ「視界に入っているエサ」を探す
        for (int i = 0; i < foods.Count; i++)
        {
            FoodItem foodItem = foods[i];
            if (foodItem == null)
                continue;

            GameObject food = foodItem.gameObject;
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
            ClosestFood = closestFood;
            DistanceToClosestFood = distanceToClosestFood;
            DirToClosestFood = dirToClosestFood;
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
            ClosestFood = null;
            DistanceToClosestFood = 1f;
            DirToClosestFood = Vector2.up;  

            // ❌ 【ロックオン線を消す】
            if (lockOnLineRenderer != null)
            {
                lockOnLineRenderer.positionCount = 0; // 点の数を0にすると線が消える
            }
        }
    }

    // 視界内にある最も近い EnvironmentZone を探して情報を更新する
    void FindAndTrackClosestEnvironmentZone()
    {
        // EnvironmentZone は namespace にあるため完全修飾名で取得
        var zones = UnityEngine.Object.FindObjectsByType<Neuro.Creature.Environment.EnvironmentZone>(FindObjectsSortMode.None);

        GameObject closestZone = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPos = GetBodyCenterWorldPosition();

        for (int i = 0; i < zones.Length; i++)
        {
            var zone = zones[i];
            if (zone == null) continue;

            Vector3 targetPos = zone.transform.position;
            float dist = Vector3.Distance(targetPos, currentPos);

            if (dist <= sightRange && dist < closestDistance)
            {
                Vector3 dirToTarget = (targetPos - currentPos).normalized;
                float angleToTarget = Vector3.Angle(transform.up, dirToTarget);

                if (angleToTarget <= fieldOfViewAngle / 2f)
                {
                    closestDistance = dist;
                    closestZone = zone.gameObject;
                }
            }
        }

        if (closestZone != null)
        {
            Vector3 directionWorld = (closestZone.transform.position - currentPos).normalized;

            dirToClosestEnvironment.x = Vector3.Dot(directionWorld, transform.right);
            dirToClosestEnvironment.y = Vector3.Dot(directionWorld, transform.up);
            distanceToClosestEnvironment = Mathf.Clamp01(closestDistance / sightRange);
            ClosestEnvironmentZone = closestZone;
            DistanceToClosestEnvironment = distanceToClosestEnvironment;
            DirToClosestEnvironment = dirToClosestEnvironment;
        }
        else
        {
            ClosestEnvironmentZone = null;
            DistanceToClosestEnvironment = 1f;
            DirToClosestEnvironment = Vector2.up;
        }
    }

    // セクターごとにレイキャストを飛ばして環境ゾーンの距離を記録する
    void SampleEnvironmentSectors()
    {
        EnsureSectorArray();
        Vector3 currentPos = GetBodyCenterWorldPosition();

        float halfFov = fieldOfViewAngle / 2f;
        int count = sectorEnvironmentDistances.Length;

        for (int i = 0; i < count; i++)
        {
            // セクター中心角（-halfFov .. +halfFov）
            float t = (count == 1) ? 0.5f : (i + 0.5f) / count;
            float sectorAngle = Mathf.Lerp(-halfFov, halfFov, t);
            Quaternion rot = Quaternion.AngleAxis(sectorAngle, Vector3.forward);
            Vector3 dir = rot * transform.up;

            RaycastHit2D hit = Physics2D.Raycast(currentPos, dir, sightRange);
            if (hit.collider != null)
            {
                var zone = hit.collider.GetComponentInParent<Neuro.Creature.Environment.EnvironmentZone>();
                if (zone != null)
                {
                    sectorEnvironmentDistances[i] = Mathf.Clamp01(hit.distance / sightRange);
                    continue;
                }
            }

            // ヒットなしまたは環境ゾーンでない場合は 1f（未検出）
            sectorEnvironmentDistances[i] = 1f;
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