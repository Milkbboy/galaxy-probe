using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 공중 부유 이동 (위아래로 떠다니며)
    /// </summary>
    public class HoverMovement : MovementBehaviorBase
    {
        private float _hoverHeight;     // 부유 높이
        private float _hoverPeriod;     // 부유 주기 (초)
        private float _baseHeight;      // 기본 비행 높이

        public HoverMovement(float height = 0.3f, float period = 2f)
        {
            _hoverHeight = height > 0f ? height : 0.3f;
            _hoverPeriod = period > 0f ? period : 2f;
            _baseHeight = 0.5f;
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            // 공격 범위 내면 X,Z 이동 중지 (호버링은 계속)
            float distance = _bug.GetDistanceTo(target);
            bool shouldMove = distance > _bug.AttackRange;

            // 호버링 계산
            float hoverOffset = Mathf.Sin(Time.time * (2f * Mathf.PI / _hoverPeriod)) * _hoverHeight;
            float targetY = _baseHeight + hoverOffset;

            // 현재 Y를 목표 Y로 부드럽게 이동
            float currentY = _bug.transform.position.y;
            float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * 5f);

            if (shouldMove)
            {
                // 타겟 방향으로 X,Z 이동
                Vector3 direction = GetDirectionToTarget(target);
                Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;

                _bug.transform.position = new Vector3(
                    _bug.transform.position.x + movement.x,
                    newY,
                    _bug.transform.position.z + movement.z
                );

                RotateTowards(direction);
            }
            else
            {
                // 호버링만
                _bug.transform.position = new Vector3(
                    _bug.transform.position.x,
                    newY,
                    _bug.transform.position.z
                );
            }
        }
    }
}
