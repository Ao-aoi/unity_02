using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Neuro.Creature;
public class ScoreUI : MonoBehaviour
{
    [Header("グラフのパーツ設定")]
    [Tooltip("ドット（点）として表示するUI Imageのプレハブ（丸い画像など）")]
    public GameObject dotPrefab;
    [Tooltip("線（ライン）として表示するUI Imageのプレハブ（白い四角い画像など）")]
    public GameObject linePrefab;

    [Header("デザイン設定")]
    public Color dotColor = Color.green;
    public Color lineColor = Color.white;
    public float lineWidth = 3f;
    public float dotSize = 8f;

    [Header("動作設定")]
    [Tooltip("グラフに保持する最大データ点数。これを超えたら古いデータから削除されます。")]
    public int maxPoints = 200;

    [Tooltip("縦軸ラベルの分割数（目盛りの個数-1）")]
    public int verticalTicks = 5;

    [Tooltip("横軸に表示するラベルの最大数（多すぎる場合は間引き表示されます）")]
    public int horizontalMaxLabels = 10;

    [Tooltip("ラベルのグラフに対するオフセット（例えば左ラベルや下ラベルの位置調整）")]
    public Vector2 labelOffset = new Vector2(-36f, -18f);

    [Header("ラベル設定（自動生成）")]
    [Tooltip("ラベルのフォントサイズ")]
    public float labelFontSize = 14f;
    [Tooltip("ラベルの色")]
    public Color labelColor = Color.white;
    [Tooltip("ラベルのサイズ（幅×高さ）")]
    public Vector2 labelSize = new Vector2(60f, 20f);

    private RectTransform graphContainer;
    private List<float> scoreHistory = new List<float>();
    private List<GameObject> graphObjects = new List<GameObject>(); // 古いグラフを消すためのリスト

    void Awake()
    {
        // このスクリプトが付いているUIのRectTransformを、グラフを描く枠（コンテナ）とする
        graphContainer = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        EcosystemManager.CreatureDied += HandleCreatureDied;
    }

    void OnDisable()
    {
        EcosystemManager.CreatureDied -= HandleCreatureDied;
    }

    private void HandleCreatureDied(float finalFitness)
    {
        scoreHistory.Add(finalFitness);

        // 最大保持数を超えたら古い要素から削除して長さを制限する
        while (scoreHistory.Count > maxPoints)
        {
            scoreHistory.RemoveAt(0);
        }

        RefreshGraph(scoreHistory);
    }

    // 📈 【外部呼び出し用API】マネージャーからスコア履歴を受け取ってグラフを描き直す
    public void RefreshGraph(List<float> scoreHistory)
    {
        // 1. 古いドットと線をすべて消去して画面をクリアにする
        foreach (GameObject obj in graphObjects)
        {
            if (obj != null) Destroy(obj);
        }
        graphObjects.Clear();

        if (scoreHistory == null || scoreHistory.Count < 2) return;

        // 2. グラフのサイズ（枠の幅と高さ）を取得
        float graphWidth = graphContainer.rect.width;
        float graphHeight = graphContainer.rect.height;

        // 3. 縦軸の最大値を自動調整（リスト内の最高スコアを見つける。最低でも10にしておく）
        float maxScore = 10f;
        foreach (float score in scoreHistory)
        {
            if (score > maxScore) maxScore = score;
        }

        // 4. 横軸の間隔を計算（スコアの数で枠の横幅を等分する）
        float xInterval = graphWidth / (scoreHistory.Count - 1);

        // --- 軸ラベルの生成（TextMeshProUGUI をプレハブなしで自動生成） ---
        // 縦軸ラベル（0〜maxScore を verticalTicks ごとに表示）
        for (int t = 0; t <= verticalTicks; t++)
        {
            float normalized = (float)t / verticalTicks; // 0..1
            float yPos = normalized * graphHeight;

            GameObject vLabel = CreateTMPLabel(Mathf.RoundToInt(normalized * maxScore).ToString(), new Vector2(labelOffset.x, yPos), TextAlignmentOptions.Right);
            graphObjects.Add(vLabel);
        }

        // 横軸ラベル（インデックスを間引き表示）
        int step = Mathf.Max(1, Mathf.CeilToInt((float)scoreHistory.Count / horizontalMaxLabels));
        for (int i = 0; i < scoreHistory.Count; i += step)
        {
            float xPos = i * xInterval;

            GameObject hLabel = CreateTMPLabel(i.ToString(), new Vector2(xPos, labelOffset.y), TextAlignmentOptions.Center);
            graphObjects.Add(hLabel);
        }

        // 前のフレームのドットの位置を覚えておく変数（線で繋ぐため）
        Vector2 lastDotPosition = Vector2.zero;

        // 5. スコアデータをループして、点と線を生成していく
        for (int i = 0; i < scoreHistory.Count; i++)
        {
            // データのインデックスから、UI上のX座標を計算
            float xPosition = i * xInterval;
            
            // スコアの値から、UI上のY座標を計算（0〜最大スコアの割合を、グラフの高さにかける）
            float yPosition = (scoreHistory[i] / maxScore) * graphHeight;
            
            Vector2 currentDotPosition = new Vector2(xPosition, yPosition);

            // 🔵 点（ドット）の生成
            GameObject dotObj = Instantiate(dotPrefab, graphContainer, false);
            graphObjects.Add(dotObj);
            
            Image dotImage = dotObj.GetComponent<Image>();
            if (dotImage != null) dotImage.color = dotColor;

            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.anchorMin = Vector2.zero;
            dotRt.anchorMax = Vector2.zero;
            dotRt.sizeDelta = new Vector2(dotSize, dotSize);
            dotRt.anchoredPosition = currentDotPosition;

            // ⚪ 線（ライン）の生成（2番目の点以降から、前の点と自分を繋ぐ）
            if (i > 0)
            {
                CreateLine(lastDotPosition, currentDotPosition);
            }

            // 今の位置を「前の位置」として記録して次のループへ
            lastDotPosition = currentDotPosition;
        }
    }

    // 二つの点を細長いImageで繋ぐロジック
    void CreateLine(Vector2 posA, Vector2 posB)
    {
        GameObject lineObj = Instantiate(linePrefab, graphContainer, false);
        graphObjects.Add(lineObj);

        Image lineImage = lineObj.GetComponent<Image>();
        if (lineImage != null) lineImage.color = lineColor;

        RectTransform lineRt = lineObj.GetComponent<RectTransform>();
        lineRt.anchorMin = Vector2.zero;
        lineRt.anchorMax = Vector2.zero;

        // 二点間の方向と距離を計算
        Vector2 direction = (posB - posA).normalized;
        float distance = Vector2.Distance(posA, posB);

        // 線の太さと長さをセット
        lineRt.sizeDelta = new Vector2(distance, lineWidth);
        
        // AとBのちょうど真ん中に配置する
        lineRt.anchoredPosition = posA + direction * distance * 0.5f;
        
        // AからBの方向へ向くように、画像を回転させる（Z軸回転）
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRt.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // TextMeshProUGUI ラベルをランタイムで生成するヘルパー
    GameObject CreateTMPLabel(string text, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("TMPLabel", typeof(RectTransform));
        go.transform.SetParent(graphContainer, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.alignment = alignment;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.sizeDelta = labelSize;
        rt.anchoredPosition = anchoredPosition;

        return go;
    }
}