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
        
        private Camera mainCamera;
        private Vector3 defaultPosition;
        private CreatureAgent targetCreature;

        void Start()
        {
            mainCamera = Camera.main;
            defaultPosition = mainCamera.transform.position;
            
            // クリーチャーが選択されたら、自動でこの関数が呼ばれるように登録
            CreatureSelector.OnCreatureSelected += SetTarget;
        }

        void OnDestroy()
        {
            CreatureSelector.OnCreatureSelected -= SetTarget;
        }

        private void SetTarget(CreatureAgent agent)
        {
            targetCreature = agent;
        }

        void LateUpdate()
        {
            if (targetCreature != null)
            {
                // 🎯 ターゲットに追従しつつズームイン
                Vector3 targetPos = targetCreature.transform.position;
                targetPos.z = defaultPosition.z; // カメラのZ軸はそのまま維持
                
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * followSpeed);
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, zoomOrthoSize, Time.deltaTime * followSpeed);
            }
            else
            {
                // 🏠 何も選択されていなければ、元の位置とサイズにスゥーッと戻る
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, defaultPosition, Time.deltaTime * followSpeed);
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, defaultOrthoSize, Time.deltaTime * followSpeed);
            }
        }
    }
}