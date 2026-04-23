using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.UI
{
    /// <summary>
    /// DamagePopup 전용 오브젝트 풀 (싱글톤, DontDestroyOnLoad).
    ///
    /// 배경:
    ///   이전 구현은 매 팝업마다 `new GameObject + AddComponent&lt;TextMeshPro&gt;` 로 생성하고
    ///   수명 만료 시 `Destroy(gameObject)` — 머신 피격 시 초당 5+ 개 누적 (50s 측정에서 +256).
    ///   TextMeshPro 는 Mesh 도 함께 생성·파괴해 GC 압박이 큼.
    ///
    /// 전략:
    ///   첫 사용 시 GameObject + TextMeshPro + DamagePopup 컴포넌트 구성 → 이후 재활용.
    ///   ReleaseToPool 에서 SetActive(false) 로 비활성만 하고 컴포넌트는 그대로 보존.
    ///
    /// 부트스트랩:
    ///   DamagePopup.Create 첫 호출 시 Instance 자동 생성 → 수동 배치 불필요.
    /// </summary>
    public class DamagePopupPool : MonoBehaviour
    {
        public static DamagePopupPool Instance { get; private set; }

        // 모든 DamagePopup 이 동일 구조라 프리팹 분기 불필요 — 단일 스택
        private readonly Stack<DamagePopup> _pool = new Stack<DamagePopup>();
        private Transform _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[DamagePopupPool]");
            DontDestroyOnLoad(go);
            go.AddComponent<DamagePopupPool>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _root = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 풀에서 비활성 상태의 DamagePopup 을 꺼냄. 비어있으면 새로 생성.
        /// 호출자가 활성화 + 초기화 책임.
        /// </summary>
        public static DamagePopup Acquire()
        {
            if (Instance == null) Bootstrap();
            return Instance.AcquireInternal();
        }

        /// <summary>풀로 반환 — DamagePopup 내부에서 수명 만료 시 호출.</summary>
        public static void Release(DamagePopup popup)
        {
            if (Instance == null || popup == null)
            {
                if (popup != null) Destroy(popup.gameObject);
                return;
            }
            Instance.ReleaseInternal(popup);
        }

        private DamagePopup AcquireInternal()
        {
            DamagePopup popup = null;
            while (_pool.Count > 0)
            {
                popup = _pool.Pop();
                if (popup != null) break;
                popup = null;
            }

            if (popup == null)
            {
                var go = new GameObject("DamagePopup");
                popup = go.AddComponent<DamagePopup>();
            }

            // 재활용 시 부모는 임시로 루트에 둠. 호출자가 위치/회전 지정.
            popup.transform.SetParent(null, false);
            return popup;
        }

        private void ReleaseInternal(DamagePopup popup)
        {
            if (popup == null) return;
            var go = popup.gameObject;
            if (go == null) return;

            go.SetActive(false);
            go.transform.SetParent(_root, false);
            _pool.Push(popup);
        }
    }
}
