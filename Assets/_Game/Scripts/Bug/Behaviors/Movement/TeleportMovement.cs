using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 순간이동 이동 - 일정 주기로 타겟 방향으로 순간이동
    /// param1 = 텔레포트 쿨다운 (기본 3초)
    /// param2 = 텔레포트 거리 (기본 3)
    /// </summary>
    public class TeleportMovement : MovementBehaviorBase
    {
        private float _teleportCooldown;
        private float _teleportDistance;
        private float _currentCooldown;
        private bool _isTeleporting;
        private float _teleportAnimTime;
        private GameObject _effectPrefab;
        private const float TELEPORT_ANIM_DURATION = 0.3f;

        public TeleportMovement(float cooldown = 3f, float distance = 3f, GameObject effectPrefab = null)
        {
            _teleportCooldown = cooldown > 0f ? cooldown : 3f;
            _teleportDistance = distance > 0f ? distance : 3f;
            _effectPrefab = effectPrefab;
            _currentCooldown = 0f;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _currentCooldown = _teleportCooldown * 0.5f; // 시작 시 절반 쿨다운
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            float deltaTime = Time.deltaTime;

            // 텔레포트 애니메이션 중
            if (_isTeleporting)
            {
                _teleportAnimTime -= deltaTime;
                if (_teleportAnimTime <= 0f)
                {
                    _isTeleporting = false;
                }
                return;
            }

            // 공격 범위 내면 이동 중지
            float distance = _bug.GetDistanceTo(target);
            if (distance <= _bug.AttackRange) return;

            // 쿨다운 감소
            _currentCooldown -= deltaTime;

            if (_currentCooldown <= 0f)
            {
                // 텔레포트 실행
                PerformTeleport(target);
                _currentCooldown = _teleportCooldown;
            }
            // 쿨다운 중에는 정지 (타겟 방향만 바라봄)
            else
            {
                Vector3 direction = GetDirectionToTarget(target);
                RotateTowards(direction);
            }
        }

        private void PerformTeleport(Transform target)
        {
            Vector3 currentPos = _bug.transform.position;
            Vector3 direction = GetDirectionToTarget(target);

            // 타겟까지 거리
            float distanceToTarget = _bug.GetDistanceTo(target);

            // 텔레포트 거리 (타겟까지 거리보다 크면 타겟 앞까지만)
            float actualDistance = Mathf.Min(_teleportDistance, distanceToTarget - _bug.AttackRange * 0.5f);
            actualDistance = Mathf.Max(0f, actualDistance);

            // 새 위치 계산 (XZ 평면)
            Vector3 newPos = currentPos + direction * actualDistance;
            newPos.y = currentPos.y; // Y 유지

            // 이동
            _bug.transform.position = newPos;

            // 타겟 방향 회전
            RotateTowards(direction);

            // 텔레포트 이펙트
            PlayTeleportEffect(currentPos, newPos);

            // 애니메이션 상태
            _isTeleporting = true;
            _teleportAnimTime = TELEPORT_ANIM_DURATION;
        }

        private void PlayTeleportEffect(Vector3 fromPos, Vector3 toPos)
        {
            if (_effectPrefab != null)
            {
                // 커스텀 이펙트 사용
                Object.Instantiate(_effectPrefab, fromPos, Quaternion.identity);
                Object.Instantiate(_effectPrefab, toPos, Quaternion.identity);
            }
            else
            {
                // 폴백: SimpleVFX
                VFX.SimpleVFX.PlayDodge(fromPos);
                VFX.SimpleVFX.PlayDodge(toPos);
            }
        }
    }
}
