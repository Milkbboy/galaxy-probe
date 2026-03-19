using UnityEngine;

namespace DrillCorp.Bug
{
    /// <summary>
    /// 지네 - 대형 벌레
    /// 특징: 높은 체력, 높은 데미지, 느린 속도
    /// </summary>
    public class CentipedeBug : BugBase
    {
        [Header("Centipede Settings")]
        [SerializeField] private float _chargeDistance = 3f;
        [SerializeField] private float _chargeSpeedMultiplier = 2f;

        private bool _isCharging;

        protected override void Update()
        {
            if (IsDead || _target == null) return;

            float distance = Vector3.Distance(transform.position, _target.position);

            // 일정 거리 이내면 돌진
            if (distance <= _chargeDistance && !_isCharging)
            {
                _isCharging = true;
            }

            MoveToTarget();
            TryAttack();
        }

        protected override void MoveToTarget()
        {
            if (_target == null) return;

            // XZ 평면에서 이동
            Vector3 targetPos = new Vector3(_target.position.x, transform.position.y, _target.position.z);
            Vector3 direction = (targetPos - transform.position).normalized;

            float currentSpeed = _isCharging ? _moveSpeed * _chargeSpeedMultiplier : _moveSpeed;
            transform.position += direction * currentSpeed * Time.deltaTime;

            // 이동 방향으로 회전 (Y축 기준)
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        protected override float GetAttackRange()
        {
            return 1.5f;
        }

        protected override void Attack()
        {
            base.Attack();
            _isCharging = false;
        }
    }
}
