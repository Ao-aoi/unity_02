using UnityEngine;
using Neuro.Creature;

namespace Neuro.Interaction
{
    public class CameraController : MonoBehaviour
    {
        [Header("カメラ設定")]
        public float defaultOrthoSize = 10f; // 普段の引きカメラのサイズ
        public float zoomOrthoSize = 5f;     // ズーム時のサイズ
        public float followSpeed = 5f;       // 追従するなめらかさ
        
        [Header("位置調整")]
        [Tooltip("重心からさらに少しズラしたい場合の微調整用（基本は 0,0,0 でOKです）")]
        public Vector3 zoomOffset = new Vector3(0, 0, 0);

        private Camera mainCamera;
        private Vector3 defaultPosition;
        
        // 🎯 追従対象を Transform ではなく、物理の主役（Rigidbody2D）に変えるっす！
        private Rigidbody2D targetRigidbody;

        void Start()
        {
            mainCamera = Camera.main;
            defaultPosition = mainCamera.transform.position;
            
            CreatureSelector.OnCreatureSelected += SetTarget;
        }

        void OnDestroy()
        {
            CreatureSelector.OnCreatureSelected -= SetTarget;
        }

        // クリーチャーが選択されたときに呼ばれる関数
        private void SetTarget(CreatureAgent agent)
        {
            if (agent == null)
            {
                targetRigidbody = null;
                return;
            }

            // 💡 【ここが最大のポイント】
            // クリーチャーの本体、またはその子オブジェクト（手足など）の中から
            // 実際に動いている「Rigidbody2D」を自動的に探し出してターゲットにするっす！
            targetRigidbody = agent.GetComponentInChildren<Rigidbody2D>();

            // もし子オブジェクトに見つからなければ、大元（親）のRigidbodyを試す
            if (targetRigidbody == null)
            {
                targetRigidbody = agent.GetComponent<Rigidbody2D>();
            }
        }

        void LateUpdate()
        {
            if (targetRigidbody != null)
            {
                // 🎯 実際の物理的な重心（worldCenterOfMass）、または現在のトランスフォーム位置を取得
                Vector3 targetPos = (Vector3)targetRigidbody.worldCenterOfMass + zoomOffset;
                targetPos.z = defaultPosition.z; // カメラの奥の距離は固定
                
                // スゥーッとなめらかに追従
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * followSpeed);
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, zoomOrthoSize, Time.deltaTime * followSpeed);
            }
            else
            {
                // 🏠 何も選択されていなければ、元の引きの画面に戻る
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, defaultPosition, Time.deltaTime * followSpeed);
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, defaultOrthoSize, Time.deltaTime * followSpeed);
            }
        }
    }
}