using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Weapon.Pool
{
    /// <summary>
    /// 탄환(Projectile) Object Pool — 싱글톤, DontDestroyOnLoad.
    ///
    /// VfxPool 과 동일 패턴이지만 반환 트리거가 다르다:
    ///   - VfxPool: 파티클 종료(OnParticleSystemStopped) → PooledVfx 가 자동 반환
    ///   - BulletPool: 탄환 로직 (수명 만료 / 명중) → 호출자가 명시적으로 Return
    ///
    /// 기관총 탄환은 초당 10발 수준으로 발사되며 기존에는 `Instantiate → Destroy`
    /// 루프로 GC 압박을 만들었다. 풀링으로 GameObject 생성 0, MonoBehaviour 인스턴스 재사용.
    ///
    /// 사용법:
    ///   GameObject bullet = BulletPool.Get(prefab, pos, rot);
    ///   // 탄환 수명 끝날 때
    ///   BulletPool.Return(prefab, bullet);
    /// </summary>
    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [Tooltip("풀 오브젝트가 부착될 부모 (Hierarchy 정리용). null 이면 this.transform.")]
        [SerializeField] private Transform _poolRoot;

        [Tooltip(
            "첫 Get 호출 시 자동 pre-warm 할 인스턴스 수.\n" +
            "기관총 이론 최대 동시 존재 탄환 = BulletLifetime / FireDelay = 3s / 0.14s ≈ 22 발.\n" +
            "여유 2 더해 24 로 설정 → 첫 교전부터 풀 신규 확장 0.")]
        [SerializeField] private int _autoPrewarmCount = 24;

        private readonly Dictionary<GameObject, Stack<GameObject>> _pools
            = new Dictionary<GameObject, Stack<GameObject>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[BulletPool]");
            DontDestroyOnLoad(go);
            go.AddComponent<BulletPool>();
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

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            if (Instance == null) Bootstrap();
            return Instance.GetInternal(prefab, position, rotation, parent);
        }

        public static void Return(GameObject prefabKey, GameObject instance)
        {
            if (instance == null) return;
            if (Instance == null)
            {
                Destroy(instance);
                return;
            }
            Instance.ReturnInternal(prefabKey, instance);
        }

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
                stack = _pools[prefab];
            }

            GameObject obj = null;
            while (stack.Count > 0)
            {
                obj = stack.Pop();
                if (obj != null) break;
                obj = null;
            }

            if (obj == null)
                obj = CreateInstance(prefab);

            obj.transform.SetParent(parent != null ? parent : _poolRoot, false);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        private void ReturnInternal(GameObject prefabKey, GameObject instance)
        {
            if (prefabKey == null)
            {
                Destroy(instance);
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
            return obj;
        }
    }
}
