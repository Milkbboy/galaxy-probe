using UnityEngine;

namespace DrillCorp.Bug
{
    /// <summary>
    /// 파리 - 빠른 비행 벌레
    /// 특징: 빠르지만 체력이 낮음
    /// </summary>
    public class FlyBug : BugBase
    {
        [Header("Fly Settings")]
        [SerializeField] private float _hoverAmplitude = 0.3f;
        [SerializeField] private float _hoverFrequency = 2f;

        private Vector3 _basePosition;
        private float _hoverOffset;

        protected override void Update()
        {
            if (IsDead || _target == null) return;

            // 호버링 효과
            _hoverOffset = Mathf.Sin(Time.time * _hoverFrequency) * _hoverAmplitude;

            MoveToTarget();
            TryAttack();
        }

        protected override void MoveToTarget()
        {
            if (_target == null) return;

            // XZ 평면에서 이동
            Vector3 targetPos = new Vector3(_target.position.x, transform.position.y, _target.position.z);
            Vector3 direction = (targetPos - transform.position).normalized;
            Vector3 movement = direction * _moveSpeed * Time.deltaTime;

            // Y축에 호버링 효과 추가 (3D에서는 높이)
            float baseHeight = 0.5f; // 기본 비행 높이
            float targetY = baseHeight + _hoverOffset;
            movement.y = (targetY - transform.position.y) * 5f * Time.deltaTime;

            transform.position += movement;

            // 이동 방향으로 회전 (Y축 기준)
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        protected override float GetAttackRange()
        {
            return 0.8f;
        }
    }
}
