using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Bug.Pool
{
    /// <summary>
    /// Bug Object Pool (싱글톤)
    /// BugData별 프리펩을 미리 생성/재사용하여 Instantiate 오버헤드 제거
    /// </summary>
    public class BugPool : MonoBehaviour
    {
        public static BugPool Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private BugPoolConfig _config;

        [Header("Runtime")]
        [Tooltip("풀 오브젝트가 부착될 부모 (씬 정리용)")]
        [SerializeField] private Transform _poolRoot;

        private readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
        private readonly Dictionary<int, BugData> _dataLookup = new Dictionary<int, BugData>();
        private readonly Dictionary<int, BugPoolConfig.PoolEntry> _entryLookup = new Dictionary<int, BugPoolConfig.PoolEntry>();
        private readonly HashSet<GameObject> _activeBugs = new HashSet<GameObject>();

        public int ActiveCount => _activeBugs.Count;
        public int MaxActiveTotal => _config != null ? _config.MaxActiveTotal : 1000;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_poolRoot == null)
                _poolRoot = transform;

            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializePools()
        {
            if (_config == null)
            {
                Debug.LogWarning("[BugPool] Config가 설정되지 않았습니다.");
                return;
            }

            foreach (var entry in _config.Entries)
            {
                if (entry.BugData == null || entry.BugData.Prefab == null)
                    continue;

                int bugId = entry.BugData.BugId;
                if (_pools.ContainsKey(bugId))
                    continue;

                var queue = new Queue<GameObject>(entry.InitialSize);
                _pools[bugId] = queue;
                _dataLookup[bugId] = entry.BugData;
                _entryLookup[bugId] = entry;

                for (int i = 0; i < entry.InitialSize; i++)
                {
                    var obj = CreateInstance(entry.BugData);
                    queue.Enqueue(obj);
                }
            }
        }

        private GameObject CreateInstance(BugData bugData)
        {
            var obj = Instantiate(bugData.Prefab, _poolRoot);
            obj.name = $"Bug_{bugData.BugName}_Pooled";
            obj.SetActive(false);

            var pooled = obj.GetComponent<PooledBug>();
            if (pooled == null)
                pooled = obj.AddComponent<PooledBug>();
            pooled.Setup(this, bugData.BugId);

            return obj;
        }

        public GameObject Get(BugData bugData, Vector3 position, Quaternion rotation)
        {
            if (bugData == null)
                return null;

            if (_activeBugs.Count >= MaxActiveTotal)
            {
                Debug.LogWarning($"[BugPool] 최대 활성 수 초과: {MaxActiveTotal}");
                return null;
            }

            int bugId = bugData.BugId;

            if (!_pools.TryGetValue(bugId, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[bugId] = queue;
                _dataLookup[bugId] = bugData;
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                bool allowGrow = !_entryLookup.TryGetValue(bugId, out var entry) || entry.AllowGrow;
                if (!allowGrow)
                    return null;

                obj = CreateInstance(bugData);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _activeBugs.Add(obj);

            return obj;
        }

        public void Return(GameObject obj, int bugId)
        {
            if (obj == null)
                return;

            _activeBugs.Remove(obj);

            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);

            if (!_pools.TryGetValue(bugId, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[bugId] = queue;
            }

            queue.Enqueue(obj);
        }
    }
}
