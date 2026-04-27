using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.Boss
{
    /// <summary>
    /// 세션 누적 처치 점수가 임계값에 도달하면 거미 보스를 한 번 등장시킨다.
    /// v2 원본의 BOSS_KILL_THRESHOLD=700 패턴을 우리 시트 규모에 맞게 조정.
    /// </summary>
    public class BossSpawnManager : MonoBehaviour
    {
        [Header("Boss")]
        [SerializeField] private GameObject _spiderBossPrefab;

        [Header("Trigger")]
        [Tooltip("세션 누적 처치 점수가 이 값 이상이면 보스 등장")]
        [SerializeField] private float _killThreshold = 250f;

        [Header("Spawn")]
        [Tooltip("보스 등장 좌표를 머신 위치 기준으로 처리할지")]
        [SerializeField] private bool _spawnAroundMachine = true;
        [Tooltip("머신 기준 보스 등장 거리 (perch 반경과 일치 권장)")]
        [SerializeField] private float _spawnDistance = 15f;

        private Transform _machine;
        private float _accumScore;
        private bool _spawned;

        public bool BossSpawned => _spawned;
        public float AccumulatedScore => _accumScore;

        private void Awake()
        {
            var mc = FindAnyObjectByType<MachineController>();
            if (mc != null) _machine = mc.transform;
        }

        private void OnEnable()
        {
            GameEvents.OnBugScoreEarned += OnScoreEarned;
        }

        private void OnDisable()
        {
            GameEvents.OnBugScoreEarned -= OnScoreEarned;
        }

        private void OnScoreEarned(float score)
        {
            if (_spawned) return;

            _accumScore += score;
            if (_accumScore >= _killThreshold) SpawnBoss();
        }

        private void SpawnBoss()
        {
            if (_spawned) return;
            if (_spiderBossPrefab == null)
            {
                Debug.LogError("[BossSpawnManager] _spiderBossPrefab가 비어있음 — 보스 등장 실패");
                return;
            }

            Vector3 spawnPos = ComputeSpawnPos();
            var boss = Instantiate(_spiderBossPrefab, spawnPos, Quaternion.identity);

            // 보스 본체 명시 초기화 — Initialize 안에서 perch 위치를 결정하므로 spawnPos는 hint.
            var spider = boss.GetComponent<SpiderBoss>();
            if (spider != null) spider.Initialize(spawnPos);
            else Debug.LogWarning("[BossSpawnManager] prefab 루트에 SpiderBoss 컴포넌트 없음");

            _spawned = true;
        }

        private Vector3 ComputeSpawnPos()
        {
            if (!_spawnAroundMachine || _machine == null) return Vector3.zero;
            // 랜덤 각도, 머신 기준 _spawnDistance 거리
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(
                _machine.position.x + Mathf.Cos(angle) * _spawnDistance,
                0f,
                _machine.position.z + Mathf.Sin(angle) * _spawnDistance);
        }

        /// <summary>디버그/치트용 — 즉시 보스 등장</summary>
        public void ForceSpawn()
        {
            SpawnBoss();
        }
    }
}
