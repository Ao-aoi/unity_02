using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EcosystemManager : MonoBehaviour
{
    [Header("プレハブ設定")]
    public GameObject creaturePrefab;
    [Header("UI設定")]
    public Canvas sliderCanvas;
    public int maxCreaturesCount = 5;
    public Transform spawnPoint;

    private List<GameObject> aliveCreatures = new List<GameObject>();
    // クリーチャー本体と、対応するUIスライダーをセットで裏で覚えておくための辞書(Dictionary)
    private Dictionary<GameObject, Slider> creatureUiMap = new Dictionary<GameObject, Slider>();

    void Start()
    {
        for (int i = 0; i < maxCreaturesCount; i++)
        {
            SpawnNewCreature();
        }
    }

    public void SpawnNewCreature()
    {
        // 1. 個体の生成
        Vector3 randomOffset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
        Vector3 spawnPos = (spawnPoint != null ? spawnPoint.position : Vector3.zero) + randomOffset;
        GameObject newCreature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);

        // 3. 各コンポーネントへの指示・初期設定（直接呼び出しによる通知）
        CreatureAgent agent = newCreature.GetComponent<CreatureAgent>();
        if (agent != null)
        {
            agent.SetupAgent(this);
            Slider sliderComponent = agent.InitializeUIFollow(sliderCanvas);

            if (sliderComponent != null)
            {
                creatureUiMap.Add(newCreature, sliderComponent);
            }
        }

        // 4. マネージャーのリストと、UI消去用のマップに登録
        aliveCreatures.Add(newCreature);
    }

    // クリーチャーが死亡した時に、クリーチャーから直接呼ばれる監視用関数
    public void OnCreatureDied(GameObject deadCreature)
    {
        if (aliveCreatures.Contains(deadCreature))
        {
            aliveCreatures.Remove(deadCreature);
        }

        // 紐づいていたUIスライダーをマップから探して削除
        if (creatureUiMap.ContainsKey(deadCreature))
        {
            Slider associatedSlider = creatureUiMap[deadCreature];
            if (associatedSlider != null)
            {
                Destroy(associatedSlider.gameObject);
            }
            creatureUiMap.Remove(deadCreature);
        }

        // クリーチャー本体の削除
        Destroy(deadCreature);

        // リアルタイム即座補充！
        SpawnNewCreature();
    }
}