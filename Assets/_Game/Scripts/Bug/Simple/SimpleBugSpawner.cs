using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Bug.Simple
{
    /// <summary>
    /// 프로토타입(_.html)의 spawnBug + spawnElite 포팅.
    /// 화면 밖 원형 둘레 랜덤 스폰, 일반/엘리트 타이머 분리.
    /// 웨이브별 파라미터는 SimpleWaveManager가 Configure()로 주입.
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
        [SerializeField] private SpawnShape _spawnShape = SpawnShape.Rect;
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
                var mc = FindAnyObjectByType<DrillCorp.Machine.MachineController>();
                if (mc != null) _machine = mc.transform;
            }
        }

        private void Update()
        {
            PruneDead();

            // EliteSpawnInterval=0 은 "엘리트 비활성" 의미 — 타이머 자체를 돌리지 않음
            if (_eliteInterval > 0f)
            {
                _eliteTimer -= Time.deltaTime;
                if (_eliteTimer <= 0f)
                {
                    SpawnElite();
                    _eliteTimer = _eliteInterval;
                }
            }

            if (_spawnInterval > 0f)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnNormal();
                    _spawnTimer = _spawnInterval;
                }
            }
        }

        /// <summary>
        /// SimpleWaveManager가 웨이브 진입 시 호출. WaveData 오버라이드 + SpawnConfig 폴백을 해석해 런타임 파라미터 주입.
        /// </summary>
        public void Configure(SimpleWaveData wave, SpawnConfigData cfg)
        {
            if (wave == null || cfg == null)
            {
                Debug.LogWarning("[SimpleBugSpawner] Configure: wave 또는 cfg가 null");
                return;
            }

            _spawnInterval = wave.ResolveNormalSpawnInterval(cfg);
            _eliteInterval = wave.ResolveEliteSpawnInterval(cfg);
            _maxBugs = wave.ResolveMaxBugs(cfg);
            _wave = Mathf.Max(1, wave.WaveNumber);

            _spawnShape = cfg.SpawnShape;
            _autoRadius = cfg.AutoRadius;
            _manualRadius = cfg.ManualRadius;
            _normalMargin = cfg.NormalMargin;
            _eliteMargin = cfg.EliteMargin;

            // 웨이브 진입 시점에 다음 스폰까지 대기를 reset
            _spawnTimer = _spawnInterval;
            _eliteTimer = _eliteInterval;
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
            return _spawnShape == SpawnShape.Rect
                ? GetRectPerimeterSpawnPos(margin)
                : GetCirclePerimeterSpawnPos(margin);
        }

        private Vector3 GetCirclePerimeterSpawnPos(float margin)
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

        // 카메라 frustum 사각형 외곽 둘레에 균등 스폰.
        // 둘레 길이로 가중해서 4변 중 하나 선택 → 변 안에서 랜덤 위치 → margin만큼 바깥으로 밀어냄.
        // 머신 위치가 아니라 카메라 위치 기준이므로 Dynamic 카메라가 마우스 쪽으로 이동해도 자연스럽게 따라감.
        private Vector3 GetRectPerimeterSpawnPos(float margin)
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                return GetCirclePerimeterSpawnPos(margin);
            }

            // XZ 평면 기준 카메라 중심과 화면 절반 크기 (카메라가 -Y로 내려다보는 탑다운)
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 camPos = cam.transform.position;

            // 둘레 길이로 가중해 변 선택 (가로변 2 * 2*halfW, 세로변 2 * 2*halfH)
            float horizLen = 2f * halfW;
            float vertLen = 2f * halfH;
            float total = 2f * (horizLen + vertLen);
            float pick = Random.Range(0f, total);

            float x, z;
            if (pick < horizLen)
            {
                // 위쪽 변 (Z+)
                x = camPos.x + Random.Range(-halfW, halfW);
                z = camPos.z + halfH + margin;
            }
            else if (pick < horizLen + vertLen)
            {
                // 오른쪽 변 (X+)
                x = camPos.x + halfW + margin;
                z = camPos.z + Random.Range(-halfH, halfH);
            }
            else if (pick < 2f * horizLen + vertLen)
            {
                // 아래쪽 변 (Z-)
                x = camPos.x + Random.Range(-halfW, halfW);
                z = camPos.z - halfH - margin;
            }
            else
            {
                // 왼쪽 변 (X-)
                x = camPos.x - halfW - margin;
                z = camPos.z + Random.Range(-halfH, halfH);
            }

            return new Vector3(x, 0f, z);
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
            if (_spawnShape == SpawnShape.Rect)
            {
                DrawRectGizmo(_normalMargin, new Color(0.4f, 1f, 0.6f, 0.6f));
                DrawRectGizmo(_eliteMargin, new Color(1f, 0.8f, 0.2f, 0.6f));
                return;
            }

            Vector3 c = _machine != null ? _machine.position : transform.position;
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(c, GetSpawnRadius() + _normalMargin);
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(c, GetSpawnRadius() + _eliteMargin);
        }

        private void DrawRectGizmo(float margin, Color color)
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float halfH = cam.orthographicSize + margin;
            float halfW = cam.orthographicSize * cam.aspect + margin;
            Vector3 c = cam.transform.position; c.y = 0f;
            Vector3 a = c + new Vector3(-halfW, 0f, -halfH);
            Vector3 b = c + new Vector3( halfW, 0f, -halfH);
            Vector3 cc= c + new Vector3( halfW, 0f,  halfH);
            Vector3 d = c + new Vector3(-halfW, 0f,  halfH);
            Gizmos.color = color;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, cc);
            Gizmos.DrawLine(cc, d);
            Gizmos.DrawLine(d, a);
        }
#endif
    }
}
