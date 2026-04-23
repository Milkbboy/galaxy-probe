using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillCorp.VFX.Pool
{
    /// <summary>
    /// VFX Object Pool (싱글톤, DontDestroyOnLoad).
    ///
    /// 문제:
    ///   과거 `Instantiate(prefab) + Destroy(vfx, t)` 패턴은
    ///   Polygon Arsenal 처럼 자식 PS/서브 이미터/트레일이 섞인 복합 VFX 에서
    ///   cullingMode=PauseAndCatchup + 화면 밖 스폰 조합으로 파티클이 영원히 종료 안 돼
    ///   GameObject 가 누적되는 누수를 유발 (분당 40~110MB 힙 증가 측정됨).
    ///
    /// 해결:
    ///   Destroy 자체를 없앤다. 사용 끝난 인스턴스는 SetActive(false) 로 비활성화 후 풀 반환.
    ///   인스턴스 개수가 pre-warm 이후 증가하지 않으면 GC 압박도 0.
    ///
    /// 사용법:
    ///   GameObject vfx = VfxPool.Get(prefab, pos, rot);
    ///   // 반환은 PooledVfx 가 OnParticleSystemStopped / 폴백 타이머로 자동 처리
    ///
    /// 부트스트랩:
    ///   RuntimeInitializeOnLoadMethod 로 씬 로드 직후 자동 생성 → 수동 배치 불필요.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        public static VfxPool Instance { get; private set; }

        [Tooltip("풀 오브젝트가 부착될 부모 (Hierarchy 정리용). null 이면 this.transform.")]
        [SerializeField] private Transform _poolRoot;

        [Tooltip("첫 Get 호출 시 자동 pre-warm 할 인스턴스 수. 0 이면 미사용.")]
        [SerializeField] private int _autoPrewarmCount = 8;

        // 프리팹 → 비활성 인스턴스 스택. Stack 이 Queue 보다 캐시/GC 에 유리 (최근 반환 먼저 재사용).
        private readonly Dictionary<GameObject, Stack<GameObject>> _pools
            = new Dictionary<GameObject, Stack<GameObject>>();

        // 반환 시 원본 프리팹 역추적용 — PooledVfx 가 Return 호출 시 자기 프리팹 키를 넘겨줌
        // (GameObject 이름 파싱 같은 비싼 방법 대신).

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[VfxPool]");
            DontDestroyOnLoad(go);
            go.AddComponent<VfxPool>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_poolRoot == null) _poolRoot = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 풀에서 VFX 인스턴스 꺼내 활성화. 풀이 비어있으면 Instantiate.
        /// 첫 호출 시 _autoPrewarmCount 만큼 미리 생성해 스폰 스파이크 완화.
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            if (Instance == null) Bootstrap();
            return Instance.GetInternal(prefab, position, rotation, parent);
        }

        /// <summary>
        /// PooledVfx 전용 반환 경로. 외부 호출 불필요 (PooledVfx 가 자동 호출).
        /// </summary>
        public void ReturnInternal(GameObject instance, GameObject prefabKey)
        {
            if (instance == null || prefabKey == null)
            {
                if (instance != null) Destroy(instance);
                return;
            }
            if (!_pools.TryGetValue(prefabKey, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[prefabKey] = stack;
            }
            instance.SetActive(false);
            if (instance.transform.parent != _poolRoot)
                instance.transform.SetParent(_poolRoot, false);
            stack.Push(instance);
        }

        /// <summary>
        /// 특정 프리팹 count 개 미리 생성 (보스 스폰 직전 등 예측 가능한 타이밍에 수동 호출용).
        /// </summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            if (Instance == null) Bootstrap();
            Instance.PrewarmInternal(prefab, count);
        }

        private GameObject GetInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!_pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[prefab] = stack;
                if (_autoPrewarmCount > 0) PrewarmInternal(prefab, _autoPrewarmCount);
                stack = _pools[prefab]; // PrewarmInternal 이 채워둠
            }

            GameObject obj = null;
            while (stack.Count > 0)
            {
                obj = stack.Pop();
                if (obj != null) break;   // Destroy 된 잔해 건너뜀
                obj = null;
            }

            if (obj == null)
                obj = CreateInstance(prefab);

            obj.transform.SetParent(parent != null ? parent : _poolRoot, false);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        private void PrewarmInternal(GameObject prefab, int count)
        {
            if (!_pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[prefab] = stack;
            }
            for (int i = 0; i < count; i++)
            {
                var obj = CreateInstance(prefab);
                obj.SetActive(false);
                stack.Push(obj);
            }
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var obj = Instantiate(prefab, _poolRoot);
            obj.name = $"{prefab.name}_Pooled";

            // PooledVfx 가 프리팹에 없더라도 런타임에 부착해 풀 반환 가능하게 보장.
            // 에디터에서 VfxPoolAttacher 로 미리 붙여두면 Awake 세팅(cullingMode 등)이 확실.
            var pooled = obj.GetComponent<PooledVfx>();
            if (pooled == null) pooled = obj.AddComponent<PooledVfx>();
            pooled.Bind(this, prefab);

            return obj;
        }
    }
}
