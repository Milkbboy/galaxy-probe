using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 돌진 이동 (멈췄다가 빠르게 돌진)
    /// </summary>
    public class BurstMovement : MovementBehaviorBase
    {
        private float _waitTime;        // 대기 시간
        private float _speedMultiplierBurst;  // 돌진 속도 배율

        private float _currentWaitTimer;
        private bool _isBursting;
        private float _burstDuration = 0.5f;  // 돌진 지속 시간
        private float _burstTimer;

        public BurstMovement(float waitTime = 2f, float speedMultiplier = 3f)
        {
            _waitTime = waitTime > 0f ? waitTime : 2f;
            _speedMultiplierBurst = speedMultiplier > 0f ? speedMultiplier : 3f;
            _currentWaitTimer = _waitTime;
            _isBursting = false;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _currentWaitTimer = _waitTime;
            _isBursting = false;
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            // 공격 범위 내면 대기
            float distance = _bug.GetDistanceTo(target);
            if (distance <= _bug.AttackRange)
            {
                _isBursting = false;
                _currentWaitTimer = _waitTime;
                return;
            }

            if (_isBursting)
            {
                // 돌진 중
                _burstTimer -= Time.deltaTime;

                Vector3 direction = GetDirectionToTarget(target);
                float burstSpeed = GetMoveSpeed() * _speedMultiplierBurst;
                Vector3 movement = direction * burstSpeed * Time.deltaTime;

                _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
                RotateTowards(direction);

                if (_burstTimer <= 0f)
                {
                    _isBursting = false;
                    _currentWaitTimer = _waitTime;
                }
            }
            else
            {
                // 대기 중
                _currentWaitTimer -= Time.deltaTime;

                // 타겟 방향 바라보기
                Vector3 direction = GetDirectionToTarget(target);
                RotateTowards(direction);

                if (_currentWaitTimer <= 0f)
                {
                    // 돌진 시작
                    _isBursting = true;
                    _burstTimer = _burstDuration;
                }
            }
        }
    }
}
