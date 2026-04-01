using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 후퇴 이동 - 공격 후 일정 시간 뒤로 물러남
    /// param1 = 후퇴 지속시간 (기본 1초)
    /// param2 = 후퇴 속도 배율 (기본 1.5배)
    /// </summary>
    public class RetreatMovement : MovementBehaviorBase
    {
        private float _retreatDuration;
        private float _retreatSpeedMultiplier;
        private float _retreatTimer;
        private bool _isRetreating;
        private int _lastAttackCount;

        public RetreatMovement(float retreatDuration = 1f, float retreatSpeedMultiplier = 1.5f)
        {
            _retreatDuration = retreatDuration > 0f ? retreatDuration : 1f;
            _retreatSpeedMultiplier = retreatSpeedMultiplier > 0f ? retreatSpeedMultiplier : 1.5f;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _lastAttackCount = 0;
            _isRetreating = false;
            _retreatTimer = 0f;
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            // 공격 횟수 체크 - 공격했으면 후퇴 시작
            int currentAttackCount = _bug.AttackCount;
            if (currentAttackCount > _lastAttackCount)
            {
                _lastAttackCount = currentAttackCount;
                StartRetreat();
            }

            if (_isRetreating)
            {
                UpdateRetreat(target);
            }
            else
            {
                UpdateApproach(target);
            }
        }

        private void StartRetreat()
        {
            _isRetreating = true;
            _retreatTimer = _retreatDuration;
        }

        private void UpdateRetreat(Transform target)
        {
            _retreatTimer -= Time.deltaTime;

            if (_retreatTimer <= 0f)
            {
                _isRetreating = false;
                return;
            }

            // 타겟 반대 방향으로 이동 (후퇴)
            Vector3 direction = -GetDirectionToTarget(target);
            float speed = GetMoveSpeed() * _retreatSpeedMultiplier;
            Vector3 movement = direction * speed * Time.deltaTime;

            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);

            // 타겟을 바라보면서 후퇴
            RotateTowards(-direction);
        }

        private void UpdateApproach(Transform target)
        {
            // 공격 범위 내면 이동 중지
            float distance = _bug.GetDistanceTo(target);
            if (distance <= _bug.AttackRange) return;

            // 타겟 방향으로 이동
            Vector3 direction = GetDirectionToTarget(target);
            Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;

            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
            RotateTowards(direction);
        }
    }
}
