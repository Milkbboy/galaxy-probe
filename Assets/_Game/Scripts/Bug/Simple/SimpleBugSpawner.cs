using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Bug.Simple
{
    /// <summary>
    /// 프로토타입(_.html)의 spawnBug + spawnElite 포팅.
    /// 화면 밖 원형 둘레 랜덤 스폰, 일반/엘리트 타이머 분리.
    /// </summary>
    public class SimpleBugSpawner : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _machine;

        [Header("Bug Data")]
        [SerializeField] private SimpleBugData _normalData;
        [SerializeField] private SimpleBugData _eliteData;

        [Header("Spawn Limits")]
        [SerializeField] private int _maxBugs = 90;
        [SerializeField] private float _spawnInterval = 0.083f;   // 5 frames @ 60fps
        [SerializeField] private float _eliteInterval = 15f;      // ELITE_INTERVAL 900f / 60

        [Header("Spawn Area")]
        [SerializeField] private bool _autoRadius = true;
        [SerializeField] private float _manualRadius = 15f;
        [SerializeField] private float _normalMargin = 0.4f;
        [SerializeField] private float _eliteMargin = 0.5f;

        [Header("Wave")]
        [SerializeField] private int _wave = 1;

        private readonly List<SimpleBug> _alive = new();
        private float _spawnTimer;
        private float _eliteTimer;

        public int AliveCount => _alive.Count;
        public int Wave { get => _wave; set => _wave = Mathf.Max(1, value); }
        public IReadOnlyList<SimpleBug> Alive => _alive;

        private void Awake()
        {
            if (_machine == null)
            {
                var mc = FindFirstObjectByType<DrillCorp.Machine.MachineController>();
                if (mc != null) _machine = mc.transform;
            }
        }

        private void Update()
        {
            PruneDead();

            _eliteTimer -= Time.deltaTime;
            if (_eliteTimer <= 0f)
            {
                SpawnElite();
                _eliteTimer = _eliteInterval;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnNormal();
                _spawnTimer = _spawnInterval;
            }
        }

        public SimpleBug SpawnNormal()
        {
            if (_alive.Count >= _maxBugs || _normalData == null || _normalData.Prefab == null) return null;
            return SpawnAt(_normalData, GetRingSpawnPos(_normalMargin));
        }

        public SimpleBug SpawnElite()
        {
            if (_eliteData == null || _eliteData.Prefab == null) return null;
            return SpawnAt(_eliteData, GetRingSpawnPos(_eliteMargin));
        }

        public SimpleBug SpawnAt(SimpleBugData data, Vector3 pos)
        {
            var go = Instantiate(data.Prefab, pos, Quaternion.identity);
            var bug = go.GetComponent<SimpleBug>();
            if (bug == null) bug = go.AddComponent<SimpleBug>();
            bug.Initialize(data, _machine, _wave);
            _alive.Add(bug);
            return bug;
        }

        private Vector3 GetRingSpawnPos(float margin)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = GetSpawnRadius() + margin;
            Vector3 center = _machine != null ? _machine.position : transform.position;
            return new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                0f,
                center.z + Mathf.Sin(angle) * radius
            );
        }

        private float GetSpawnRadius()
        {
            if (!_autoRadius) return _manualRadius;
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return _manualRadius;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            return Mathf.Sqrt(halfW * halfW + halfH * halfH);
        }

        private void PruneDead()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] == null || _alive[i].IsDead) _alive.RemoveAt(i);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 c = _machine != null ? _machine.position : transform.position;
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(c, GetSpawnRadius() + _normalMargin);
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(c, GetSpawnRadius() + _eliteMargin);
        }
#endif
    }
}
