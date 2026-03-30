using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 직선 이동
    /// </summary>
    public class LinearMovement : MovementBehaviorBase
    {
        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            // 공격 범위 내면 이동 중지
            float distance = _bug.GetDistanceTo(target);
            if (distance <= _bug.AttackRange) return;

            // 타겟 방향으로 이동
            Vector3 direction = GetDirectionToTarget(target);
            Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;

            // Y축 유지하며 이동
            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);

            // 회전
            RotateTowards(direction);
        }
    }
}
