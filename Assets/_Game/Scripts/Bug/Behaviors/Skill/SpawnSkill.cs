using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Skill
{
    /// <summary>
    /// 졸개 소환 스킬 - 주변에 작은 벌레를 소환
    /// param1 = 쿨다운
    /// param2 = 소환 수량
    /// prefab = 소환할 버그 프리펩
    /// </summary>
    public class SpawnSkill : SkillBehaviorBase
    {
        private int _spawnCount;
        private GameObject _spawnPrefab;
        private float _spawnRadius = 2f;

        public SpawnSkill(float cooldown = 10f, int count = 2, GameObject prefab = null)
            : base(cooldown, 999f) // 거리 제한 없음
        {
            _spawnCount = count > 0 ? count : 2;
            _spawnPrefab = prefab;
        }

        protected override void UseSkill(Transform target)
        {
            if (_spawnPrefab == null)
            {
                Debug.LogWarning("[SpawnSkill] Spawn prefab not set");
                return;
            }

            Vector3 center = _bug.transform.position;

            // 타겟(머신) 방향 계산 (XZ 평면)
            Vector3 toTarget = Vector3.forward;
            if (_bug.Target != null)
            {
                Vector3 diff = _bug.Target.position - center;
                toTarget = new Vector3(diff.x, 0f, diff.z).normalized;
            }

            // 소환 위치 각도들 (앞 제외, 측면/후방)
            float[] spawnAngles = GetSpawnAngles(_spawnCount);

            for (int i = 0; i < _spawnCount; i++)
            {
                // 타겟 방향 기준으로 회전
                float angle = spawnAngles[i] * Mathf.Deg2Rad;
                Vector3 offset = Quaternion.Euler(0f, spawnAngles[i], 0f) * (-toTarget) * _spawnRadius;
                Vector3 spawnPos = new Vector3(center.x + offset.x, 0f, center.z + offset.z);

                // 소환
                GameObject spawned = Object.Instantiate(_spawnPrefab, spawnPos, Quaternion.identity);

                // BugController가 있으면 타겟 설정
                var bugController = spawned.GetComponent<BugController>();
                if (bugController != null)
                {
                    // 소환된 버그는 부모의 타겟을 공유
                    // Initialize는 BugSpawner에서 호출되거나, 프리펩에 설정된 데이터 사용
                }
            }

            // 소환 이펙트
            VFX.SimpleVFX.PlayBugHit(center); // 임시로 BugHit 이펙트 사용
        }

        /// <summary>
        /// 소환 개수에 따른 각도 배열 (앞 제외, 측면/후방)
        /// 0도 = 뒤, 90도 = 왼쪽, -90도 = 오른쪽
        /// </summary>
        private float[] GetSpawnAngles(int count)
        {
            switch (count)
            {
                case 1:
                    return new float[] { 0f }; // 뒤
                case 2:
                    return new float[] { 90f, -90f }; // 왼, 오
                case 3:
                    return new float[] { 90f, -90f, 0f }; // 왼, 오, 뒤
                case 4:
                    return new float[] { 90f, -90f, 135f, -135f }; // 왼, 오, 왼뒤, 오른뒤
                case 5:
                    return new float[] { 90f, -90f, 135f, -135f, 0f }; // 왼, 오, 왼뒤, 오른뒤, 뒤
                default:
                    // 6개 이상: 후방 180도 범위에 균등 배치
                    float[] angles = new float[count];
                    float step = 180f / (count - 1);
                    for (int i = 0; i < count; i++)
                    {
                        angles[i] = -90f + (step * i);
                    }
                    return angles;
            }
        }
    }
}
