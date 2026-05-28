using UnityEngine;
using System;
using Neuro.Creature;

namespace Neuro.Interaction
{
    public class CreatureSelector : MonoBehaviour
    {
        // 📢 他のスクリプト（UIなど）に「誰かが選択されたよ！」と知らせるためのイベント
        public static event Action<CreatureAgent> OnCreatureSelected;
        
        // 現在選択されているクリーチャー
        public static CreatureAgent CurrentSelected { get; private set; }

        [Header("選択時の見た目（オプション）")]
        [Tooltip("選択されていることを示すマーカー（リング画像などのプレハブ）")]
        public GameObject selectionMarkerPrefab;
        private GameObject currentMarker;

        void Update()
        {
            // スマホのタップ、またはマウスの左クリックを検知
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }

            // マーカーがあれば、選択中のクリーチャーに毎フレーム追従させる
            if (currentMarker != null && CurrentSelected != null)
            {
                currentMarker.transform.position = CurrentSelected.transform.position;
            }
            // 選択中のクリーチャーが死んで消滅したらマーカーも消す
            else if (currentMarker != null && CurrentSelected == null)
            {
                Destroy(currentMarker);
            }
        }

        private void HandleClick()
        {
            // タップした画面の位置を、ゲーム内の2Dワールド座標に変換
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            // 👆 その位置から画面の奥に向かって「見えないレーザー（Ray）」を撃ち、何かに当たるかチェック
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                // 手足に触れた場合でも、親（胴体）についている CreatureAgent を取得する
                CreatureAgent agent = hit.collider.GetComponentInParent<CreatureAgent>();

                if (agent != null)
                {
                    SelectCreature(agent);
                }
                else
                {
                    // エサや背景など、クリーチャー以外を触ったら選択解除
                    Deselect();
                }
            }
            else
            {
                // 何もない空間を触ったら選択解除
                Deselect();
            }
        }

        private void SelectCreature(CreatureAgent agent)
        {
            // 同じ個体を連続でタップしたら何もしない
            if (CurrentSelected == agent) return;

            CurrentSelected = agent;
            
            // 選択マーカーの表示
            if (selectionMarkerPrefab != null)
            {
                if (currentMarker != null) Destroy(currentMarker);
                currentMarker = Instantiate(selectionMarkerPrefab, agent.transform.position, Quaternion.identity);
            }

            // 遺伝子データ（手足の数）をログに表示して確認
            CreatureGenome genome = agent.GetGenome();
            int armCount = genome != null ? genome.armCount : 0;
            Debug.Log($"🧬 クリーチャーを選択しました！ この個体の手足の数: {armCount}本");

            // UIなどにイベントを発行して知らせる
            OnCreatureSelected?.Invoke(agent);
        }

        private void Deselect()
        {
            if (CurrentSelected != null)
            {
                CurrentSelected = null;
                if (currentMarker != null) Destroy(currentMarker);
                
                Debug.Log("選択を解除しました。");
                OnCreatureSelected?.Invoke(null); // nullを渡してUIを閉じるよう知らせる
            }
        }
    }
}