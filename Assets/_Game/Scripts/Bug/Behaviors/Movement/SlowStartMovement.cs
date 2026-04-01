using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 점점 가속 이동 - 시작 시 느리게, 점점 빨라짐
    /// param1 = 가속 시간 (기본 2초)
    /// param2 = 최종 속도 배율 (기본 2배)
    /// </summary>
    public class SlowStartMovement : MovementBehaviorBase
    {
        private float _accelerationTime;
        private float _maxSpeedMultiplier;
        private float _elapsedTime;

        public SlowStartMovement(float accelerationTime = 2f, float maxSpeedMultiplier = 2f)
        {
            _accelerationTime = accelerationTime > 0f ? accelerationTime : 2f;
            _maxSpeedMultiplier = maxSpeedMultiplier > 0f ? maxSpeedMultiplier : 2f;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _elapsedTime = 0f;
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            _elapsedTime += Time.deltaTime;

            // 공격 범위 내면 이동 중지
            float distance = _bug.GetDistanceTo(target);
            if (distance <= _bug.AttackRange) return;

            // 가속도 계산 (0 ~ 1)
            float t = Mathf.Clamp01(_elapsedTime / _accelerationTime);
            // EaseInQuad로 부드럽게 가속
            float speedRatio = t * t;
            float currentSpeedMult = Mathf.Lerp(0.1f, _maxSpeedMultiplier, speedRatio);

            // 타겟 방향으로 이동
            Vector3 direction = GetDirectionToTarget(target);
            float speed = GetMoveSpeed() * currentSpeedMult;
            Vector3 movement = direction * speed * Time.deltaTime;

            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
            RotateTowards(direction);
        }
    }
}
