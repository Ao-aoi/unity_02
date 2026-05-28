using UnityEngine;
using System;
using UnityEngine.EventSystems; // ★追加：UIをクリックしているか判定するために必要
using Neuro.Creature;

namespace Neuro.Interaction
{
    public class CreatureSelector : MonoBehaviour
    {
        public static event Action<CreatureAgent> OnCreatureSelected;
        public static CreatureAgent CurrentSelected { get; private set; }

        [Header("選択時の見た目")]
        public GameObject selectionMarkerPrefab;
        private GameObject currentMarker;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 🛡️ 【重要】もしクリックした場所にUI（ボタンやパネルなど）がある場合は、
                // 選択の判定を行わずにここで処理をストップする（貫通防止）
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return; 
                }

                // スマホのタッチ判定用（念のため）
                if (Input.touchCount > 0 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    return;
                }

                HandleClick();
            }

            // マーカーの追従処理（今まで通り）
            if (currentMarker != null && CurrentSelected != null)
            {
                currentMarker.transform.position = CurrentSelected.transform.position;
            }
            else if (currentMarker != null && CurrentSelected == null)
            {
                Destroy(currentMarker);
            }
        }

        private void HandleClick()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                CreatureAgent agent = hit.collider.GetComponentInParent<CreatureAgent>();

                if (agent != null)
                {
                    SelectCreature(agent);
                }
                else
                {
                    Deselect();
                }
            }
            else
            {
                Deselect();
            }
        }

        private void SelectCreature(CreatureAgent agent)
        {
            if (CurrentSelected == agent) return;

            CurrentSelected = agent;
            
            if (selectionMarkerPrefab != null)
            {
                if (currentMarker != null) Destroy(currentMarker);
                currentMarker = Instantiate(selectionMarkerPrefab, agent.transform.position, Quaternion.identity);
            }

            OnCreatureSelected?.Invoke(agent);
        }

        private void Deselect()
        {
            if (CurrentSelected != null)
            {
                CurrentSelected = null;
                if (currentMarker != null) Destroy(currentMarker);
                OnCreatureSelected?.Invoke(null);
            }
        }
    }
}