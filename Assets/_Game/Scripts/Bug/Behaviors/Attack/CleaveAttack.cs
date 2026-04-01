using UnityEngine;
using DrillCorp.Machine;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 부채꼴 범위 공격 - 전방 일정 각도 내 모든 대상에게 데미지
    /// param1 = 공격 각도 (기본 90도)
    /// </summary>
    public class CleaveAttack : AttackBehaviorBase
    {
        private float _cleaveAngle;
        private LineRenderer _rangeIndicator;
        private const int ARC_SEGMENTS = 20;

        public CleaveAttack(float angle = 90f)
        {
            _cleaveAngle = angle > 0f ? angle : 90f;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _attackRange = bug.AttackRange;
            CreateRangeIndicator();
        }

        public override void Cleanup()
        {
            if (_rangeIndicator != null)
            {
                Object.Destroy(_rangeIndicator.gameObject);
                _rangeIndicator = null;
            }
            base.Cleanup();
        }

        private void CreateRangeIndicator()
        {
            if (_bug == null) return;

            GameObject indicatorObj = new GameObject("CleaveRangeIndicator");
            indicatorObj.transform.SetParent(_bug.transform);
            indicatorObj.transform.localPosition = Vector3.zero;

            _rangeIndicator = indicatorObj.AddComponent<LineRenderer>();
            _rangeIndicator.useWorldSpace = true;
            _rangeIndicator.loop = true;
            _rangeIndicator.startWidth = 0.05f;
            _rangeIndicator.endWidth = 0.05f;
            _rangeIndicator.positionCount = ARC_SEGMENTS + 2; // 호 + 중심점 2개 (양쪽 직선)

            // 머티리얼 설정 (반투명 빨간색)
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.3f, 0.2f, 0.7f);
            _rangeIndicator.material = mat;
        }

        /// <summary>
        /// 매 프레임 범위 표시 업데이트 (BugController에서 호출 필요)
        /// </summary>
        public void UpdateRangeIndicator(Transform target)
        {
            if (_rangeIndicator == null || _bug == null || target == null) return;

            Vector3 bugPos = _bug.transform.position;
            bugPos.y = 0.1f; // 바닥 약간 위

            Vector3 targetPos = target.position;
            Vector3 forward = new Vector3(targetPos.x - bugPos.x, 0f, targetPos.z - bugPos.z).normalized;

            float halfAngle = _cleaveAngle / 2f;
            Vector3[] positions = new Vector3[ARC_SEGMENTS + 2];

            // 시작점 (중심)
            positions[0] = bugPos;

            // 호 그리기
            for (int i = 0; i <= ARC_SEGMENTS; i++)
            {
                float t = (float)i / ARC_SEGMENTS;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 direction = rotation * forward;
                positions[i + 1] = bugPos + direction * _attackRange;
                positions[i + 1].y = 0.1f;
            }

            _rangeIndicator.SetPositions(positions);
        }

        protected override void PerformAttack(Transform target)
        {
            if (_bug == null) return;

            Vector3 bugPos = _bug.transform.position;
            Vector3 targetPos = target.position;
            // 탑뷰: 벌레 → 타겟 방향 (XZ 평면)
            Vector3 forward = new Vector3(targetPos.x - bugPos.x, 0f, targetPos.z - bugPos.z).normalized;

            // 범위 내 모든 타겟 검색 (탑다운 뷰에서는 XZ 평면)
            Collider[] hits = Physics.OverlapSphere(bugPos, _attackRange);

            float damage = GetDamage();

            foreach (var hit in hits)
            {
                // 자기 자신 제외
                if (hit.transform == _bug.transform) continue;
                if (hit.transform.IsChildOf(_bug.transform)) continue;

                // IDamageable이 있는 대상만
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;

                // 각도 체크
                Vector3 toHit = (hit.transform.position - bugPos);
                toHit.y = 0f;
                toHit.Normalize();

                float angle = Vector3.Angle(forward, toHit);
                if (angle <= _cleaveAngle / 2f)
                {
                    DealDamage(hit.transform, damage);
                }
            }

            // VFX 재생 - 부채꼴 범위 표시
            PlayCleaveVfx(bugPos, forward, _attackRange, _cleaveAngle);
        }

        private void PlayCleaveVfx(Vector3 center, Vector3 forward, float range, float angle)
        {
            // 부채꼴 범위 내 여러 지점에 이펙트 생성
            int effectCount = Mathf.CeilToInt(angle / 15f); // 15도마다 이펙트
            float halfAngle = angle / 2f;

            for (int i = 0; i <= effectCount; i++)
            {
                float t = effectCount > 0 ? (float)i / effectCount : 0.5f;
                float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);

                // 방향 회전
                Quaternion rotation = Quaternion.Euler(0f, currentAngle, 0f);
                Vector3 direction = rotation * forward.normalized;

                // 거리별로 이펙트 (가까이, 중간, 끝)
                for (float dist = 0.3f; dist <= range; dist += range * 0.4f)
                {
                    Vector3 pos = center + direction * dist;
                    pos.y = 0.5f;
                    VFX.SimpleVFX.PlayMeleeHit(pos);
                }
            }
        }
    }
}
